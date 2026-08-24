namespace Domain.Entities
{
    public class TipoDocumento : EntityBase //DNI, PASAPORTE, CE
    {
        public string Nombre { get; set; } = null!;

        public List<Cliente> Clientes { get; set; } = new List<Cliente>();
    }
}
