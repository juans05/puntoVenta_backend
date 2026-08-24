namespace Domain.Entities;

public class CorrelativoAnulacion : EntityBase
{
    public int? SucursalId { get; set; }
    public int Correlativo { get; set; }
}
