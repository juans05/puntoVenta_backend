namespace Application.Abstractions;

/// <summary>
/// Calcula los totales según el impuesto del país.
/// Strategy por país (D-D): Perú IGV, México IVA, Colombia IVA, Ecuador IVA.
/// </summary>
public interface ITaxCalculator
{
    decimal GetImpuestoRate(int? paisId, decimal? configurado);

    decimal CalcularSubtotal(decimal total, int? paisId, decimal? configurado);

    decimal CalcularImpuesto(decimal total, int? paisId, decimal? configurado);
}

public class PeruTaxCalculator : ITaxCalculator
{
    public decimal GetImpuestoRate(int? paisId, decimal? configurado) => configurado ?? 18m;

    public decimal CalcularSubtotal(decimal total, int? paisId, decimal? configurado)
    {
        var factor = 1m + (GetImpuestoRate(paisId, configurado) / 100m);
        return Math.Round(total / factor, 2);
    }

    public decimal CalcularImpuesto(decimal total, int? paisId, decimal? configurado)
        => total - CalcularSubtotal(total, paisId, configurado);
}

public class MexicoTaxCalculator : ITaxCalculator
{
    public decimal GetImpuestoRate(int? paisId, decimal? configurado) => configurado ?? 16m;

    public decimal CalcularSubtotal(decimal total, int? paisId, decimal? configurado)
    {
        var factor = 1m + (GetImpuestoRate(paisId, configurado) / 100m);
        return Math.Round(total / factor, 2);
    }

    public decimal CalcularImpuesto(decimal total, int? paisId, decimal? configurado)
        => total - CalcularSubtotal(total, paisId, configurado);
}

public class ColombiaTaxCalculator : ITaxCalculator
{
    public decimal GetImpuestoRate(int? paisId, decimal? configurado) => configurado ?? 19m;

    public decimal CalcularSubtotal(decimal total, int? paisId, decimal? configurado)
    {
        var factor = 1m + (GetImpuestoRate(paisId, configurado) / 100m);
        return Math.Round(total / factor, 2);
    }

    public decimal CalcularImpuesto(decimal total, int? paisId, decimal? configurado)
        => total - CalcularSubtotal(total, paisId, configurado);
}

public class EcuadorTaxCalculator : ITaxCalculator
{
    public decimal GetImpuestoRate(int? paisId, decimal? configurado) => configurado ?? 12m;

    public decimal CalcularSubtotal(decimal total, int? paisId, decimal? configurado)
    {
        var factor = 1m + (GetImpuestoRate(paisId, configurado) / 100m);
        return Math.Round(total / factor, 2);
    }

    public decimal CalcularImpuesto(decimal total, int? paisId, decimal? configurado)
        => total - CalcularSubtotal(total, paisId, configurado);
}

/// <summary>
/// Factoría de estrategias de impuesto por país. La configuración explicita
/// (ConfiguracionFiscal.PorcentajeImpuesto) siempre prevalece.
/// </summary>
public class TaxCalculatorFactory
{
    private static readonly Dictionary<int, ITaxCalculator> _strategies = new()
    {
        [604] = new PeruTaxCalculator(),     // Perú
        [484] = new MexicoTaxCalculator(),   // México
        [170] = new ColombiaTaxCalculator(), // Colombia
        [218] = new EcuadorTaxCalculator(),  // Ecuador
    };

    public ITaxCalculator GetCalculator(int? paisId)
    {
        if (paisId.HasValue && _strategies.TryGetValue(paisId.Value, out var calc))
            return calc;

        return new PeruTaxCalculator(); // default (Perú)
    }
}