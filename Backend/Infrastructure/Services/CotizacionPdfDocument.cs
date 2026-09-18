using Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services;

// PDF propio para Cotizacion (no pasa por el microservicio de facturacion SUNAT: ese servicio
// solo entiende tipoDoc fiscales 01/03/07/08, y una cotizacion no es un comprobante tributario).
public class CotizacionPdfDocument : IDocument
{
    private readonly ComprobanteCabecera _cotizacion;
    private readonly ConfiguracionFiscal? _config;

    public CotizacionPdfDocument(ComprobanteCabecera cotizacion, ConfiguracionFiscal? config)
    {
        _cotizacion = cotizacion;
        _config = config;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(_config?.RazonSocial ?? "EMPRESA").FontSize(14).Bold();
                        c.Item().Text(_config?.NombreComercial ?? "");
                        c.Item().Text(_config?.Direccion ?? "");
                        c.Item().Text($"RUC: {_config?.Ruc ?? "-"}");
                    });
                    row.ConstantItem(160).Border(1).Padding(8).Column(c =>
                    {
                        c.Item().AlignCenter().Text("COTIZACIÓN").Bold();
                        c.Item().AlignCenter().Text($"{_cotizacion.Serie}-{_cotizacion.Correlativo.ToString().PadLeft(7, '0')}");
                    });
                });
                col.Item().PaddingTop(10).LineHorizontal(1);
            });

            page.Content().PaddingVertical(15).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Cliente: {_cotizacion.RazonSocial ?? "SIN NOMBRE"}");
                    row.RelativeItem().AlignRight().Text($"Fecha: {_cotizacion.FechaCreacion:dd/MM/yyyy}");
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Documento: {_cotizacion.NumeroDocumento ?? "-"}");
                    row.RelativeItem().AlignRight().Text(
                        _cotizacion.FechaVigencia.HasValue
                            ? $"Válida hasta: {_cotizacion.FechaVigencia.Value:dd/MM/yyyy}"
                            : "Sin fecha de vigencia");
                });

                col.Item().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Producto");
                        header.Cell().Element(HeaderCell).AlignCenter().Text("Cant.");
                        header.Cell().Element(HeaderCell).AlignRight().Text("P. Unit.");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");

                        static IContainer HeaderCell(IContainer c) => c.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1);
                    });

                    foreach (var item in _cotizacion.ComprobanteDetalles)
                    {
                        table.Cell().PaddingVertical(4).Text(item.Producto?.Nombre ?? "-");
                        table.Cell().PaddingVertical(4).AlignCenter().Text(item.Cantidad.ToString());
                        table.Cell().PaddingVertical(4).AlignRight().Text($"S/ {item.ValorUnitario:0.00}");
                        table.Cell().PaddingVertical(4).AlignRight().Text($"S/ {item.ValorUnitarioTotal:0.00}");
                    }
                });

                col.Item().PaddingTop(10).AlignRight().Column(c =>
                {
                    c.Item().Text($"Subtotal: S/ {_cotizacion.ValorSubtotal:0.00}");
                    c.Item().Text($"IGV: S/ {_cotizacion.ValorIgv:0.00}");
                    c.Item().Text($"Total: S/ {_cotizacion.ValorTotal:0.00}").Bold().FontSize(12);
                });

                if (!string.IsNullOrWhiteSpace(_cotizacion.Observacion))
                    col.Item().PaddingTop(10).Text($"Observación: {_cotizacion.Observacion}");
            });

            page.Footer().AlignCenter().Text("Documento sin validez tributaria — no es un comprobante de pago SUNAT.").Italic().FontSize(8);
        });
    }
}
