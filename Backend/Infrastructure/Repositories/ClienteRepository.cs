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

public class ClienteRepository :  IClienteRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;

    public ClienteRepository(SpaContext dbContext, IMapper mapper)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
    }

    public async Task<(ServiceStatus, ClienteDto?, string)> CreateCliente(CreateClientePayload payload)
    {
        try
        {
            var entity = mapper.Map<Cliente>(payload);

            var busquedaPaciente = await dbContext.Cliente.FirstOrDefaultAsync(p => p.NumeroDocumento == payload.NumeroDocumento);

            if(busquedaPaciente != null) return (ServiceStatus.FailedValidation, null, "El número de documento existe");

            await dbContext.Cliente.AddAsync(entity);
            await dbContext.SaveChangesAsync();

            var mapeo = mapper.Map<ClienteDto>(entity);

            return (ServiceStatus.Ok, mapeo, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error crear cliente -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    public async Task<(ServiceStatus, ClienteDto?, string)> UpdateCliente(UpdateClientePayload payload)
    {
        try
        {

            var search = await dbContext.Cliente.AsNoTracking()
                                                .FirstAsync(p => p.Id == payload.ClienteId);

            var entity = mapper.Map(payload, search);
            await dbContext.SaveChangesAsync();

            var mapeo = mapper.Map<ClienteDto>(entity);
            dbContext.Entry(entity).State = EntityState.Modified;

            await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, mapeo, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error actualizar cliente -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    public async Task<(ServiceStatus, bool, string)> EliminarCliente(int ClienteId)
    {
        try
        {

            var entity = await dbContext.Cliente.AsNoTracking()
                            .FirstAsync(p => p.Id == ClienteId);

            //producto.Estado = false;

            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, true, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, false, $"Error eliminar -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<ClienteDto>?, string)> GetClientes(ClientePayload payload)
    {

        DataCollection<ClienteDto> lista = null;

        try
        {

            if (payload.Value is null)
            {
                lista = await dbContext.Cliente.AsNoTracking()
                    .Include(i => i.Ubigeo)
                    .Where(p => p.Estado == true)
                    .ProjectTo<ClienteDto>(mapper.ConfigurationProvider)
                    .GetPagedAsync(payload.Page, payload.Amount);
            }
            else
            {
                if (payload.Value.All(char.IsDigit))
                {
                    if(payload.Value.Length == 8)
                        lista = await dbContext.Cliente.AsNoTracking()
                            .Include(i => i.Ubigeo)
                            .Where(p => p.NumeroDocumento == payload.Value)
                            .ProjectTo<ClienteDto>(mapper.ConfigurationProvider)
                            .GetPagedAsync(payload.Page, payload.Amount);
                    else
                        lista = await dbContext.Cliente.AsNoTracking()
                            .Include(i => i.Ubigeo)
                            .Where(p => p.Id == Convert.ToInt32(payload.Value))
                            .ProjectTo<ClienteDto>(mapper.ConfigurationProvider)
                            .GetPagedAsync(payload.Page, payload.Amount);
                }
                else
                {
                    lista = await dbContext.Cliente.AsNoTracking()
                        .Include(i => i.Ubigeo)
                        .Where(p => p.Nombre.Contains(payload.Value))
                        .ProjectTo<ClienteDto>(mapper.ConfigurationProvider)
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
            return (ServiceStatus.InternalError, null, $"Error al consultar clientes -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

}