namespace Domain.Entities
{

    public class Empresa
    {
        public int Id { get; set; }
        public string? Ruc { get; set; }
        public string? NombreComercial { get; set; }
        public string? RazonSocial { get; set; }
        public string? Direccion { get; set; }
        public string? Celular { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? SitioWeb { get; set; }
        public string? ImagenPortada { get; set; }
        public string? GifCarga { get; set; }
        public string? LogoSidebar { get; set; }
        public string? Logo { get; set; }
        public string? RegimenTributario { get; set; }
        public string? LogoCuadrado { get; set; }
        public string? LogoRectangular { get; set; }
        public string? Urbanizacion { get; set; }
        public string? Facebook { get; set; }
        public string? Instagram { get; set; }
        public string? Tiktok { get; set; }
        public string? Youtube { get; set; }
        public string? X { get; set; }
        public string? UbigeoId { get; set; }
        public Ubigeo? Ubigeo { get; set; }
        public List<EmpresaTenant> EmpresaTenants { get; set; } = new List<EmpresaTenant>();

    }
}