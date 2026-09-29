namespace Domain.Entities;

// Centro de costo: clasificacion interna de a que area/proyecto se le imputa una linea de compra
// o gasto (ej. "Almacen Central", "Administracion"). Catalogo simple por tenant, sin jerarquia --
// mismo criterio que TipoDetraccion/Departamento.
public class CentroCosto : EntityBase
{
    public string Nombre { get; set; } = null!;
}
