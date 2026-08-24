namespace Domain.DTO;


public class ApplicationUserDto
{
    public List<AccesosDetalle> Resumen { get; set; }

}

public class AccesosDetalle
{
    public string Modulo { get; set; }
    public string ModuloNombre { get; set; }
    public List<SubModuloDetalle> SubModulos { get; set; }
}

public class SubModuloDetalle
{
    public string SubModulo { get; set; }
    public string SubModuloNombre { get; set; }
}


