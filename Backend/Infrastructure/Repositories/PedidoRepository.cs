using Application.Abstractions;
using Domain.Common;
using Domain.DTO;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Application.Interfaces.IRepository;
using System.Text;

namespace Infrastructure.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly SpaContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public PedidoRepository(SpaContext context, IMapper mapper, ITenantContextAccessor tenantContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<(ServiceStatus, PedidoDTO?, string)> CrearPedido(CreatePedidoPayload payload)
    {
        try
        {
            var token = GenerarTokenUnico();

            // payload.SucursalId nunca se usa como fuente de verdad: el filtro global de
            // Pedido acepta SucursalId == null como "visible para todo el tenant", así que
            // cuando el contexto no trae sede (sin X-Sucursal/claim) hay que guardar null,
            // no un valor inventado del payload -> con SucursalId != null quedaría fuera
            // del propio filtro (SucursalId == null || SucursalId == tenant.SucursalId).
            var sucursalId = _tenantContextAccessor.CurrentContext?.SucursalId;

            var pedido = new Pedido
            {
                SucursalId = sucursalId,
                Token = token,
                EstadoPedido = EstatusPedido.Enviado,
                Total = payload.Total,
                ClienteId = payload.ClienteId,
                Nombre = payload.Nombre,
                Dni = payload.Dni,
                Celular = payload.Celular,
                PedidoDetalles = payload.Detalles.Select(d => new PedidoDetalle
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    ValorUnitario = d.ValorUnitario
                }).ToList()
            };

            await _context.AddAsync(pedido);
            await _context.SaveChangesAsync();

            // Recarga con Producto: el pedido recien guardado no lo trae y el DTO saldria sin nombres.
            var (_, dto, _) = await ObtenerPedidoPorId(pedido.Id);

            return (ServiceStatus.Ok, dto, "Pedido creado exitosamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al crear pedido -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, PedidoDTO?, string)> ObtenerPedidoPorId(int id)
    {
        try
        {
            var pedido = await _context.Pedido
                .Include(p => p.Cliente)
                .Include(p => p.Ubigeo)
                .Include(p => p.Salon)
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return (ServiceStatus.NotFound, null, "Pedido no encontrado");

            var dto = _mapper.Map<PedidoDTO>(pedido);
            dto.EstadoPedidoDescripcion = ObtenerDescripcionEstado(pedido.EstadoPedido);
            dto.FechaCreacion = pedido.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
            dto.UsuarioCreacion = pedido.UsuarioCreacion;
            dto.UbigeoNombre = pedido.Ubigeo != null ? $"{pedido.Ubigeo.Departamento} - {pedido.Ubigeo.Provincia} - {pedido.Ubigeo.Distrito}" : null;

            return (ServiceStatus.Ok, dto, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener pedido -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, PedidoPublicoDTO?, string)> ObtenerPedidoPublico(string token)
    {
        try
        {
            // IgnoreQueryFilters: este endpoint es [AllowAnonymous] (enlace público sin login),
            // por lo que no hay tenant resuelto en el request y el filtro global por TenantId
            // dejaría cualquier pedido invisible. El Token único (32 chars, índice unique) ya
            // es la credencial de acceso para este flujo, no hace falta el filtro de tenant.
            var pedido = await _context.Pedido
                .IgnoreQueryFilters()
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Token == token && p.EstadoPedido != EstatusPedido.Cancelado);

            if (pedido == null)
                return (ServiceStatus.NotFound, null, "Pedido no encontrado o cancelado");

            var dto = new PedidoPublicoDTO
            {
                Id = pedido.Id,
                Token = pedido.Token,
                Total = pedido.Total,
                EstadoPedido = pedido.EstadoPedido,
                PedidoDetalles = pedido.PedidoDetalles.Select(d => new PedidoDetallePublicoDTO
                {
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.Nombre ?? "",
                    ProductoImagen = d.Producto?.RutaImagen,
                    Cantidad = d.Cantidad,
                    ValorUnitario = d.ValorUnitario
                }).ToList()
            };

            return (ServiceStatus.Ok, dto, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener pedido público -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<PedidoDTO>?, string)> ListarPedidos(PedidoQueryParams payload)
    {
        try
        {
            var query = _context.Pedido
                .Include(p => p.Cliente)
                .Include(p => p.Ubigeo)
                .Include(p => p.PedidoDetalles)
                .AsNoTracking()
                .Where(p => p.Estado == true);

            if (payload.EstadoPedido.HasValue)
            {
                query = query.Where(p => p.EstadoPedido == payload.EstadoPedido.Value);
            }

            var lista = await query
                .OrderByDescending(p => p.FechaCreacion)
                .ProjectTo<PedidoDTO>(_mapper.ConfigurationProvider)
                .GetPagedAsync(payload.Page, payload.Amount);

            foreach (var item in lista.Items ?? Enumerable.Empty<PedidoDTO>())
            {
                item.EstadoPedidoDescripcion = ObtenerDescripcionEstado(item.EstadoPedido);
            }

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar pedidos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, PedidoDTO?, string)> ActualizarEstadoPedido(UpdatePedidoEstadoPayload payload)
    {
        try
        {
            var pedido = await _context.Pedido
                .Include(p => p.Cliente)
                .Include(p => p.Ubigeo)
                .Include(p => p.Salon)
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == payload.Id);

            if (pedido == null)
                return (ServiceStatus.NotFound, null, "Pedido no encontrado");

            if (!EsTransicionValida(pedido.EstadoPedido, payload.EstadoPedido))
                return (ServiceStatus.FailedValidation, null, $"Transición de estado inválida: {pedido.EstadoPedido} -> {payload.EstadoPedido}");

            pedido.EstadoPedido = payload.EstadoPedido;

            if (payload.EstadoPedido == EstatusPedido.Despachado && !string.IsNullOrWhiteSpace(payload.CodigoSeguimiento))
            {
                pedido.CodigoSeguimiento = payload.CodigoSeguimiento;
                pedido.FechaDespacho = DateTime.UtcNow.AddHours(-5);
            }

            if (payload.EstadoPedido == EstatusPedido.Entregado)
            {
                pedido.FechaEntrega = DateTime.UtcNow.AddHours(-5);
            }

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<PedidoDTO>(pedido);
            dto.EstadoPedidoDescripcion = ObtenerDescripcionEstado(pedido.EstadoPedido);
            dto.UbigeoNombre = pedido.Ubigeo != null ? $"{pedido.Ubigeo.Departamento} - {pedido.Ubigeo.Provincia} - {pedido.Ubigeo.Distrito}" : null;

            return (ServiceStatus.Ok, dto, "Estado actualizado");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error al actualizar estado -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, bool, string)> ReenviarPassword(int pedidoId)
    {
        try
        {
            var pedido = await _context.Pedido.FirstOrDefaultAsync(p => p.Id == pedidoId);
            
            if (pedido == null)
                return (ServiceStatus.NotFound, false, "Pedido no encontrado");

            if (pedido.PasswordEnviado)
                return (ServiceStatus.FailedValidation, false, "El password ya fue enviado anteriormente");

            // La lógica real de envío de WhatsApp se maneja en el servicio
            // Aquí solo marcamos que se va a reintentar
            return (ServiceStatus.Ok, true, "Listo para reenvío");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, false, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, EtiquetaEnvioDTO?, string)> ObtenerEtiquetaEnvio(int pedidoId)
    {
        try
        {
            var pedido = await _context.Pedido
                .Include(p => p.Ubigeo)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
                return (ServiceStatus.NotFound, null, "Pedido no encontrado");

            var etiqueta = new EtiquetaEnvioDTO
            {
                PedidoId = pedido.Id,
                Nombre = pedido.Nombre ?? "",
                Direccion = pedido.Direccion ?? "",
                Distrito = pedido.Ubigeo?.Distrito ?? "",
                Celular = pedido.Celular ?? "",
                TipoEnvio = pedido.TipoEnvio ?? "",
                CodigoSeguimiento = pedido.CodigoSeguimiento
            };

            return (ServiceStatus.Ok, etiqueta, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener etiqueta -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, SubmitPedidoResult, string)> SubmitPedidoPublico(string token, PedidoPublicoSubmitPayload payload)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Mismo motivo que en ObtenerPedidoPublico: endpoint [AllowAnonymous], sin tenant resuelto.
            var pedido = await _context.Pedido
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Token == token);

            if (pedido == null)
                return (ServiceStatus.NotFound, new SubmitPedidoResult { Exito = false }, "Pedido no encontrado");

            if (pedido.EstadoPedido != EstatusPedido.Enviado)
                return (ServiceStatus.FailedValidation, new SubmitPedidoResult { Exito = false }, "Este pedido ya tiene los datos completados o fue cancelado");

            // Buscar o crear cliente por DNI
            var cliente = await _context.Cliente
                .FirstOrDefaultAsync(c => c.NumeroDocumento == payload.Dni && c.TipoDocumentoId == 1); // DNI = 1

            if (cliente == null)
            {
                cliente = new Cliente
                {
                    Nombre = payload.Nombre,
                    TipoDocumentoId = 1, // DNI
                    NumeroDocumento = payload.Dni,
                    Telefono = payload.Celular,
                    UbigeoId = payload.UbigeoId
                };
                await _context.Cliente.AddAsync(cliente);
                await _context.SaveChangesAsync();
            }

            // Derivar TipoEnvio
            var ubigeo = await _context.Ubigeo.FindAsync(payload.UbigeoId);
            var tipoEnvio = (ubigeo?.Departamento == "LIMA" && ubigeo?.Provincia == "LIMA") ? "LOCAL" : "PROVINCIA";

            if (tipoEnvio == "PROVINCIA")
            {
                if (payload.SalonId == null)
                    return (ServiceStatus.FailedValidation, new SubmitPedidoResult { Exito = false }, "Debes elegir el salón donde recogerás tu pedido");

                var salon = await _context.Salon.FirstOrDefaultAsync(s => s.Id == payload.SalonId && s.Activo && s.UbigeoId == payload.UbigeoId);
                if (salon == null)
                    return (ServiceStatus.FailedValidation, new SubmitPedidoResult { Exito = false }, "El salón elegido no es válido para tu ubicación");
            }

            // Actualizar pedido con datos del cliente
            pedido.ClienteId = cliente.Id;
            pedido.Nombre = payload.Nombre;
            pedido.Dni = payload.Dni;
            pedido.Celular = payload.Celular;
            pedido.UbigeoId = payload.UbigeoId;
            pedido.TipoEnvio = tipoEnvio;
            pedido.Direccion = payload.Direccion;
            pedido.Referencia = payload.Referencia;
            pedido.Latitud = payload.Latitud;
            pedido.Longitud = payload.Longitud;
            pedido.Currier = tipoEnvio == "PROVINCIA" ? "SHALOM" : null;
            pedido.SalonId = tipoEnvio == "PROVINCIA" ? payload.SalonId : null;
            pedido.EstadoPedido = EstatusPedido.DatosCompletos;

            // Crear ClienteCuenta si no existe
            var clienteCuenta = await _context.Set<ClienteCuenta>()
                .FirstOrDefaultAsync(cc => cc.ClienteId == cliente.Id);

            string? passwordGenerado = null;
            
            if (clienteCuenta == null)
            {
                passwordGenerado = GenerarPasswordAleatorio(8);
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ClienteCuenta>();
                var hash = passwordHasher.HashPassword(null!, passwordGenerado);

                clienteCuenta = new ClienteCuenta
                {
                    ClienteId = cliente.Id,
                    Email = $"{payload.Dni}@puntosventa.local", // Email temporal basado en DNI
                    PasswordHash = hash
                };
                await _context.Set<ClienteCuenta>().AddAsync(clienteCuenta);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Retornar resultado con password generado (solo para envío por WhatsApp en el servicio)
            return (ServiceStatus.Ok, new SubmitPedidoResult 
            { 
                Exito = true, 
                PasswordGenerado = passwordGenerado,
                PedidoId = pedido.Id,
                Celular = pedido.Celular,
                CuentaExistia = passwordGenerado == null
            }, "Datos registrados exitosamente");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (ServiceStatus.InternalError, new SubmitPedidoResult { Exito = false }, $"Error al procesar formulario -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, bool, string)> ActualizarPasswordEnviado(int pedidoId, bool enviado)
    {
        try
        {
            var pedido = await _context.Pedido.FirstOrDefaultAsync(p => p.Id == pedidoId);
            if (pedido == null)
                return (ServiceStatus.NotFound, false, "Pedido no encontrado");

            pedido.PasswordEnviado = enviado;
            await _context.SaveChangesAsync();

            return (ServiceStatus.Ok, true, "Actualizado");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, false, $"Error -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, DataCollection<PedidoDTO>?, string)> ListarPedidosCliente(int clienteId, PaginationPayload payload)
    {
        try
        {
            // IgnoreQueryFilters: el JWT de cliente no trae claim de sede (un cliente no
            // pertenece a una sola sucursal), así que el filtro global TenantId+SucursalId
            // de Pedido no puede resolverse y dejaría esto siempre vacío. El Where por
            // ClienteId ya es el límite de autorización correcto para este endpoint.
            var query = _context.Pedido
                .IgnoreQueryFilters()
                .Include(p => p.Ubigeo)
                .Include(p => p.PedidoDetalles)
                .AsNoTracking()
                .Where(p => p.ClienteId == clienteId && p.Estado == true)
                .OrderByDescending(p => p.FechaCreacion);

            var lista = await query
                .ProjectTo<PedidoDTO>(_mapper.ConfigurationProvider)
                .GetPagedAsync(payload.Page, payload.Amount);

            foreach (var item in lista.Items ?? Enumerable.Empty<PedidoDTO>())
            {
                item.EstadoPedidoDescripcion = ObtenerDescripcionEstado(item.EstadoPedido);
                item.UbigeoNombre = item.UbigeoId != null ? "" : null; // Se llenará en el mapper
            }

            return (ServiceStatus.Ok, lista, "Succeeded");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al listar pedidos del cliente -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, PedidoDTO?, string)> ObtenerPedidoCliente(int clienteId, int pedidoId)
    {
        try
        {
            // Mismo motivo que en ListarPedidosCliente: sin sede en el JWT de cliente.
            var pedido = await _context.Pedido
                .IgnoreQueryFilters()
                .Include(p => p.Ubigeo)
                .Include(p => p.Salon)
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.ClienteId == clienteId);

            if (pedido == null)
                return (ServiceStatus.NotFound, null, "Pedido no encontrado");

            var dto = _mapper.Map<PedidoDTO>(pedido);
            dto.EstadoPedidoDescripcion = ObtenerDescripcionEstado(pedido.EstadoPedido);
            dto.UbigeoNombre = pedido.Ubigeo != null ? $"{pedido.Ubigeo.Departamento} - {pedido.Ubigeo.Provincia} - {pedido.Ubigeo.Distrito}" : null;

            return (ServiceStatus.Ok, dto, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al obtener pedido del cliente -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static string GenerarTokenUnico()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var token = new char[32];
        for (int i = 0; i < token.Length; i++)
        {
            token[i] = chars[random.Next(chars.Length)];
        }
        return new string(token);
    }

    private static string GenerarPasswordAleatorio(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var password = new char[length];
        for (int i = 0; i < length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        return new string(password);
    }

    private static string ObtenerDescripcionEstado(char estado)
    {
        return estado switch
        {
            EstatusPedido.Enviado => "Enviado (esperando datos)",
            EstatusPedido.DatosCompletos => "Datos completados",
            EstatusPedido.EnPreparacion => "En preparación",
            EstatusPedido.Despachado => "Despachado",
            EstatusPedido.EnCamino => "En camino",
            EstatusPedido.Entregado => "Entregado",
            EstatusPedido.Cancelado => "Cancelado",
            _ => "Desconocido"
        };
    }

    private static bool EsTransicionValida(char estadoActual, char estadoNuevo)
    {
        // No permitir saltos inválidos
        var transicionesValidas = new Dictionary<char, List<char>>
        {
            { EstatusPedido.Enviado, new List<char> { EstatusPedido.DatosCompletos, EstatusPedido.Cancelado } },
            { EstatusPedido.DatosCompletos, new List<char> { EstatusPedido.EnPreparacion, EstatusPedido.Cancelado } },
            { EstatusPedido.EnPreparacion, new List<char> { EstatusPedido.Despachado, EstatusPedido.Cancelado } },
            { EstatusPedido.Despachado, new List<char> { EstatusPedido.EnCamino, EstatusPedido.Entregado, EstatusPedido.Cancelado } },
            { EstatusPedido.EnCamino, new List<char> { EstatusPedido.Entregado, EstatusPedido.Cancelado } },
            { EstatusPedido.Entregado, new List<char> { } },
            { EstatusPedido.Cancelado, new List<char> { } }
        };

        return transicionesValidas.TryGetValue(estadoActual, out var validos) && validos.Contains(estadoNuevo);
    }
}