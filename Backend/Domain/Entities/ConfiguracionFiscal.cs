namespace Domain.Entities;

public class ConfiguracionFiscal : EntityBase
{
    public int? EmpresaId { get; set; }
    public string? Pais { get; set; }
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? UbigeoId { get; set; }
    public string? Departamento { get; set; }
    public string? Provincia { get; set; }
    public string? Distrito { get; set; }
    public string? SerieFactura { get; set; }
    public string? SerieBoleta { get; set; }
    public string? SerieNota { get; set; }
    public string? CodigoAdaptador { get; set; }
    public string? Token { get; set; }
    public string? Moneda { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public bool Activo { get; set; } = true;

    public Empresa Empresa { get; set; } = null!;
}