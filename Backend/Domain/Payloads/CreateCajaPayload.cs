using System.ComponentModel.DataAnnotations;

namespace Domain.Payloads
{
    public class CreateCajaPayload
    {
        [Required]
        public string Nombre { get; set; } = null!;

        public int? SucursalId { get; set; }
    }
}