using Application.Abstractions;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Domain.Payloads;
using Newtonsoft.Json.Linq;
using System.Linq;
using Domain.Common;
using AutoMapper;
using Domain.Common.Utils;
using AutoMapper.QueryableExtensions;
using Application.Interfaces.IRepository;
using Domain.Entities;

namespace Infrastructure.Repositories;

public class CargaInicialRepository : ICargaInicialRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;


    public CargaInicialRepository(SpaContext dbContext, IMapper mapper)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;

}

    public async Task<(ServiceStatus, CategoriaDto?, string)> CrearDataInicialSegunRubro(int RubroId, string TenantId)
    {
        try
        {
            // Las plantillas del rubro son catálogo global (Rubro es global, no por tenant).
            // El query filter de tenant ocultaría las plantillas del rubro al registrar un 2º tenant,
            // por lo que se consultan con IgnoreQueryFilters y se elige la plantilla maestra del rubro.
            var plantillaTenantId = dbContext.Categoria.IgnoreQueryFilters()
                                    .Where(p => p.RubroId == RubroId)
                                    .GroupBy(p => p.TenantId)
                                    .OrderByDescending(g => g.Count())
                                    .Select(g => g.Key)
                                    .FirstOrDefault();

            if (string.IsNullOrEmpty(plantillaTenantId))
                return (ServiceStatus.FailedValidation, null, "No existe plantilla (Categoria) para el rubro indicado");

            #region Agregar Categoria
            var lista = await dbContext.Categoria.AsNoTracking().IgnoreQueryFilters()
                            .Where(p => p.RubroId == RubroId && p.TenantId == plantillaTenantId).ToListAsync();

            //seteamos el tenant Id
            lista.ForEach(p => { p.TenantId = TenantId; p.Id = 0; });

            await dbContext.Categoria.AddRangeAsync(lista);
            await dbContext.SaveChangesAsync();

            #endregion

            #region Agregar Grupo
            var listaGrupo = await dbContext.Grupo.AsNoTracking().IgnoreQueryFilters()
                            .Where(p => p.TenantId == plantillaTenantId && p.Categoria.RubroId == RubroId).ToListAsync();

            //seteamos el tenant Id
            listaGrupo.ForEach(p => { p.TenantId = TenantId; p.Id = 0; });

            await dbContext.Grupo.AddRangeAsync(listaGrupo);
            await dbContext.SaveChangesAsync();

            #endregion

            #region Agregar Productos
            var listaProductos = await dbContext.Producto.AsNoTracking().IgnoreQueryFilters()
                            .Where(p => p.TenantId == plantillaTenantId && p.Categoria.RubroId == RubroId).ToListAsync();

            //seteamos el tenant Id
            listaProductos.ForEach(p => { p.TenantId = TenantId; p.Id = 0; });

            await dbContext.Producto.AddRangeAsync(listaProductos);
            await dbContext.SaveChangesAsync();

            #endregion


            return (ServiceStatus.Ok, null, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Categoria -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

        //public async Task<IReadOnlyList<Producto>> GetAllAsync()
        //{
        //    return await dbContext.Producto.ToListAsync();
        //}
        public async Task<(ServiceStatus, List<CategoriaDto>?, string)> GetCategoriasRubro(int RubroId, string TenantId)
        {



        try
        {
            var lista = await dbContext.Categoria.AsNoTracking()
                                .Include(i => i.Grupos)
                                .ThenInclude(i => i.Productos)
                      .Where(p => p.RubroId == RubroId).ToListAsync();   
            

            //agregar automaper para devolver parametros

            var dtoCategoria = mapper.Map<List<CategoriaDto>>(lista);

            return (ServiceStatus.Ok, dtoCategoria, "Se encontraron registros");

        }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error al consultar Categoria -> {ex.InnerException?.Message ?? ex.Message}");
            }
    }

    public async Task<(ServiceStatus, CategoriaDto?, string)> UpdateCategory(UpdateCategoryPayload payload)
    {
        try
        {

            var entity = await dbContext.Categoria.AsNoTracking()
                            .FirstAsync(p => p.Id == payload.CategoriaId);

            var mapeoCategoria = mapper.Map(payload, entity);

            dbContext.Entry(entity).State = EntityState.Modified;

            //await dbContext.Categoria.AddAsync(mapeoCategoria);
            await dbContext.SaveChangesAsync();

            //mapeamos para devolver el dto categoria

            var mapCategoria = mapper.Map<CategoriaDto>(mapeoCategoria);
                
                ////borramos los comentarios
            //var comentarios = await dbContext.AsNoTracking().Where(p => p.ProductoId == payload.mapeoCategoria).ToListAsync();
            //dbContext.RemoveRange(comentarios);
            return (ServiceStatus.Ok, mapCategoria, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Categoria -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    public async Task<(ServiceStatus, Producto?, string)> DeleteCategory(int CategoriaId)
    {
        try
        {

            var categoria = await dbContext.Categoria.AsNoTracking()
                            .FirstAsync(p => p.Id == CategoriaId);

            dbContext.Remove(categoria);

            await dbContext.SaveChangesAsync();


            return (ServiceStatus.Ok, null, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Categoria -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

}