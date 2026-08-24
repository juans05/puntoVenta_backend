namespace Domain.Payloads
{
    public class CreateGrupoPayload
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? UsuarioCreacion { get; set; }
    }
}
