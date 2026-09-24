namespace Domain.DTO;

// Un socio (cliente o proveedor) con su saldo pendiente.
public class SocioSaldoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Documento { get; set; }
    public decimal Saldo { get; set; }
    public decimal Vencido { get; set; }
    public int Documentos { get; set; }
}

// Un documento a credito con saldo (venta o compra).
public class DocumentoSaldoDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public string Fecha { get; set; } = null!;
    public string Vencimiento { get; set; } = null!;
    public decimal Total { get; set; }
    public decimal Saldo { get; set; }
    public int DiasVencido { get; set; }
}

public class AntiguedadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PorVencer { get; set; }
    public decimal Dias1a30 { get; set; }
    public decimal Dias31a60 { get; set; }
    public decimal Dias61a90 { get; set; }
    public decimal Mas90 { get; set; }
    public decimal Total { get; set; }
}

public class PagoCuentaDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = null!;
    public int SocioId { get; set; }
    public string? Socio { get; set; }
    public string Fecha { get; set; } = null!;
    public string? MetodoPago { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public string Estado { get; set; } = null!;
    public List<PagoCuentaDetalleDto> Detalle { get; set; } = new();
}

public class PagoCuentaDetalleDto
{
    public int DocumentoId { get; set; }
    public string Documento { get; set; } = null!;
    public decimal MontoAplicado { get; set; }
}
