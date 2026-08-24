using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Application.Abstractions;
using Application.Interfaces.IRepository;
using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class GrupoRepository : IGrupoRepository
    {
        private readonly SpaContext dbContext;
        private readonly IMapper mapper;
        private readonly ITenantContextAccessor tenantContextAccessor;


        public GrupoRepository(SpaContext dbContext, IMapper mapper, ITenantContextAccessor tenantContextAccessor)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.tenantContextAccessor = tenantContextAccessor;

        }

        public async Task<(ServiceStatus, GrupoDto?, string)> CrearGrupo(CreateGrupoPayload payload)
        {
            try
            {
                //    var goods = new Producto { Nombre = goodsDto.Nombre, Precio = goodsDto.Precio };

                var entity = mapper.Map<Grupo>(payload);

                await dbContext.Grupo.AddAsync(entity);
                await dbContext.SaveChangesAsync();

                var maper = mapper.Map<GrupoDto>(entity);

                return (ServiceStatus.Ok, maper, "Success");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.FailedValidation, null, $"Error Grupo -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        //public async Task<IReadOnlyList<Producto>> GetAllAsync()
        //{
        //    return await dbContext.Producto.ToListAsync();
        //}
        public async Task<(ServiceStatus, List<GrupoDto>?, string)> GetGrupo(GrupoPayload payload)
        {

            //DataCollection<GrupoDto> lista = null;
            List<Grupo> lista = null;
            var rubroId = tenantContextAccessor.CurrentContext?.RubroId;

            try
            {
                //var index = (payload.Page * payload.Amount) - payload.Amount + 1;

                payload.CategoriaId = payload.CategoriaId == 0 ? null : payload.CategoriaId;

                if (payload.Value is null)
                {

                    lista = await dbContext.Grupo.AsNoTracking()
                        .Where(p => (rubroId == null || p.Categoria!.RubroId == rubroId) &&
                               payload.CategoriaId == null ? 
                               p.CategoriaId == p.CategoriaId : 
                               p.CategoriaId == payload.CategoriaId 
                               )
                        .ToListAsync();
                        //.ProjectTo<GrupoDto>(mapper.ConfigurationProvider)
                        //.GetPagedAsync(payload.Page, payload.Amount);
                }
                else
                {
                    if (payload.Value.All(char.IsDigit))
                    {
                        lista = await dbContext.Grupo.AsNoTracking()
                            .Where(p => (rubroId == null || p.Categoria!.RubroId == rubroId) &&
                                p.Id == Convert.ToInt32(payload.Value) &&
                               payload.CategoriaId == null ?
                               p.CategoriaId == p.CategoriaId :
                               p.CategoriaId == payload.CategoriaId)
                            .ToListAsync();
                            //.ProjectTo<GrupoDto>(mapper.ConfigurationProvider)
                            //.GetPagedAsync(payload.Page, payload.Amount);


                    }
                    else
                    {
                        lista = await dbContext.Grupo.AsNoTracking()
                            .Where(p => (rubroId == null || p.Categoria!.RubroId == rubroId) &&
                                p.Nombre.Contains(payload.Value) &&
                               payload.CategoriaId == null ?
                               p.CategoriaId == p.CategoriaId :
                               p.CategoriaId == payload.CategoriaId)
                            .ToListAsync();
                            //.ProjectTo<GrupoDto>(mapper.ConfigurationProvider)
                            //.GetPagedAsync(payload.Page, payload.Amount);
                    }

                }

                if (lista.Count < 1) return (ServiceStatus.NotFound, null, "No hay registros para mostrar");

                var listaDto = mapper.Map<List<GrupoDto>>(lista);

                //if(payload.CategoriaId == null)
                listaDto.Insert(0, new GrupoDto { GrupoId = 0,  CategoriaId = 0, Estado = true, Nombre = "Todos", UsuarioCreacion = "ADMIN", FechaCreacion = DateTime.Now });


                return (ServiceStatus.Ok, listaDto, "Succeeded");

            }
            catch (Exception ex)
            {
                return (ServiceStatus.InternalError, null, $"Error al consultar Categoria -> {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<(ServiceStatus, GrupoDto?, string)> UpdateGrupo(UpdateGrupoPayload payload)
        {
            try
            {

                var entity = await dbContext.Grupo.AsNoTracking()
                                .FirstAsync(p => p.Id == payload.GrupoId);

                var mapeoCategoria = mapper.Map(payload, entity);

                dbContext.Entry(entity).State = EntityState.Modified;

                //await dbContext.Categoria.AddAsync(mapeoCategoria);
                await dbContext.SaveChangesAsync();

                //mapeamos para devolver el dto categoria

                var mapCategoria = mapper.Map<GrupoDto>(mapeoCategoria);

                ////borramos los comentarios
                //var comentarios = await dbContext.AsNoTracking().Where(p => p.ProductoId == payload.mapeoCategoria).ToListAsync();
                //dbContext.RemoveRange(comentarios);
                return (ServiceStatus.Ok, mapCategoria, "Success");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.FailedValidation, null, $"Error Producto -> {ex.InnerException?.Message ?? ex.Message}");
            }

        }

        public async Task<(ServiceStatus, Grupo?, string)> DeleteGrupo(int GrupoId)
        {
            try
            {

                var tieneProductos = await dbContext.Producto.AsNoTracking()
                                    .AnyAsync(p => p.GrupoId == GrupoId);

                if (tieneProductos)
                    return (ServiceStatus.FailedValidation, null, "No se puede eliminar el grupo porque tiene productos asociados");

                var categoria = await dbContext.Grupo.AsNoTracking()
                                .FirstAsync(p => p.Id == GrupoId);

                dbContext.Remove(categoria);

                await dbContext.SaveChangesAsync();


                return (ServiceStatus.Ok, null, "Success");
            }
            catch (Exception ex)
            {
                return (ServiceStatus.FailedValidation, null, $"Error Producto -> {ex.InnerException?.Message ?? ex.Message}");
            }

        }
    }

}