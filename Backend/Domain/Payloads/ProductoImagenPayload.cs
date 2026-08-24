namespace Domain.Payloads
{
    public class ProductoImagenPayload
    {
        public int ProductoId { get; set; }
        public string NombreArchivo { get; set; } = null!;
        public string? TipoContenido { get; set; }
        public byte[] Contenido { get; set; } = Array.Empty<byte>();
    }
}