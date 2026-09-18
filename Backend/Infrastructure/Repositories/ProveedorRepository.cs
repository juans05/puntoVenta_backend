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

public class ProveedorRepository : IProveedorRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;


    public ProveedorRepository(SpaContext dbContext, IMapper mapper)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;

}

    public async Task<(ServiceStatus, ProveedorDto?, string)> CrearProveedor(CreateProveedorPayload payload)
    {
        try
        {
            var entity = mapper.Map<Proveedor>(payload);

            await dbContext.Proveedor.AddAsync(entity);
            await dbContext.SaveChangesAsync();

            var mapeo = mapper.Map<ProveedorDto>(entity);
            
            return (ServiceStatus.Ok, mapeo, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Proveedor -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    public async Task<(ServiceStatus, ProveedorDto?, string)> UpdateProveedor(UpdateProveedorPayload payload)
    {
        try
        {

            var search = await dbContext.Proveedor.AsNoTracking()
                            .FirstAsync(p => p.Id == payload.ProveedorId);

            var entity = mapper.Map(payload, search);
            dbContext.Entry(entity).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();

            var mapeo = mapper.Map<ProveedorDto>(entity);

            //await dbContext.AddAsync(mapComentario);
            await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, mapeo, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Producto -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    public async Task<(ServiceStatus, Proveedor?, string)> DeleteProveedor(int ProveedorId)
    {
        try
        {

            var entity = await dbContext.Proveedor.AsNoTracking()
                            .FirstAsync(p => p.Id == ProveedorId);

            //producto.Estado = false;

            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, null, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Producto -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<ProveedorDto>?, string)> GetProveedor(ProveedorPayload payload)
    {

        DataCollection<ProveedorDto> lista = null;

        try
        {

            if (payload.Value is null)
            {

                lista = await dbContext.Proveedor.AsNoTracking().Where( p => p.Estado == true)
                    .Include(i => i.Productos)
                    .ProjectTo<ProveedorDto>(mapper.ConfigurationProvider)
                    .GetPagedAsync(payload.Page, payload.Amount); ;
            }
            else
            {
                if (payload.Value.All(char.IsDigit))
                {
                    // RUC (11 digitos): buscar por Ruc. Cualquier otro largo numerico se
                    // interpreta como el Id interno -- pero Convert.ToInt32 desborda con un RUC
                    // (mismo bug ya corregido en ClienteRepository), asi que se usa TryParse y,
                    // si no entra en un int, no hay resultados en vez de tirar un 500.
                    if (payload.Value.Length == 11)
                        lista = await dbContext.Proveedor.AsNoTracking()
                            .Include(i => i.Productos)
                            .Where(p => p.Ruc == payload.Value)
                            .ProjectTo<ProveedorDto>(mapper.ConfigurationProvider)
                            .GetPagedAsync(payload.Page, payload.Amount);
                    else if (int.TryParse(payload.Value, out var proveedorId))
                        lista = await dbContext.Proveedor.AsNoTracking()
                            .Include(i => i.Productos)
                            .Where(p => p.Id == proveedorId)
                            .ProjectTo<ProveedorDto>(mapper.ConfigurationProvider)
                            .GetPagedAsync(payload.Page, payload.Amount);
                    else
                        lista = await dbContext.Proveedor.AsNoTracking()
                            .Include(i => i.Productos)
                            .Where(p => false)
                            .ProjectTo<ProveedorDto>(mapper.ConfigurationProvider)
                            .GetPagedAsync(payload.Page, payload.Amount);
                }
                else
                {
                    lista = await dbContext.Proveedor.AsNoTracking()
                        .Include(i => i.Productos)
                        .Where(p => p.Nombre.Contains(payload.Value))
                        .ProjectTo<ProveedorDto>(mapper.ConfigurationProvider)
                        .GetPagedAsync(payload.Page, payload.Amount);
                }

            }

            if (!lista.HasItems) return (ServiceStatus.NotFound, null, "No hay registros para mostrar");

            foreach (var (item, index) in lista.Items!.WithCustomIndex())
            {
                item.Index = (payload.Page * payload.Amount) - payload.Amount + index;
            }

            return (ServiceStatus.Ok, lista, "Succeeded");

        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar Proveedor -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

}