using Application.Interfaces.IProxies;
using Application.Interfaces.IServices;
using AutoMapper;
using Coravel.Invocable;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Newtonsoft.Json;
using System;

namespace Api.Jobs;

public class InvoiceJob : IInvocable
{
    private readonly IServiceProvider provider;
    private readonly IFacturacionProxy _facturacionProxy;
    private readonly IMapper _mapper;


    public InvoiceJob(IServiceProvider provider, IFacturacionProxy facturacionProxy, IMapper mapper)
    {
        this.provider = provider;
        _facturacionProxy = facturacionProxy;
        _mapper = mapper;
    }

    public async Task Invoke()
    {
        using (var scope = provider.CreateScope())
        {
            var generalService = scope.ServiceProvider.GetService<IComprobanteService>();

            await Core(generalService);
        }
    }

    public async Task Core(IComprobanteService comprobanteService)
    {
        try
        {
            //JOB reitera por tenant/configuración fiscal activa
            var configsResult = await comprobanteService.ObtenerConfiguracionesFiscalesActivas();
            var configs = configsResult.Data;

            if (configs == null || configs.Count == 0) return;

            foreach (var config in configs)
            {
                var tenant = config.TenantId;

                var comprobantesPendientes = await comprobanteService.ListarComprobantesPendientesEnviarSunat(tenant);

                if (comprobantesPendientes.Data != null && comprobantesPendientes.Data.Count >= 1)
                {
                    foreach (var comprobante in comprobantesPendientes.Data)
                    {
                        if (comprobante.TipoDocumentoVentaId == (int)TipoComprobante.NotaCredito
                            || comprobante.TipoDocumentoVentaId == (int)TipoComprobante.NotaDebito)
                        {
                            var notaRequest = ArmarNota(comprobante, config);

                            var notaResponse = await _facturacionProxy.EnviarNotaSunar<InvoiceResponse>(notaRequest, config.Token);

                            if (notaResponse.sunatResponse.success)
                            {
                                await comprobanteService.ActualizarComprobanteAEnviado(comprobante.Id, JsonConvert.SerializeObject(notaResponse.sunatResponse.cdrResponse.notes));
                            }
                            else
                            {
                                await comprobanteService.ActualizarComprobanteAError(comprobante.Id, notaResponse.sunatResponse.error.message);
                            }

                            continue;
                        }

                        var request = ArmarInvoice(comprobante, config);

                        var response = await _facturacionProxy.EnviarComprobanteSunar<InvoiceResponse>(request, config.Token);

                        if (response.sunatResponse.success)
                        {
                            await comprobanteService.ActualizarComprobanteAEnviado(comprobante.Id, JsonConvert.SerializeObject(response.sunatResponse.cdrResponse.notes));
                        }
                        else
                        {
                            await comprobanteService.ActualizarComprobanteAError(comprobante.Id, response.sunatResponse.error.message);
                        }
                    }
                }

                var comprobantesAnulados = await comprobanteService.ListarComprobantesAnulados(tenant);

                if (comprobantesAnulados.Data != null && comprobantesAnulados.Data.Count >= 1)
                {
                    foreach (var comprobante in comprobantesAnulados.Data)
                    {

                        var correlativo = await comprobanteService.ObtenerCorrelativoAnulacion(tenant);

                        var request = ArmarVoided(comprobante, correlativo.Data.Correlativo.ToString().PadLeft(5, '0'), config);

                        var response = await _facturacionProxy.ResumenAnulacion<InvoiceResponse>(request, config.Token);

                        if (response.sunatResponse.success && !string.IsNullOrEmpty(response.sunatResponse.ticket))
                        {
                            await comprobanteService.ActualizarComprobanteAAnuladoDesdeSunat(comprobante.Id, response.sunatResponse.ticket);

                            await comprobanteService.ActualizarCorrelativoAnulacion(correlativo.Data.Id);
                        }
                    }

                    //TODO
                    //JOB QUE VERIFIQUE QUE EL TICKET HAYA PASADO CORRECTAMENTE
                    //else
                    //{
                    //    await comprobanteService.ActualizarComprobanteAError(comprobante.Id, response.sunatResponse.error.message);
                    //}
                }
            }

        }
        catch (Exception) { return; }

    }

    private Client ArmarCliente(ComprobanteCabecera comprobanteCabecera) => new Client
    {
        tipoDoc = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "0" : comprobanteCabecera.NumeroDocumento.Length == 11 ? "6" : "1",
        numDoc = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "00000000" : comprobanteCabecera.NumeroDocumento,
        rznSocial = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "SIN NOMBRE" : comprobanteCabecera.RazonSocial,
    };

    private Company ArmarCompany(ConfiguracionFiscal config) => new Company
    {
        ruc = long.Parse(config.Ruc ?? "0"),
        razonSocial = config.RazonSocial ?? "EMPRESA NO CONFIGURADA",
        nombreComercial = config.NombreComercial ?? config.RazonSocial,
        address = new Address
        {
            ubigueo = config.UbigeoId ?? "000000",
            departamento = config.Departamento ?? "",
            provincia = config.Provincia ?? "",
            distrito = config.Distrito ?? "",
            direccion = config.Direccion ?? ""
        }
    };

    // Usa los valores ya calculados y persistidos por linea (ValorUnitarioTotal/ValorIgv, que en
    // ComprobanteRepository.CrearComprobante ya tienen en cuenta el TipoIgv de cada linea) en vez
    // de recalcular aqui con el factor global -- evita duplicar la logica de exoneracion/inafectacion.
    private List<Detail> ArmarDetails(ComprobanteCabecera comprobanteCabecera, decimal impuesto, decimal factor) =>
        comprobanteCabecera.ComprobanteDetalles.Select(x =>
        {
            var mtoBaseIgv = x.ValorUnitarioTotal - x.ValorIgv;
            var aplicaImpuesto = x.TipoIgv?.AplicaPorcentajeImpuesto ?? true;

            return new Detail
            {
                unidad = x.UnidadMedida?.Codigo ?? "NIU",
                codProducto = "P001",
                cantidad = x.Cantidad,
                descripcion = x.Producto.Nombre,
                mtoValorUnitario = Math.Round(mtoBaseIgv / x.Cantidad, 2),
                mtoValorVenta = mtoBaseIgv,
                mtoBaseIgv = mtoBaseIgv,
                porcentajeIgv = aplicaImpuesto ? impuesto : 0m,
                igv = x.ValorIgv,
                tipAfeIgv = x.TipoIgv?.Codigo ?? "10",
                totalImpuestos = x.ValorIgv,
                mtoPrecioUnitario = x.ValorUnitario,
            };
        }).ToList();

    private InvoiceRequest ArmarInvoice(ComprobanteCabecera comprobanteCabecera, ConfiguracionFiscal config)
    {

        var cliente = ArmarCliente(comprobanteCabecera);

        var moneda = config.Moneda ?? "PEN";
        var impuesto = config.PorcentajeImpuesto > 0 ? config.PorcentajeImpuesto : 18m;
        var factor = 1m + (impuesto / 100m);

        var company = ArmarCompany(config);

        var serie = comprobanteCabecera.TipoDocumentoVentaId == (int)TipoComprobante.Factura
            ? (config.SerieFactura ?? "F001")
            : (config.SerieBoleta ?? "B001");

        string fechaHoraFormateada = comprobanteCabecera.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:sszzz");

        var invoice = new InvoiceRequest
        {
            ublVersion = "2.1",
            tipoOperacion = "0101",
            tipoDoc = comprobanteCabecera.TipoDocumentoVentaId == (int)TipoComprobante.Factura ? "01" : "03",
            serie = serie,
            correlativo = comprobanteCabecera.Correlativo.ToString().PadLeft(7, '0'),
            fechaEmision = fechaHoraFormateada,
            formaPago = new FormaPago { tipo = "Contado", moneda = moneda },
            tipoMoneda = moneda,
            client = cliente,
            company = company,
            subTotal = comprobanteCabecera.ValorTotal,
            mtoImpVenta = comprobanteCabecera.ValorTotal,
            mtoOperGravadas = comprobanteCabecera.ValorSubtotal,
            valorVenta = comprobanteCabecera.ValorSubtotal,
            mtoIGV = comprobanteCabecera.ValorIgv,
            totalImpuestos = comprobanteCabecera.ValorIgv,
            details = ArmarDetails(comprobanteCabecera, impuesto, factor),
            legends = new List<Legend>
            {
                new Legend
                {
                    code = "1000",
                    value = comprobanteCabecera.TotalLetras
                }
            }
        };

        return invoice;

    }

    private NoteRequest ArmarNota(ComprobanteCabecera comprobanteCabecera, ConfiguracionFiscal config)
    {
        var cliente = ArmarCliente(comprobanteCabecera);

        var moneda = config.Moneda ?? "PEN";
        var impuesto = config.PorcentajeImpuesto > 0 ? config.PorcentajeImpuesto : 18m;
        var factor = 1m + (impuesto / 100m);

        var company = ArmarCompany(config);

        var afectado = comprobanteCabecera.ComprobanteAfectado;
        var motivo = comprobanteCabecera.MotivoNota;

        var serieAfectado = afectado.TipoDocumentoVentaId == (int)TipoComprobante.Factura
            ? (config.SerieFactura ?? "F001")
            : (config.SerieBoleta ?? "B001");

        var serie = comprobanteCabecera.TipoDocumentoVentaId == (int)TipoComprobante.NotaCredito
            ? (config.SerieNotaCredito ?? "FC01")
            : (config.SerieNotaDebito ?? "FD01");

        string fechaHoraFormateada = comprobanteCabecera.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:sszzz");

        var nota = new NoteRequest
        {
            ublVersion = "2.1",
            tipoDoc = comprobanteCabecera.TipoDocumentoVentaId == (int)TipoComprobante.NotaCredito ? "07" : "08",
            tipoDocAfectado = afectado.TipoDocumentoVentaId == (int)TipoComprobante.Factura ? "01" : "03",
            numDocfectado = $"{serieAfectado}-{afectado.Correlativo.ToString().PadLeft(7, '0')}",
            codMotivo = motivo?.Codigo ?? "01",
            desMotivo = motivo?.Descripcion ?? "",
            serie = serie,
            correlativo = comprobanteCabecera.Correlativo.ToString().PadLeft(7, '0'),
            fechaEmision = fechaHoraFormateada,
            formaPago = new FormaPago { tipo = "Contado", moneda = moneda },
            tipoMoneda = moneda,
            client = cliente,
            company = company,
            subTotal = comprobanteCabecera.ValorTotal,
            mtoImpVenta = comprobanteCabecera.ValorTotal,
            mtoOperGravadas = comprobanteCabecera.ValorSubtotal,
            valorVenta = comprobanteCabecera.ValorSubtotal,
            mtoIGV = comprobanteCabecera.ValorIgv,
            totalImpuestos = comprobanteCabecera.ValorIgv,
            details = ArmarDetails(comprobanteCabecera, impuesto, factor),
            legends = new List<Legend>
            {
                new Legend
                {
                    code = "1000",
                    value = comprobanteCabecera.TotalLetras
                }
            }
        };

        return nota;
    }


    private SummaryRequest ArmarVoided(ComprobanteCabecera comprobante, string correlativo, ConfiguracionFiscal config)
    {

        var company = new Company
        {
            ruc = long.Parse(config.Ruc ?? "0"),
            razonSocial = config.RazonSocial ?? "EMPRESA NO CONFIGURADA",
            nombreComercial = config.NombreComercial ?? config.RazonSocial,
            address = new Address
            {
                ubigueo = config.UbigeoId ?? "000000",
                departamento = config.Departamento ?? "",
                provincia = config.Provincia ?? "",
                distrito = config.Distrito ?? "",
                direccion = config.Direccion ?? ""
            }
        };

        var serie = comprobante.TipoDocumentoVentaId == (int)TipoComprobante.Factura
            ? (config.SerieFactura ?? "F001")
            : (config.SerieBoleta ?? "B001");

        List<Detaile> detalles = new List<Detaile>
        {
            new Detaile
            {
                tipoDoc = comprobante.TipoDocumentoVentaId == (int)TipoComprobante.Factura ? "01" : "03",
                serieNro = $"{serie}-{comprobante.Correlativo.ToString().PadLeft(7, '0')}",
                estado = "3",
                clienteTipo = "1",
                clienteNro = string.IsNullOrEmpty(comprobante.NumeroDocumento) ? "00000000" : comprobante.NumeroDocumento,
                total = comprobante.ValorTotal,
                mtoOperGravadas = comprobante.ValorSubtotal,
                mtoOperInafectas = 0,
                mtoOperExoneradas = 0,
                mtoOperExportacion = 0,
                mtoOtrosCargos = 0,
                mtoIGV = comprobante.ValorIgv,
            }
        };

        var voided = new SummaryRequest
        {
            fecGeneracion = comprobante.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            fecResumen = DateTime.UtcNow.AddHours(-5).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            correlativo = correlativo,
            moneda = config.Moneda ?? "PEN",
            company = company,
            details = detalles
        };

        return voided;

    }

}