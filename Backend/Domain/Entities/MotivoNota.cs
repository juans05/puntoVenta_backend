namespace Domain.Entities;

public class MotivoNota : EntityBase
{
    public int TipoDocumentoVentaId { get; set; } // 4 = NotaCredito, 5 = NotaDebito
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public bool RevierteStock { get; set; }
}
