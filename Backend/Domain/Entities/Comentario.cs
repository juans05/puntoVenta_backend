
namespace Domain.Entities
{
    public class Comentario : EntityBase
    {
        public int? SucursalId { get; set; }
        public int Item { get; set; }
        public string Descripcion { get; set; } = null!;
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
    }
}
