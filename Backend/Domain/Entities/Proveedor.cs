namespace Domain.Entities
{
    public class Proveedor : EntityBase
    {
        public string Nombre { get; set; } = null!;
        public string? Dirección { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Celular { get; set; }
        public string? UbigeoId { get; set; }
        public Ubigeo? Ubigeo { get; set; }

        public List<Producto> Productos { get; set; } = new List<Producto>();

    }
}
