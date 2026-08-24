using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application.Services;

public class WhatsappService : IWhatsappService
{
    private readonly IWhatsappRepository _whatsappRepository;
    private readonly IAIService _aiService;
    private readonly IComprobanteRepository _comprobanteRepository;
    private readonly ICompraRepository _compraRepository;
    private readonly IGastoRepository _gastoRepository;
    private readonly IIngresoRepository _ingresoRepository;

    private const string Confirmar = "CONFIRMACION_PENDIENTE";

    public WhatsappService(
        IWhatsappRepository whatsappRepository,
        IAIService aiService,
        IComprobanteRepository comprobanteRepository,
        ICompraRepository compraRepository,
        IGastoRepository gastoRepository,
        IIngresoRepository ingresoRepository)
    {
        _whatsappRepository = whatsappRepository;
        _aiService = aiService;
        _comprobanteRepository = comprobanteRepository;
        _compraRepository = compraRepository;
        _gastoRepository = gastoRepository;
        _ingresoRepository = ingresoRepository;
    }

    public async Task<IntentResult> ProcesarMensaje(WhatsappMessagePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.MessageId) || await _whatsappRepository.ExisteMensaje(payload.MessageId))
            return new IntentResult { Intencion = "duplicado", Respuesta = "Mensaje ya procesado." };

        var conversacion = await _whatsappRepository.ObtenerConversacion(payload.Numero);
        var esConfirmacion = EsConfirmacion(payload.Texto) && conversacion?.Estado == Confirmar;

        IntentResult resultado;

        if (esConfirmacion)
        {
            resultado = await EjecutarConfirmacion(conversacion!);
            await _whatsappRepository.GuardarConversacion(NuevaConversacion(conversacion!, null, null));
        }
        else
        {
            resultado = await _aiService.Procesar(payload.Texto, payload.Username, payload.SucursalId);

            if (resultado.RequiereConfirmacion && !string.IsNullOrEmpty(resultado.PayloadJson))
            {
                var nueva = conversacion ?? new WhatsappConversation { Numero = payload.Numero };
                nueva.Estado = Confirmar;
                nueva.ContextoJson = resultado.PayloadJson;
                await _whatsappRepository.GuardarConversacion(nueva);
            }
            else if (conversacion?.Estado == Confirmar)
            {
                await _whatsappRepository.GuardarConversacion(NuevaConversacion(conversacion, null, null));
            }
        }

        await _whatsappRepository.RegistrarMensaje(payload, resultado.Intencion, resultado.RequiereConfirmacion || esConfirmacion ? Confirmar : "PROCESADO", resultado.Respuesta);

        return resultado;
    }

    private async Task<IntentResult> EjecutarConfirmacion(WhatsappConversation conversacion)
    {
        if (string.IsNullOrWhiteSpace(conversacion.ContextoJson))
            return new IntentResult { Intencion = "sin_contexto", Respuesta = "No hay una operación pendiente por confirmar." };

        var d = JObject.Parse(conversacion.ContextoJson!);
        var intencion = d["intencion"]?.ToString();

        try
        {
            switch (intencion)
            {
                case "create_expense":
                    return await RegistrarGasto(d);
                case "create_income":
                    return await RegistrarIngreso(d);
                case "create_purchase":
                    return await RegistrarCompra(d);
                case "create_sale":
                    return await RegistrarVenta(d);
                default:
                    return new IntentResult { Intencion = intencion ?? "desconocida", Respuesta = "No puedo procesar esa confirmación." };
            }
        }
        catch (Exception)
        {
            return new IntentResult { Intencion = intencion ?? "error", Respuesta = "Ocurrió un error al procesar la operación. Inténtalo de nuevo." };
        }
    }

    private async Task<IntentResult> RegistrarVenta(JObject d)
    {
        var cantidad = d["cantidad"]?.Value<int>() ?? 0;
        var precio = d["precio"]?.Value<decimal>() ?? 0;
        var total = cantidad * precio;

        var payload = new ComprobantePayload
        {
            TipoDocumentoVentaId = d["tipoDocumentoVentaId"]?.Value<int>() ?? 3,
            Total = total,
            DetalleComprobante = new List<ComprobanteDetallePayload>
            {
                new() { ProductoId = d["productoId"]?.Value<int>() ?? 0, Cantidad = cantidad, ValorUnitario = precio }
            }
        };

        var metodoPagoId = d["metodoPagoId"]?.Value<int?>();
        if (metodoPagoId.HasValue)
            payload.DetallePago.Add(new PagoPayload { MetodoPagoId = metodoPagoId.Value, Monto = total });

        var (estado, _, message) = await _comprobanteRepository.CrearComprobante(payload);
        return estado == ServiceStatus.Ok
            ? new IntentResult { Intencion = "create_sale", Respuesta = $"Venta registrada: {cantidad} unid. por S/{total:N2}. Stock actualizado." }
            : new IntentResult { Intencion = "create_sale", Respuesta = $"No pude registrar la venta: {message}" };
    }

    private async Task<IntentResult> RegistrarCompra(JObject d)
    {
        var cantidad = d["cantidad"]?.Value<int>() ?? 0;
        var costo = d["precio"]?.Value<decimal>() ?? 0;

        var payload = new CreateCompraPayload
        {
            MetodoPagoId = d["metodoPagoId"]?.Value<int?>(),
            Observacion = "Registrada por WhatsApp",
            Detalle = new List<CompraDetallePayload>
            {
                new() { ProductoId = d["productoId"]?.Value<int>() ?? 0, Cantidad = cantidad, CostoUnitario = costo }
            }
        };

        var (estado, _, message) = await _compraRepository.CrearCompra(payload);
        return estado == ServiceStatus.Ok
            ? new IntentResult { Intencion = "create_purchase", Respuesta = $"Compra registrada: {cantidad} unid. por S/{cantidad * costo:N2}. Stock actualizado." }
            : new IntentResult { Intencion = "create_purchase", Respuesta = $"No pude registrar la compra: {message}" };
    }

    private async Task<IntentResult> RegistrarGasto(JObject d)
    {
        var payload = new CreateGastoPayload
        {
            Categoria = d["categoria"]?.ToString() ?? "Otros",
            Descripcion = d["descripcion"]?.ToString() ?? "Registrado por WhatsApp",
            Monto = d["monto"]?.Value<decimal>() ?? 0,
            MetodoPagoId = d["metodoPagoId"]?.Value<int?>()
        };

        var (estado, _, message) = await _gastoRepository.CrearGasto(payload);
        return estado == ServiceStatus.Ok
            ? new IntentResult { Intencion = "create_expense", Respuesta = $"Gasto registrado: S/{payload.Monto:N2} en {payload.Categoria}." }
            : new IntentResult { Intencion = "create_expense", Respuesta = $"No pude registrar el gasto: {message}" };
    }

    private async Task<IntentResult> RegistrarIngreso(JObject d)
    {
        var payload = new CreateIngresoPayload
        {
            Tipo = d["tipo"]?.ToString() ?? "OTRO",
            Descripcion = d["descripcion"]?.ToString() ?? "Registrado por WhatsApp",
            Monto = d["monto"]?.Value<decimal>() ?? 0,
            MetodoPagoId = d["metodoPagoId"]?.Value<int?>()
        };

        var (estado, _, message) = await _ingresoRepository.CrearIngreso(payload);
        return estado == ServiceStatus.Ok
            ? new IntentResult { Intencion = "create_income", Respuesta = $"Ingreso registrado: S/{payload.Monto:N2}." }
            : new IntentResult { Intencion = "create_income", Respuesta = $"No pude registrar el ingreso: {message}" };
    }

    private static bool EsConfirmacion(string texto)
    {
        var t = texto.ToLower().Trim();
        return t is "si" or "sí" or "confirmo" or "confirmar" or "yes" or "ok" or "dale";
    }

    private static WhatsappConversation NuevaConversacion(WhatsappConversation conversacion, string? estado, string? contexto)
    {
        var nueva = new WhatsappConversation
        {
            Numero = conversacion.Numero,
            Estado = estado,
            ContextoJson = contexto,
            UltimoMensaje = DateTime.UtcNow.AddHours(-5)
        };
        return nueva;
    }
}