namespace Domain.Entities;

public class RentaDetalle : EntityBase
{
    public int? SucursalId { get; set; }

    public int RentaId { get; set; }

    public int? ProductoId { get; set; }

    public string? NombreProducto { get; set; }

    public string? RutaImagen { get; set; }

    public decimal Precio { get; set; }

    public Renta? Renta { get; set; }
}