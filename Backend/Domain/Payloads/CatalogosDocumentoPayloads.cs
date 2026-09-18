namespace Domain.Payloads;

// Payloads de CRUD para los catalogos "base SUNAT + personalizables por tenant"
// (TipoIgv, UnidadMedida, TipoOperacion, MotivoNota, Moneda). Create/Update comparten
// forma porque son los mismos campos editables; se separan en dos clases por entidad
// para que cada endpoint tenga su propio tipo (mas facil de versionar a futuro).

public class CreateTipoIgvPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public bool AplicaPorcentajeImpuesto { get; set; }
}

public class UpdateTipoIgvPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public bool AplicaPorcentajeImpuesto { get; set; }
}

public class CreateUnidadMedidaPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
}

public class UpdateUnidadMedidaPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
}

public class CreateTipoOperacionPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
}

public class UpdateTipoOperacionPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
}

public class CreateMotivoNotaPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public bool RevierteStock { get; set; }
    public int TipoDocumentoVentaId { get; set; }
}

public class UpdateMotivoNotaPayload
{
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public bool RevierteStock { get; set; }
    public int TipoDocumentoVentaId { get; set; }
}

public class CreateMonedaPayload
{
    public string Codigo { get; set; } = null!;
    public string Simbolo { get; set; } = null!;
    public string Locale { get; set; } = null!;
    public int PaisId { get; set; }
}

public class UpdateMonedaPayload
{
    public string Codigo { get; set; } = null!;
    public string Simbolo { get; set; } = null!;
    public string Locale { get; set; } = null!;
    public int PaisId { get; set; }
}
