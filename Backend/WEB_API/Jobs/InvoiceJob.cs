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

    private InvoiceRequest ArmarInvoice(ComprobanteCabecera comprobanteCabecera, ConfiguracionFiscal config)
    {

        var cliente = new Client
        {
            tipoDoc = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "0" : comprobanteCabecera.NumeroDocumento.Length == 11 ? "6" : "1",
            numDoc = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "00000000" : comprobanteCabecera.NumeroDocumento,
            rznSocial = string.IsNullOrEmpty(comprobanteCabecera.NumeroDocumento) ? "SIN NOMBRE" : comprobanteCabecera.RazonSocial,
            //address = null
        };

        var moneda = config.Moneda ?? "PEN";
        var impuesto = config.PorcentajeImpuesto > 0 ? config.PorcentajeImpuesto : 18m;
        var factor = 1m + (impuesto / 100m);

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
            details = comprobanteCabecera.ComprobanteDetalles.Select(x => new Detail
            {
                unidad = "NIU",
                codProducto = "P001",
                cantidad = x.Cantidad,
                descripcion = x.Producto.Nombre,
                mtoValorUnitario = Math.Round(x.ValorUnitario / factor, 2),
                mtoValorVenta = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                mtoBaseIgv = Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad,
                porcentajeIgv = impuesto,
                igv = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                tipAfeIgv = "10",
                totalImpuestos = (x.ValorUnitario * x.Cantidad) - (Math.Round(x.ValorUnitario / factor, 2) * x.Cantidad),
                mtoPrecioUnitario = x.ValorUnitario,
            }).ToList(),
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