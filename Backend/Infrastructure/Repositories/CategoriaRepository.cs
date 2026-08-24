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

public class CategoriaRepository : ICategoryRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;
    private readonly ITenantContextAccessor tenantContextAccessor;


    public CategoriaRepository(SpaContext dbContext, IMapper mapper, ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        this.tenantContextAccessor = tenantContextAccessor;

}

    public async Task<(ServiceStatus, CategoriaDto?, string)> CrearCategoria(CreateCategoryPayload payload)
    {
        try
        {
            //    var goods = new Producto { Nombre = goodsDto.Nombre, Precio = goodsDto.Precio };

            var entity = mapper.Map<Categoria>(payload);

            if (entity.RubroId <= 0)
                entity.RubroId = tenantContextAccessor.CurrentContext?.RubroId ?? 1;

            await dbContext.Categoria.AddAsync(entity);
            await dbContext.SaveChangesAsync();

            var maperCategoria = mapper.Map<CategoriaDto>(entity);

            return (ServiceStatus.Ok, maperCategoria, "Success");
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
        public async Task<(ServiceStatus, List<CategoriaDto>?, string)> GetCategoria(CategoryPayload payload)
        {

        //List<CategoriaDto> lista = null;
        List<Categoria> lista = null;
        var rubroId = tenantContextAccessor.CurrentContext?.RubroId;


        try
        {
                //var index = (payload.Page * payload.Amount) - payload.Amount + 1;

                if (payload.Value is null)
                {

                    lista = await dbContext.Categoria.AsNoTracking()
                        .Where(p => rubroId == null || p.RubroId == rubroId)
                        .ToListAsync();

                    lista.Insert(0, new Categoria { Id = 0, Estado = true, Nombre = "Todos", UsuarioCreacion = "ADMIN", FechaCreacion = DateTime.Now });

                //.ProjectTo<CategoriaDto>(mapper.ConfigurationProvider).ToListAsync();
                //.GetPagedAsync(payload.Page, payload.Amount);
            }
            else
                {
                    if (payload.Value.All(char.IsDigit))
                    {
                        lista = await dbContext.Categoria.AsNoTracking()
                            .Where(p => (rubroId == null || p.RubroId == rubroId) && p.Id == Convert.ToInt32(payload.Value)).ToListAsync();
                            //.ProjectTo<CategoriaDto>(mapper.ConfigurationProvider)
                            //.GetPagedAsync(payload.Page, payload.Amount);


                    }
                    else
                    {
                        lista = await dbContext.Categoria.AsNoTracking()
                            .Where(p => (rubroId == null || p.RubroId == rubroId) && p.Nombre.Contains(payload.Value)).ToListAsync();
                            //.ProjectTo<CategoriaDto>(mapper.ConfigurationProvider)
                            //.GetPagedAsync(payload.Page, payload.Amount);
                    }

                }


                if (lista.Count < 1) return (ServiceStatus.NotFound, null, "No hay registros para mostrar");

                var listaDto = mapper.Map<List<CategoriaDto>>(lista);


                return (ServiceStatus.Ok, listaDto, "Succeeded");

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

            var tieneGrupos = await dbContext.Grupo.AsNoTracking()
                                .AnyAsync(g => g.CategoriaId == CategoriaId);

            if (tieneGrupos)
                return (ServiceStatus.FailedValidation, null, "No se puede eliminar la categoría porque tiene grupos asociados");

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