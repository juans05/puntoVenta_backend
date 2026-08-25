using Application.Interfaces.IRepository;
using AutoMapper;
using Domain.DTO;
using Domain.Entities;
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

        var productoIds = payload.Filas.Select(f => f.ProductoId).Distinct().ToList();
        var productosExistentes = await _context.Producto.AsNoTracking()
            .Where(p => productoIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var errores = new List<string>();
        var candidatas = new List<GastoPublicidad>();

        for (var i = 0; i < payload.Filas.Count; i++)
        {
            var fila = payload.Filas[i];

            if (!productosExistentes.Contains(fila.ProductoId))
            {
                errores.Add($"Fila {i + 1}: el producto {fila.ProductoId} no existe");
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
                ProductoId = fila.ProductoId,
                NombreAnuncio = fila.NombreAnuncio,
                NombreConjuntoAnuncios = fila.NombreConjuntoAnuncios,
                FechaInicio = fila.FechaInicio,
                FechaFin = fila.FechaFin,
                ImporteGastado = fila.ImporteGastado,
                Impresiones = fila.Impresiones,
                Alcance = fila.Alcance,
                Resultados = fila.Resultados,
                CostoPorResultado = fila.CostoPorResultado,
                LoteImportacionId = payload.LoteImportacionId,
                HashAnuncio = CalcularHash(fila.NombreAnuncio, fila.FechaInicio, fila.FechaFin)
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

    private static string CalcularHash(string nombreAnuncio, DateTime fechaInicio, DateTime fechaFin)
    {
        var input = $"{nombreAnuncio}|{fechaInicio:O}|{fechaFin:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
