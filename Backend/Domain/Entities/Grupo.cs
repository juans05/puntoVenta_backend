namespace Domain.Entities
{
    public class Grupo : EntityBase
    {
        public string Nombre { get; set; } = null!;
        public int? SucursalId { get; set; }
        public int? CategoriaId { get; set; }
        public Categoria? Categoria { get; set; } = null!;
        public List<Producto>? Productos { get; set; } = new List<Producto>();

    }
}
