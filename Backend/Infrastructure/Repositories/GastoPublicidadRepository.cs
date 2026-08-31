using Application.Interfaces.IRepository;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Repositories;

public class GastoPublicidadRepository : IGastoPublicidadRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;

    public GastoPublicidadRepository(SpaContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(ServiceStatus, ImportarGastoPublicidadResultDto?, string)> Importar(ImportarGastoPublicidadPayload payload)
    {
        if (payload.Filas == null || payload.Filas.Count == 0)
            return (ServiceStatus.FailedValidation, null, "No hay filas para importar");

        var grupoIds = payload.Filas.Where(f => f.GrupoId.HasValue).Select(f => f.GrupoId!.Value).Distinct().ToList();
        var gruposExistentes = await _context.Grupo.AsNoTracking()
            .Where(g => grupoIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync();

        var errores = new List<string>();
        var candidatas = new List<GastoPublicidad>();

        for (var i = 0; i < payload.Filas.Count; i++)
        {
            var fila = payload.Filas[i];

            // GrupoId null = "No aplica" (ad sin grupo de productos asociado), es válido.
            if (fila.GrupoId.HasValue && !gruposExistentes.Contains(fila.GrupoId.Value))
            {
                errores.Add($"Fila {i + 1}: el grupo {fila.GrupoId} no existe");
                continue;
            }

            if (fila.FechaFin < fila.FechaInicio)
            {
                errores.Add($"Fila {i + 1}: la fecha fin debe ser posterior o igual a la fecha inicio");
                continue;
            }

            if (fila.ImporteGastado < 0)
            {
                errores.Add($"Fila {i + 1}: el importe gastado no puede ser negativo");
                continue;
            }

            candidatas.Add(new GastoPublicidad
            {
                GrupoId = fila.GrupoId,
                Descartado = fila.Descartado,
                NombreAnuncio = fila.NombreAnuncio,
                NombreConjuntoAnuncios = fila.NombreConjuntoAnuncios,
                FechaInicio = fila.FechaInicio,
                FechaFin = fila.FechaFin,
                ImporteGastado = fila.ImporteGastado,
                Impresiones = fila.Impresiones,
                Alcance = fila.Alcance,
                Resultados = fila.Resultados,
                CostoPorResultado = fila.CostoPorResultado,
                Clics = fila.Clics,
                CostoPorClic = fila.CostoPorClic,
                LoteImportacionId = payload.LoteImportacionId,
                HashAnuncio = CalcularHash(fila.NombreAnuncio, fila.FechaInicio, fila.FechaFin, fila.GrupoId, fila.Descartado)
            });
        }

        if (errores.Count > 0)
            return (ServiceStatus.FailedValidation, null, string.Join(" | ", errores));

        try
        {
            var hashes = candidatas.Select(c => c.HashAnuncio).ToList();
            var hashesExistentes = await _context.GastoPublicidad.AsNoTracking()
                .Where(g => hashes.Contains(g.HashAnuncio))
                .Select(g => g.HashAnuncio)
                .ToListAsync();

            var aInsertar = candidatas.Where(c => !hashesExistentes.Contains(c.HashAnuncio)).ToList();
            var omitidas = candidatas.Count - aInsertar.Count;

            if (aInsertar.Count > 0)
            {
                await _context.GastoPublicidad.AddRangeAsync(aInsertar);
                await _context.SaveChangesAsync();
            }

            var resultado = new ImportarGastoPublicidadResultDto
            {
                FilasInsertadas = aInsertar.Count,
                FilasOmitidasPorDuplicado = omitidas
            };

            return (ServiceStatus.Ok, resultado, "Importación completada");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al importar publicidad -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    private static string CalcularHash(string nombreAnuncio, DateTime fechaInicio, DateTime fechaFin, int? grupoId, bool descartado)
    {
        var input = $"{nombreAnuncio}|{fechaInicio:O}|{fechaFin:O}|{grupoId?.ToString() ?? "null"}|{descartado}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public async Task<(ServiceStatus, List<RoiPorGrupoDto>?, string)> CalcularRoi(GastoPublicidadRoiQueryParams payload)
    {
        try
        {
            var query = _context.GastoPublicidad.AsNoTracking()
                .Include(g => g.Grupo)
                .Where(g => !g.Descartado)
                .AsQueryable();

            if (payload.Desde.HasValue)
                query = query.Where(g => g.FechaFin >= payload.Desde.Value);

            if (payload.Hasta.HasValue)
                query = query.Where(g => g.FechaInicio <= payload.Hasta.Value);

            if (payload.GrupoId.HasValue)
                query = query.Where(g => g.GrupoId == payload.GrupoId.Value);

            var ads = await query.ToListAsync();

            if (ads.Count == 0)
                return (ServiceStatus.Ok, new List<RoiPorGrupoDto>(), "Sin datos para el rango seleccionado");

            var resultado = new List<RoiPorGrupoDto>();

            foreach (var grupo in ads.GroupBy(a => a.GrupoId))
            {
                var minFecha = grupo.Min(a => a.FechaInicio);
                var maxFecha = grupo.Max(a => a.FechaFin);

                // Ventas de CUALQUIER producto que pertenezca a este grupo — una campaña
                // suele promocionar varias variantes/productos del mismo grupo a la vez.
                var detalles = await _context.ComprobanteDetalle.AsNoTracking()
                    .Where(d => d.Producto.GrupoId == grupo.Key
                             && (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion) >= minFecha
                             && (d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion) <= maxFecha
                             && d.ComprobanteCabecera.EstadoComprobante != EstatusComprobante.Anulado)
                    .Select(d => new
                    {
                        d.Cantidad,
                        d.ValorUnitarioTotal,
                        FechaVenta = d.ComprobanteCabecera.FechaVenta ?? d.ComprobanteCabecera.FechaCreacion,
                        CostoUnitario = d.Producto.CostoUnitario
                    })
                    .ToListAsync();

                // Cada venta cuenta una sola vez para el grupo aunque caiga dentro de
                // varios anuncios de ese mismo grupo que se solapan en fechas —
                // sumarla más de una vez inflaría el ingreso de una sola fila del reporte.
                var ventasEnRango = detalles
                    .Where(d => grupo.Any(a => d.FechaVenta >= a.FechaInicio && d.FechaVenta <= a.FechaFin))
                    .ToList();

                var gastoAds = grupo.Sum(a => a.ImporteGastado);
                var impresiones = grupo.Sum(a => a.Impresiones ?? 0);
                var alcance = grupo.Sum(a => a.Alcance ?? 0);
                var clics = grupo.Sum(a => a.Clics ?? 0);
                var costoPorClic = clics > 0 ? gastoAds / clics : (decimal?)null;
                var ingresos = ventasEnRango.Sum(d => d.ValorUnitarioTotal);
                var costoProducto = ventasEnRango.Sum(d => d.Cantidad * (d.CostoUnitario ?? 0));
                var utilidadNeta = ingresos - costoProducto - gastoAds;

                resultado.Add(new RoiPorGrupoDto
                {
                    GrupoId = grupo.Key,
                    NombreGrupo = grupo.Key.HasValue ? grupo.First().Grupo?.Nombre : "Sin grupo asignado",
                    GastoAds = gastoAds,
                    Impresiones = impresiones > 0 ? impresiones : null,
                    Alcance = alcance > 0 ? alcance : null,
                    Clics = clics > 0 ? clics : null,
                    CostoPorClic = costoPorClic,
                    Ingresos = ingresos,
                    CostoProducto = costoProducto,
                    UtilidadNeta = utilidadNeta,
                    RoiPorcentaje = gastoAds > 0 ? utilidadNeta / gastoAds : (decimal?)null
                });
            }

            return (ServiceStatus.Ok, resultado.OrderByDescending(r => r.RoiPorcentaje ?? decimal.MinValue).ToList(), "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al calcular ROI -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, List<MapeoAnuncioDto>?, string)> ObtenerMapeosAnuncios(ObtenerMapeosAnunciosPayload payload)
    {
        try
        {
            if (payload.NombresAnuncio == null || payload.NombresAnuncio.Count == 0)
                return (ServiceStatus.Ok, new List<MapeoAnuncioDto>(), "Succeeded");

            var nombres = payload.NombresAnuncio.Distinct().ToList();

            // Para cada anuncio, el Grupo (o "No aplica") que se le asignó la última vez
            // que se importó — evita que el usuario tenga que remapear anuncios recurrentes
            // cada vez que sube un Excel nuevo.
            var mapeos = await _context.GastoPublicidad.AsNoTracking()
                .Where(g => nombres.Contains(g.NombreAnuncio))
                .GroupBy(g => g.NombreAnuncio)
                .Select(g => new MapeoAnuncioDto
                {
                    NombreAnuncio = g.Key,
                    GrupoId = g.OrderByDescending(x => x.FechaFin).ThenByDescending(x => x.Id).First().GrupoId,
                    Descartado = g.OrderByDescending(x => x.FechaFin).ThenByDescending(x => x.Id).First().Descartado
                })
                .ToListAsync();

            return (ServiceStatus.Ok, mapeos, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener mapeos de anuncios -> {e.InnerException?.Message ?? e.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<GastoPublicidadDto>?, string)> Listar(GastoPublicidadQueryParams payload)
    {
        try
        {
            var query = _context.GastoPublicidad.AsNoTracking().Include(g => g.Grupo).AsQueryable();

            if (payload.GrupoId.HasValue)
                query = query.Where(g => g.GrupoId == payload.GrupoId.Value);

            if (payload.Desde.HasValue)
                query = query.Where(g => g.FechaFin >= payload.Desde.Value);

            if (payload.Hasta.HasValue)
                query = query.Where(g => g.FechaInicio <= payload.Hasta.Value);

            var lista = await query.OrderByDescending(g => g.Id)
                                   .ProjectTo<GastoPublicidadDto>(_mapper.ConfigurationProvider)
                                   .GetPagedAsync(payload.Page, payload.Amount);

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception e)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar publicidad -> {e.InnerException?.Message ?? e.Message}");
        }
    }
}
