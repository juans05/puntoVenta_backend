namespace Domain.Payloads
{
    public class CreateCategoryPayload
    {
        public string Nombre { get; set; } = null!;
        public string? UsuarioCreacion { get; set; }
        public int RubroId { get; set; }

    }
}
