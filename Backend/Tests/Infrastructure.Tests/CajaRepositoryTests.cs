using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class CajaRepositoryTests
{
    private const string Usuario = "CAJERO1";

    private static async Task<int> SeedTipoDocumentoVentaAsync(SpaContext context)
    {
        context.TipoDocumentoVenta.Add(new TipoDocumentoVenta { Id = 1, Nombre = "Boleta" });
        await context.SaveChangesAsync();
        return 1;
    }

    private static async Task<int> SeedMetodoPagoAsync(SpaContext context)
    {
        var metodo = new Metodopago { Nombre = "Efectivo" };
        context.Metodopago.Add(metodo);
        await context.SaveChangesAsync();
        return metodo.Id;
    }

    private static async Task<ComprobanteCabecera> SeedComprobanteAsync(SpaContext context, int tipoDocumentoVentaId, char estado, decimal total)
    {
        var cabecera = new ComprobanteCabecera
        {
            TipoDocumentoVentaId = tipoDocumentoVentaId,
            Serie = "B001",
            Correlativo = 1,
            ValorTotal = total,
            EstadoComprobante = estado,
        };
        context.ComprobanteCabecera.Add(cabecera);
        await context.SaveChangesAsync();
        return cabecera;
    }

    [Fact]
    public async Task ReporteCajaResumido_ExcluyeVentasAnuladasDelTotal()
    {
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: Usuario));
        using var _ = connection;

        var tipoDocId = await SeedTipoDocumentoVentaAsync(context);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var ventaValida = await SeedComprobanteAsync(context, tipoDocId, EstatusComprobante.Creado, 50m);
        context.Pago.Add(new Pago { ComprobanteCabeceraId = ventaValida.Id, MetodoPagoId = metodoPagoId, Monto = 50m });

        var ventaAnulada = await SeedComprobanteAsync(context, tipoDocId, EstatusComprobante.Anulado, 999m);
        context.Pago.Add(new Pago { ComprobanteCabeceraId = ventaAnulada.Id, MetodoPagoId = metodoPagoId, Monto = 999m });

        await context.SaveChangesAsync();

        var repo = new CajaRepository(context, TestDbContextFactory.Mapper, httpContextAccessor: null);

        var fecha = DateTime.UtcNow.AddHours(-5).ToString("yyyy-MM-dd");
        var (estado, resultado, _) = await repo.ReporteCajaResumido("TODOS", fecha);

        Assert.Equal(ServiceStatus.Ok, estado);

        // objeto anonimo: { boletas, facturas, ticketInterno, total: { cantidad, efectivo, tarjeta, yape, total } }
        var totalGroup = resultado!.GetType().GetProperty("total")!.GetValue(resultado)!;
        var total = (decimal)totalGroup.GetType().GetProperty("total")!.GetValue(totalGroup)!;
        var cantidad = (int)totalGroup.GetType().GetProperty("cantidad")!.GetValue(totalGroup)!;

        Assert.Equal(50m, total);
        Assert.Equal(1, cantidad); // solo la venta valida, la anulada no cuenta
    }

    [Fact]
    public async Task CerrarCaja_ExcluyePagosDeVentasAnuladasDelMontoCierre()
    {
        var httpContextAccessor = new FakeHttpContextAccessor(Usuario);
        var (context, connection) = TestDbContextFactory.CreateContext(new FakeTenantResolver(username: Usuario));
        using var _ = connection;

        var tipoDocId = await SeedTipoDocumentoVentaAsync(context);
        var metodoPagoId = await SeedMetodoPagoAsync(context);

        var caja = new Caja { MontoInicio = 100m, MontoCierre = 0m };
        context.Caja.Add(caja);
        await context.SaveChangesAsync();

        var ventaValida = await SeedComprobanteAsync(context, tipoDocId, EstatusComprobante.Creado, 30m);
        context.Pago.Add(new Pago { ComprobanteCabeceraId = ventaValida.Id, MetodoPagoId = metodoPagoId, Monto = 30m });

        var ventaAnulada = await SeedComprobanteAsync(context, tipoDocId, EstatusComprobante.Anulado, 500m);
        context.Pago.Add(new Pago { ComprobanteCabeceraId = ventaAnulada.Id, MetodoPagoId = metodoPagoId, Monto = 500m });

        await context.SaveChangesAsync();

        var repo = new CajaRepository(context, TestDbContextFactory.Mapper, httpContextAccessor);

        var (estado, resultado, _) = await repo.CerrarCaja();

        Assert.Equal(ServiceStatus.Ok, estado);

        var pagoDto = Assert.IsType<Domain.DTO.PagoDto>(resultado);
        Assert.Equal(130m, pagoDto.MontoCierre); // 100 inicio + 30 de la venta valida, sin los 500 de la anulada
        Assert.Equal(30m, pagoDto.MontoEfectivo);
    }
}
