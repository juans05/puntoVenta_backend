namespace Domain.Payloads
{
    public class UpdateCategoryPayload
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? UsuarioMofificacion { get; set; }
    }
}
