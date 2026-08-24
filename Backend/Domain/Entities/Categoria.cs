namespace Domain.Entities
{
    public class Categoria : EntityBase
    {
        public string Nombre { get; set; } = null!;
        public int? SucursalId { get; set; }
        public List<Grupo>? Grupos { get; set; } = new List<Grupo>();

        public int RubroId { get; set; }
        public Rubro Rubro { get; set; }

    }
}
