using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class GuiaRemisionRepositoryTests
{
    private static (GuiaRemisionRepository GuiaRepo, PedidoVentaRepository PedidoRepo, OrdenCompraRepository Config, SpaContext Context, System.Data.Common.DbConnection Connection) Preparar()
    {
        var (context, connection) = TestDbContextFactory.CreateContext();
        var guiaRepo = new GuiaRemisionRepository(context, httpContextAccessor: null);
        var pedidoRepo = new PedidoVentaRepository(context, httpContextAccessor: null, guiaRepo);
        var compra = new CompraRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);
        var config = new OrdenCompraRepository(context, compra, httpContextAccessor: null);
        return (guiaRepo, pedidoRepo, config, context, connection);
    }

    private static Task ConfigurarVentasCompleto(OrdenCompraRepository config)
        => config.GuardarConfiguracion(new ConfiguracionFlujoPayload
        {
            FlujoCompras = FlujoComprasModo.Simplificado,
            CruceFactura = CruceFacturaModo.Advertir,
            FlujoVentas = FlujoComprasModo.Completo
        });

    // Pedido confirmado + entrega activa lista, con un producto con stock de sobra.
    private static async Task<(int PedidoId, int EntregaId)> SeedPedidoConEntrega(PedidoVentaRepository pedidoRepo, SpaContext context, int cantidad = 5)
    {
        var producto = new Producto { Nombre = "Producto GRE", Precio = 10, Stock = 20, RestriccionEdad = 0 };
        context.Producto.Add(producto);
        await context.SaveChangesAsync();

        var (_, pedido, _) = await pedidoRepo.Crear(new CreatePedidoVentaPayload
        {
            NumeroDocumento = "20123456789", RazonSocial = "CLIENTE SAC", DireccionCliente = "AV LIMA 123",
            Detalle = new() { new() { ProductoId = producto.Id, Cantidad = cantidad, ValorUnitario = 10m } }
        });
        var (_, entregado, _) = await pedidoRepo.RegistrarEntrega(pedido!.Id, new CreateEntregaPayload
        {
            Placa = "ABC-123",
            Detalle = new() { new() { PedidoVentaDetalleId = pedido.Detalle[0].Id, Cantidad = cantidad } }
        });
        return (pedido.Id, entregado!.Entregas[0].Id);
    }

    [Fact]
    public async Task GenerarDesdeEntrega_CopiaClienteDireccionYPlaca()
    {
        var (guiaRepo, pedidoRepo, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentasCompleto(config);
        var (_, entregaId) = await SeedPedidoConEntrega(pedidoRepo, context);

        var (estado, guia, mensaje) = await guiaRepo.GenerarDesdeEntrega(entregaId);

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal("CLIENTE SAC", guia!.ClienteNombre);
        Assert.Equal("20123456789", guia.ClienteDocumento);
        Assert.Equal("AV LIMA 123", guia.DireccionLlegada);
        Assert.Equal("ABC-123", guia.Placa);
        Assert.Equal(EstadoGuiaRemision.Emitida, guia.EstadoGuia);
        Assert.Equal(5, Assert.Single(guia.Detalle).Cantidad);
    }

    [Fact]
    public async Task NoSePuedeGenerarDosGuiasActivasParaLaMismaEntrega()
    {
        var (guiaRepo, pedidoRepo, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentasCompleto(config);
        var (_, entregaId) = await SeedPedidoConEntrega(pedidoRepo, context);
        await guiaRepo.GenerarDesdeEntrega(entregaId);

        var (estado, _, mensaje) = await guiaRepo.GenerarDesdeEntrega(entregaId);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("ya tiene una guía", mensaje);
    }

    [Fact]
    public async Task AnularEntrega_ConGuiaActiva_EsRechazada()
    {
        var (guiaRepo, pedidoRepo, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentasCompleto(config);
        var (_, entregaId) = await SeedPedidoConEntrega(pedidoRepo, context);
        await guiaRepo.GenerarDesdeEntrega(entregaId);

        var (estado, _, mensaje) = await pedidoRepo.AnularEntrega(entregaId);

        Assert.Equal(ServiceStatus.FailedValidation, estado);
        Assert.Contains("guía de remisión activa", mensaje);
    }

    [Fact]
    public async Task AnularGuia_LiberaLaEntregaParaGenerarOtraVez()
    {
        var (guiaRepo, pedidoRepo, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentasCompleto(config);
        var (_, entregaId) = await SeedPedidoConEntrega(pedidoRepo, context);
        var (_, guia, _) = await guiaRepo.GenerarDesdeEntrega(entregaId);

        var (estadoAnular, anulada, _) = await guiaRepo.Anular(guia!.Id);
        Assert.Equal(ServiceStatus.Ok, estadoAnular);
        Assert.Equal(EstadoGuiaRemision.Anulada, anulada!.EstadoGuia);

        var (estadoNueva, nueva, mensaje) = await guiaRepo.GenerarDesdeEntrega(entregaId);
        Assert.True(estadoNueva == ServiceStatus.Ok, mensaje);
        Assert.NotEqual(guia.Id, nueva!.Id);

        // Ahora si tiene una guia activa (la nueva): anular la entrega vuelve a rechazarse.
        var (estadoAnularEntrega, _, mensajeEntrega) = await pedidoRepo.AnularEntrega(entregaId);
        Assert.Equal(ServiceStatus.FailedValidation, estadoAnularEntrega);
        Assert.Contains("guía de remisión activa", mensajeEntrega);
    }

    [Fact]
    public async Task Actualizar_CompletaDatosDeTransporte()
    {
        var (guiaRepo, pedidoRepo, config, context, connection) = Preparar();
        using var _ = connection; using var __ = context;
        await ConfigurarVentasCompleto(config);
        var (_, entregaId) = await SeedPedidoConEntrega(pedidoRepo, context);
        var (_, guia, _) = await guiaRepo.GenerarDesdeEntrega(entregaId);

        var (estado, actualizada, mensaje) = await guiaRepo.Actualizar(guia!.Id, new ActualizarGuiaRemisionPayload
        {
            ModTraslado = ModalidadTraslado.Publico,
            TransportistaRuc = "20999999999",
            TransportistaRazonSocial = "TRANSPORTES SAC",
            PesoTotal = 12.5m,
            UndPesoTotal = "KGM"
        });

        Assert.True(estado == ServiceStatus.Ok, mensaje);
        Assert.Equal("TRANSPORTES SAC", actualizada!.TransportistaRazonSocial);
        Assert.Equal(12.5m, actualizada.PesoTotal);
    }
}
