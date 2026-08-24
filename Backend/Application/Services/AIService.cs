using System.Text.RegularExpressions;
using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application.Services;

public class AIService : IAIService
{
    private readonly IProductRepository _productRepository;
    private readonly ICajaRepository _cajaRepository;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IExtensionesRepository _extensionesRepository;

    private static readonly Dictionary<string, string> CategoriasGasto = new()
    {
        ["movilidad"] = "Movilidad", ["taxi"] = "Movilidad", ["pasaje"] = "Movilidad",
        ["publicidad"] = "Publicidad", ["alquiler"] = "Alquiler", ["internet"] = "Internet",
        ["luz"] = "Luz", ["agua"] = "Agua", ["sueldo"] = "Sueldos", ["comision"] = "Comisiones",
        ["mantenimiento"] = "Mantenimiento", ["insumo"] = "Insumos", ["limpieza"] = "Limpieza"
    };

    public AIService(
        IProductRepository productRepository,
        ICajaRepository cajaRepository,
        IDashboardRepository dashboardRepository,
        IExtensionesRepository extensionesRepository)
    {
        _productRepository = productRepository;
        _cajaRepository = cajaRepository;
        _dashboardRepository = dashboardRepository;
        _extensionesRepository = extensionesRepository;
    }

    public async Task<IntentResult> Procesar(string texto, string? username, int? sucursalId)
    {
        var t = Normalizar(texto);

        if (t.Contains("cuanto") || t.Contains("cuánto") || t.Contains("que hay"))
        {
            if (t.Contains("vend")) return await VentasHoy();
            if (t.Contains("dinero") || t.Contains("caja") || t.Contains("tengo")) return await DineroActual(username, sucursalId);
            return await ResumenDia();
        }

        if (t.Contains("gast")) return await ProponerGasto(t);
        if (t.Contains("vend") || t.Contains("venta")) return await ProponerVenta(t);
        if (t.Contains("compr")) return await ProponerCompra(t);
        if (t.Contains("ingreso")) return await ProponerIngreso(t);
        if (t.Contains("stock")) return await Stock(t);
        if (t.Contains("resumen") || t.Contains("estado")) return await ResumenDia();

        return new IntentResult
        {
            Intencion = "desconocida",
            Respuesta = "Hola, soy el asistente de tu negocio. Puedes preguntarme:\n" +
                        "- ¿Cuánto vendí hoy?\n" +
                        "- ¿Cuánto dinero tengo?\n" +
                        "- ¿Qué producto tiene stock bajo?\n" +
                        "- Registra un gasto de 30 soles en movilidad\n" +
                        "- Compré 20 polos negros a 18 soles cada uno\n" +
                        "- Vendí 2 polos negros a 35 soles cada uno y me pagaron por Yape"
        };
    }

    private async Task<IntentResult> VentasHoy()
    {
        var (estado, resumen, _) = await _dashboardRepository.Resumen();
        if (estado != ServiceStatus.Ok || resumen == null)
            return new IntentResult { Intencion = "get_sales", Respuesta = "No pude consultar las ventas de hoy." };

        var top = resumen.ProductosMasVendidos.FirstOrDefault();
        var respuesta = $"Hoy vendiste S/{resumen.VentasHoy:N2}.";
        if (top != null)
            respuesta += $" El producto más vendido fue {top.Producto} ({top.Cantidad} unid.).";
        return new IntentResult { Intencion = "get_sales", Respuesta = respuesta };
    }

    private async Task<IntentResult> DineroActual(string? username, int? sucursalId)
    {
        var (estado, obj, _) = await _cajaRepository.MontoActual(username ?? "TODOS", sucursalId);
        if (estado != ServiceStatus.Ok || obj is not PagoDto pago)
            return new IntentResult { Intencion = "get_cash_balance", Respuesta = "No hay una caja abierta registrada para consultar el dinero." };

        var detalle = pago.MontosPorMetodo.Count > 0
            ? string.Join(", ", pago.MontosPorMetodo.Select(m => $"{m.Nombre}: S/{m.Monto:N2}"))
            : "sin movimientos aún";

        return new IntentResult
        {
            Intencion = "get_cash_balance",
            Respuesta = $"Dinero registrado en caja: S/{pago.MontoTotal:N2} ({detalle})."
        };
    }

    private async Task<IntentResult> Stock(string texto)
    {
        var (estado, resumen, _) = await _dashboardRepository.Resumen();
        if (estado != ServiceStatus.Ok || resumen == null)
            return new IntentResult { Intencion = "get_stock", Respuesta = "No pude consultar el stock." };

        if (texto.Contains("bajo") || texto.Contains("falta"))
            return new IntentResult
            {
                Intencion = "get_low_stock",
                Respuesta = $"Tienes {resumen.ProductosStockBajo} producto(s) con stock bajo. Stock total actual: {resumen.StockTotal}."
            };

        return new IntentResult
        {
            Intencion = "get_stock",
            Respuesta = $"Stock total de tu negocio: {resumen.StockTotal} unidades, de los cuales {resumen.ProductosStockBajo} producto(s) están bajo el mínimo."
        };
    }

    private async Task<IntentResult> ResumenDia()
    {
        var (estado, resumen, _) = await _dashboardRepository.Resumen();
        if (estado != ServiceStatus.Ok || resumen == null)
            return new IntentResult { Intencion = "get_daily_summary", Respuesta = "No pude consultar el resumen del día." };

        return new IntentResult
        {
            Intencion = "get_daily_summary",
            Respuesta = $"Resumen de hoy:\n" +
                        $"- Ventas: S/{resumen.VentasHoy:N2}\n" +
                        $"- Gastos: S/{resumen.GastosHoy:N2}\n" +
                        $"- Compras: S/{resumen.ComprasHoy:N2}\n" +
                        $"- Utilidad estimada: S/{resumen.UtilidadEstimada:N2}"
        };
    }

    private async Task<IntentResult> ProponerGasto(string texto)
    {
        var monto = PrimerMonto(texto);
        if (monto == null)
            return new IntentResult { Intencion = "create_expense", Respuesta = "No pude identificar el monto. Ejemplo: \"gasté 30 soles en movilidad\"." };

        var categoria = DetectarCategoria(texto);
        var metodo = DetectaMetodoPago(texto);
        var metodoId = await ResolverMetodoPago(metodo);
        var descripcion = ExtraerDescripcion(texto, categoria);

        var payload = JObject.FromObject(new
        {
            intencion = "create_expense",
            categoria,
            descripcion,
            monto = monto.Value,
            metodoPagoId = metodoId
        });

        return new IntentResult
        {
            Intencion = "create_expense",
            RequiereConfirmacion = true,
            Respuesta = $"Detecté un gasto de S/{monto.Value:N0} en {categoria}{(metodo != null ? $" pagado por {metodo}" : "")}. ¿Confirmas el registro? (responde \"sí\" o \"confirmo\")",
            PayloadJson = payload.ToString(Formatting.None)
        };
    }

    private async Task<IntentResult> ProponerIngreso(string texto)
    {
        var monto = PrimerMonto(texto);
        if (monto == null)
            return new IntentResult { Intencion = "create_income", Respuesta = "No pude identificar el monto. Ejemplo: \"registra un ingreso de 50 soles\"." };

        var metodo = DetectaMetodoPago(texto);
        var metodoId = await ResolverMetodoPago(metodo);
        var tipo = texto.Contains("otro") ? "OTRO" : texto.Contains("capital") ? "CAPITAL" : "VENTA_EXTRA";
        var descripcion = texto;

        var payload = JObject.FromObject(new
        {
            intencion = "create_income",
            tipo,
            descripcion,
            monto = monto.Value,
            metodoPagoId = metodoId
        });

        return new IntentResult
        {
            Intencion = "create_income",
            RequiereConfirmacion = true,
            Respuesta = $"Detecté un ingreso de S/{monto.Value:N0}{(metodo != null ? $" por {metodo}" : "")}. ¿Confirmas el registro? (responde \"sí\" o \"confirmo\")",
            PayloadJson = payload.ToString(Formatting.None)
        };
    }

    private async Task<IntentResult> ProponerCompra(string texto)
    {
        var (ok, cantidad, precio, productoNombre, metodo) = ParseOperacion(texto);
        if (!ok)
            return new IntentResult { Intencion = "create_purchase", Respuesta = "No entendí la compra. Formato: \"Compré 20 polos negros a 18 soles cada uno\"." };

        var producto = await BuscarProducto(productoNombre);
        if (producto == null)
            return new IntentResult { Intencion = "create_purchase", Respuesta = $"No encontré un producto llamado \"{productoNombre}\". Verifica el nombre." };
        if (producto.ExisteAmbiguo)
            return new IntentResult { Intencion = "create_purchase", Respuesta = $"Encontré varios productos que coinciden ({producto.Nombres}). Por favor escribe el nombre exacto." };

        var metodoId = await ResolverMetodoPago(metodo);
        var total = cantidad * precio;

        var payload = JObject.FromObject(new
        {
            intencion = "create_purchase",
            productoId = producto.ProductoId,
            cantidad,
            precio,
            metodoPagoId = metodoId
        });

        return new IntentResult
        {
            Intencion = "create_purchase",
            RequiereConfirmacion = true,
            Respuesta = $"Detecté una compra de {cantidad} unid. de {productoNombre} a S/{precio:N2} c/u (total S/{total:N2}){(metodo != null ? $" pagado por {metodo}" : "")}. ¿Confirmas? (responde \"sí\" o \"confirmo\")",
            PayloadJson = payload.ToString(Formatting.None)
        };
    }

    private async Task<IntentResult> ProponerVenta(string texto)
    {
        var (ok, cantidad, precio, productoNombre, metodo) = ParseOperacion(texto);
        if (!ok)
            return new IntentResult { Intencion = "create_sale", Respuesta = "No entendí la venta. Formato: \"Vendí 2 polos negros a 35 soles cada uno y me pagaron por Yape\"." };

        var producto = await BuscarProducto(productoNombre);
        if (producto == null)
            return new IntentResult { Intencion = "create_sale", Respuesta = $"No encontré un producto llamado \"{productoNombre}\". Verifica el nombre." };
        if (producto.ExisteAmbiguo)
            return new IntentResult { Intencion = "create_sale", Respuesta = $"Encontré varios productos que coinciden ({producto.Nombres}). Por favor escribe el nombre exacto." };

        var metodoId = await ResolverMetodoPago(metodo);
        var total = cantidad * precio;

        var payload = JObject.FromObject(new
        {
            intencion = "create_sale",
            tipoDocumentoVentaId = 3,
            productoId = producto.ProductoId,
            cantidad,
            precio,
            metodoPagoId = metodoId
        });

        return new IntentResult
        {
            Intencion = "create_sale",
            RequiereConfirmacion = true,
            Respuesta = $"Detecté una venta de {cantidad} unid. de {productoNombre} a S/{precio:N2} c/u (total S/{total:N2}){(metodo != null ? $", pago por {metodo}" : "")}. ¿Confirmas? (responde \"sí\" o \"confirmo\")",
            PayloadJson = payload.ToString(Formatting.None)
        };
    }

    private async Task<Producido?> BuscarProducto(string nombre)
    {
        var (estado, data, _) = await _productRepository.GetProducto(new ProductPayload { Value = nombre, Page = 1, Amount = 5 });
        if (estado != ServiceStatus.Ok || data?.Items == null || data.Items.Count == 0)
            return null;

        var items = data.Items.Where(p => p.Nombre != null && p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase)).ToList();
        if (items.Count == 0) return null;
        if (items.Count == 1)
            return new Producido { ProductoId = items[0].productoId, ExisteAmbiguo = false, Nombres = items[0].Nombre! };

        return new Producido { ProductoId = items[0].productoId, ExisteAmbiguo = true, Nombres = string.Join(" / ", items.Select(i => i.Nombre)) };
    }

    private async Task<int?> ResolverMetodoPago(string? metodo)
    {
        if (metodo == null) return null;

        var (estado, obj, _) = await _extensionesRepository.ListarMetodoPago();
        if (estado != ServiceStatus.Ok || obj == null) return null;

        var arr = JArray.FromObject(obj);
        var match = arr.FirstOrDefault(m => m["value"]?.ToString().ToLower().Contains(metodo) == true);
        return match?["id"]?.Value<int>();
    }

    private static (bool ok, int cantidad, decimal precio, string producto, string? metodo) ParseOperacion(string texto)
    {
        var matchPrecio = Regex.Match(texto, @"a\s+(\d+(?:[.,]\d+)?)");
        var matchCantidad = Regex.Match(texto, @"(\d+)\s+");
        if (!matchCantidad.Success)
            return (false, 0, 0, string.Empty, null);

        var cantidad = int.Parse(matchCantidad.Groups[1].Value);
        decimal precio = 0;
        if (matchPrecio.Success)
            precio = decimal.Parse(matchPrecio.Groups[1].Value.Replace(",", "."));

        var producto = string.Empty;
        if (matchCantidad.Success && matchPrecio.Success)
        {
            var start = matchCantidad.Index + matchCantidad.Length;
            var idxA = texto.LastIndexOf("a", matchPrecio.Index);
            if (idxA >= start)
                producto = texto.Substring(start, idxA - start).Trim();
        }

        if (string.IsNullOrWhiteSpace(producto) || precio <= 0)
            return (false, 0, 0, string.Empty, null);

        return (true, cantidad, precio, producto, DetectaMetodoPago(texto));
    }

    private static string? DetectaMetodoPago(string texto)
    {
        if (texto.Contains("yape")) return "yape";
        if (texto.Contains("plin")) return "plin";
        if (texto.Contains("tarjeta")) return "tarjeta";
        if (texto.Contains("transferencia")) return "transferencia";
        if (texto.Contains("efectivo") || texto.Contains("cash")) return "efectivo";
        return null;
    }

    private static string DetectarCategoria(string texto)
    {
        foreach (var kv in CategoriasGasto)
            if (texto.Contains(kv.Key)) return kv.Value;
        return "Otros";
    }

    private static string ExtraerDescripcion(string texto, string categoria)
    {
        var idx = texto.LastIndexOf("en");
        return idx >= 0 ? texto.Substring(idx + 2).Trim() : $"{categoria} registrado por WhatsApp";
    }

    private static decimal? PrimerMonto(string texto)
    {
        var match = Regex.Match(texto, @"\d+(?:[.,]\d+)?");
        if (!match.Success) return null;
        return decimal.Parse(match.Value.Replace(",", "."));
    }

    private static string Normalizar(string texto)
        => texto.ToLower().Trim();

    private class Producido
    {
        public int ProductoId { get; set; }
        public bool ExisteAmbiguo { get; set; }
        public string Nombres { get; set; } = null!;
    }
}