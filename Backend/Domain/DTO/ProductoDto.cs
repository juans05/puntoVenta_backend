using Domain.Entities;
using System.Text.Json.Serialization;

namespace Domain.DTO;
public record class ProductoDto
{
    public int productoId { get; set; }
    public int Index { get; set; }
    public string? Nombre { get; set; }
    public decimal Precio { get; set; }
    [JsonIgnore]
    public Categoria? Categoria { get; set; }

    public int CategoriaId { get; set; }
    public string? NombreCategoria { get => Categoria == null ? null : Categoria.Nombre; }
    public int GrupoId { get; set; }
    public string? NombreGrupo { get => Grupo == null ? null : Grupo.Nombre; }
    public int ProveedorId { get; set; }
    [JsonIgnore]
    public Grupo? Grupo { get; set; }
    public Proveedor? Proveedor { get; set; }
    public string? CodigoBarra { get; set; }
    public decimal PrecioVentaSinInpuesto { get; set; }
    public decimal PrecioVentaConInpuesto { get; set; }
    public decimal MargenGanancia { get; set; }
    public bool CambioPrecioPermitido { get; set; }
    public int Stock { get; set; }
    public string? RutaImagen { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public string? Comentario { get; set; }
    //public List<ComprobanteDetalle>? ComprobanteDetalles { get; set; }
    public string? UsuarioCreacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool Estado { get; set; }



};

