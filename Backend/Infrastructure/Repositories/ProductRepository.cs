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
using AutoMapper.QueryableExtensions;
using Application.Interfaces.IRepository;
using System.Linq.Expressions;
using Domain.Entities;
using Domain.Enumerations;
using Domain.Common.Utils;

namespace Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly SpaContext dbContext;
    private readonly IMapper mapper;


    public ProductRepository(SpaContext dbContext, IMapper mapper)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;

}

    // Las cuentas contables del producto solo pueden ser de ultimo nivel (Nivel 5 = 8 digitos): es la
    // unica hoja que admite asientos. Devuelve el mensaje de error o null.
    private async Task<string?> ValidarCuentasUltimoNivel(params int?[] ids)
    {
        var pedidos = ids.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (pedidos.Count == 0) return null;
        var cuentas = await dbContext.CuentaContable.AsNoTracking().Where(c => pedidos.Contains(c.Id)).Select(c => new { c.Id, c.Codigo }).ToListAsync();
        if (cuentas.Count != pedidos.Count) return "Una de las cuentas contables elegidas no existe";
        return cuentas.Any(c => c.Codigo.Length != 8) ? "Las cuentas contables del producto deben ser de último nivel (8 dígitos)" : null;
    }

    private const int MaxGaleria = 5;

    private static bool UrlHttp(string? u) => Uri.TryCreate(u?.Trim(), UriKind.Absolute, out var x) && (x.Scheme == Uri.UriSchemeHttp || x.Scheme == Uri.UriSchemeHttps);

    private static string? ValidarMedia(string? videoUrl, string? galeria, string? descripcion = null)
    {
        if (descripcion?.Length > 2000) return "La descripción admite máx. 2000 caracteres";
        if (!string.IsNullOrWhiteSpace(videoUrl) && (videoUrl.Trim().Length > 500 || !UrlHttp(videoUrl)))
            return "El video debe ser una URL http(s) de hasta 500 caracteres";
        if (string.IsNullOrWhiteSpace(galeria)) return null;
        try
        {
            var urls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(galeria) ?? new();
            if (urls.Count > MaxGaleria) return $"La galería admite hasta {MaxGaleria} imágenes";
            if (urls.Any(u => u.Length > 500 || !UrlHttp(u))) return "Las imágenes de la galería deben ser URLs http(s) de hasta 500 caracteres";
            return null;
        }
        catch (System.Text.Json.JsonException) { return "La galería no tiene un formato válido"; }
    }

    private static IEnumerable<int?> CuentasInventarioDe(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Enumerable.Empty<int?>();
        try { return (System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new()).Values.Select(v => (int?)v); }
        catch (System.Text.Json.JsonException) { return new int?[] { -1 }; } // JSON invalido -> "no existe"
    }

    public async Task<(ServiceStatus, Producto?, string)> CreateProduct(CreateProductPayload payload)
    {
        try
        {
            if (ValidarMedia(payload.VideoUrl, payload.Galeria, payload.Descripcion) is { } errorMedia)
                return (ServiceStatus.FailedValidation, null, errorMedia);
            if (await ValidarCuentasUltimoNivel(new[] { payload.CuentaIngresoId, payload.CuentaIngresoDebeId, payload.CuentaCostoId, payload.CuentaGastoHaberId, payload.CuentaInventarioId }.Concat(CuentasInventarioDe(payload.CuentasInventarioMovimiento)).ToArray()) is { } errorCuentas)
                return (ServiceStatus.FailedValidation, null, errorCuentas);
        //    var goods = new Producto { Nombre = goodsDto.Nombre, Precio = goodsDto.Precio };

            var entity = mapper.Map<Producto>(payload);

            if (entity.CategoriaId == 0) entity.CategoriaId = null;
            if (entity.GrupoId == 0) entity.GrupoId = null;
            if (entity.ProveedorId == 0) entity.Proveedor = null;

            if (payload.PreciosAlternativos?.Count > 0)
                entity.PreciosAlternativos = mapper.Map<List<PrecioAlternativo>>(payload.PreciosAlternativos);

            if (payload.Presentaciones?.Count > 0)
                entity.Presentaciones = mapper.Map<List<Presentacion>>(payload.Presentaciones);

            //entity.Comentarios?.ForEach(x => x.UsuarioCreacion = payload.UsuarioCreacion);

            //entity.Id = 34;

            await dbContext.Producto.AddAsync(entity);
            await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, entity, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Producto -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    public async Task<(ServiceStatus, Producto?, string)> UpdateProduct(UpdateProductPayload payload)
    {
        try
        {
            if (ValidarMedia(payload.VideoUrl, payload.Galeria, payload.Descripcion) is { } errorMedia)
                return (ServiceStatus.FailedValidation, null, errorMedia);
            if (await ValidarCuentasUltimoNivel(new[] { payload.CuentaIngresoId, payload.CuentaIngresoDebeId, payload.CuentaCostoId, payload.CuentaGastoHaberId, payload.CuentaInventarioId }.Concat(CuentasInventarioDe(payload.CuentasInventarioMovimiento)).ToArray()) is { } errorCuentas)
                return (ServiceStatus.FailedValidation, null, errorCuentas);

            var producto = await dbContext.Producto.AsNoTracking()
                            .FirstAsync(p => p.Id == payload.ProductoId);

            var entity = mapper.Map(payload, producto);

            // El formulario manda 0 cuando no hay categoria/grupo/proveedor seleccionado -- 0 no es
            // un Id real y viola la FK correspondiente si se intenta guardar tal cual (ver CreateProduct).
            if (entity.CategoriaId == 0) entity.CategoriaId = null;
            if (entity.GrupoId == 0) entity.GrupoId = null;
            if (entity.ProveedorId == 0) entity.ProveedorId = null;

            dbContext.Entry(entity).State = EntityState.Modified;

            // Reemplazo completo de los hijos en cada edicion -- mas simple que diffear altas/bajas/cambios
            // uno por uno, y coincide con como el modal los maneja (arma la lista completa en memoria y la
            // manda entera al guardar el producto).
            if (payload.PreciosAlternativos != null)
            {
                var preciosExistentes = await dbContext.PrecioAlternativo
                    .Where(p => p.ProductoId == payload.ProductoId).ToListAsync();
                dbContext.PrecioAlternativo.RemoveRange(preciosExistentes);

                var nuevosPrecios = mapper.Map<List<PrecioAlternativo>>(payload.PreciosAlternativos);
                nuevosPrecios.ForEach(p => p.ProductoId = payload.ProductoId);
                await dbContext.PrecioAlternativo.AddRangeAsync(nuevosPrecios);
            }

            if (payload.Presentaciones != null)
            {
                var presentacionesExistentes = await dbContext.Presentacion
                    .Where(p => p.ProductoId == payload.ProductoId).ToListAsync();
                dbContext.Presentacion.RemoveRange(presentacionesExistentes);

                var nuevasPresentaciones = mapper.Map<List<Presentacion>>(payload.Presentaciones);
                nuevasPresentaciones.ForEach(p => p.ProductoId = payload.ProductoId);
                await dbContext.Presentacion.AddRangeAsync(nuevasPresentaciones);
            }

            //dbContext.Add(entity);

            //await dbContext.SaveChangesAsync();

            //await dbContext.Producto.AddAsync(entity);

            ////borramos los comentarios
            //var comentarios = await dbContext.Comentario.AsNoTracking().Where(p => p.ProductoId == payload.ProductoId).ToListAsync();
            // dbContext.RemoveRange(comentarios);


            await dbContext.SaveChangesAsync();

            // Recargar con las navegaciones incluidas -- "entity" solo tiene los Ids escalares (el mapper
            // no toca Categoria/Moneda/TipoIgv/UnidadMedida), asi que devolverlo tal cual serializa esos
            // campos en null y pisa la fila en el frontend con datos incompletos (categoria/moneda "perdidas"
            // hasta el proximo refresh manual).
            var actualizado = await dbContext.Producto.AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .Include(p => p.Grupo)
                .Include(p => p.Moneda)
                .Include(p => p.TipoIgv)
                .Include(p => p.UnidadMedida)
                .FirstAsync(p => p.Id == payload.ProductoId);

            //agregamos los comentarios
            //var mapComentario = mapper.Map<List<Comentario>>(payload.Comentarios);

            //var comentarioLista = new List<Comentario>();



            //foreach (var comentario in payload.Comentarios)
            //{
            //    comentarios.Add(new Comentario
            //    {
            //        ProductoId = payload.ProductoId,
            //        Descripcion = comentario.Descripcion,
            //        Item = comentario.Item
            //    });


            //}
              
            //await dbContext.AddRangeAsync(comentarios);

            //await dbContext.SaveChangesAsync();


            //await dbContext.AddAsync(mapComentario);
            //await dbContext.SaveChangesAsync();

            return (ServiceStatus.Ok, actualizado, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Producto -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    public async Task<(ServiceStatus, Producto?, string)> DeleteProduct(int ProductoId)
    {
        try
        {

            var producto = await dbContext.Producto.AsNoTracking()
                            .FirstAsync(p => p.Id == ProductoId);

            //producto.Estado = false;

            dbContext.Remove(producto);
         
            await dbContext.SaveChangesAsync();

            //borramos los comentarios
            //var comentarios = await dbContext.Comentario.AsNoTracking().Where(p => p.ProductoId == ProductoId).ToListAsync();
            //dbContext.RemoveRange(comentarios);
            //await dbContext.SaveChangesAsync();


            return (ServiceStatus.Ok, null, "Success");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.FailedValidation, null, $"Error Producto -> {ex.InnerException?.Message ?? ex.Message}");
        }

    }

    //public async Task<IReadOnlyList<Producto>> GetAllAsync()
    //{
    //    return await dbContext.Producto.ToListAsync();
    //}
    public async Task<(ServiceStatus, DataCollection<ProductoDto>?, string)> GetProducto(ProductPayload payload)
    {

        DataCollection<ProductoDto> lista = null;

        try
        {
            payload.CategoriaId = payload.CategoriaId == 0 ? null : payload.CategoriaId;
            payload.GrupoId = payload.GrupoId == 0 ? null :  payload.GrupoId;


            if (string.IsNullOrWhiteSpace(payload.Value))
            {

                lista = await dbContext.Producto.AsNoTracking()
                    .Where( p => p.Estado == true &&
                                (payload.CategoriaId == null || p.CategoriaId == payload.CategoriaId) &&
                                (payload.GrupoId == null || p.GrupoId == payload.GrupoId) &&
                                (payload.SeVende == null || p.SeVende == payload.SeVende) &&
                                (payload.SeCompra == null || p.SeCompra == payload.SeCompra))
                    .Include(i => i.Proveedor)
                    //.Include(i => i.Comentarios)
                    .Include(i => i.Categoria)
                    .Include(i => i.Grupo)
                    .ProjectTo<ProductoDto>(mapper.ConfigurationProvider)
                    .GetPagedAsync(payload.Page, payload.Amount);
            }
            else
            {
                if (payload.Value.All(char.IsDigit))
                {
                    lista = await dbContext.Producto.AsNoTracking()
                        .Include(i => i.Proveedor)
                        //.Include(i => i.Comentarios)
                        .Where(p => p.Id == Convert.ToInt32(payload.Value) &&
                                (payload.CategoriaId == null || p.CategoriaId == payload.CategoriaId) &&
                                (payload.GrupoId == null || p.GrupoId == payload.GrupoId) &&
                                (payload.SeVende == null || p.SeVende == payload.SeVende) &&
                                (payload.SeCompra == null || p.SeCompra == payload.SeCompra)
                        )
                        .ProjectTo<ProductoDto>(mapper.ConfigurationProvider)
                        .GetPagedAsync(payload.Page, payload.Amount);


                }
                else
                {
                    lista = await dbContext.Producto.AsNoTracking()
                        .Include(i => i.Proveedor)
                        //.Include(i => i.Comentarios)
                        .Where(p => p.Nombre.Contains(payload.Value) &&
                                (payload.CategoriaId == null || p.CategoriaId == payload.CategoriaId) &&
                                (payload.GrupoId == null || p.GrupoId == payload.GrupoId) &&
                                (payload.SeVende == null || p.SeVende == payload.SeVende) &&
                                (payload.SeCompra == null || p.SeCompra == payload.SeCompra)
                        )
                        .ProjectTo<ProductoDto>(mapper.ConfigurationProvider)
                        .GetPagedAsync(payload.Page, payload.Amount);
                }

            }

            if (!lista.HasItems) return (ServiceStatus.NotFound, null, "No hay registros para mostrar");


            //lista.Items?.ForEach(c => { c.Index = index++; });

            foreach (var (item, index) in lista.Items!.WithCustomIndex())
            {
                item.Index = (payload.Page * payload.Amount) - payload.Amount + index;
            }

            return (ServiceStatus.Ok, lista, "Succeeded");

        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al consultar Productos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static List<ProductoCsvRow> ParseCsv(string csv)
    {
        var filas = new List<ProductoCsvRow>();
        var lineas = csv.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lineas.Length; i++)
        {
            var line = lineas[i].Trim();
            if (line.Length == 0) continue;

            var delim = line.Contains(';') ? ';' : ',';
            var cols = line.Split(delim).Select(c => c.Trim()).ToList();

            if (i == 0 && cols.Count > 1 && (cols[0].ToLower().Contains("nombre") || cols[0].ToLower() == "sku" || cols[0].ToLower() == "codigo"))
                continue;

            var fila = new ProductoCsvRow();
            fila.Sku = cols.Count > 0 ? cols[0] : null;
            fila.Nombre = cols.Count > 1 ? cols[1] : string.Empty;
            fila.Categoria = cols.Count > 2 ? cols[2] : null;
            fila.PrecioCompra = cols.Count > 3 && decimal.TryParse(cols[3].Replace("S/", "").Trim(), out var pc) ? pc : 0;
            fila.PrecioVenta = cols.Count > 4 && decimal.TryParse(cols[4].Replace("S/", "").Trim(), out var pv) ? pv : 0;
            fila.Stock = cols.Count > 5 && int.TryParse(cols[5], out var st) ? st : 0;
            fila.StockMinimo = cols.Count > 6 && int.TryParse(cols[6], out var sm) ? sm : (int?)null;

            if (string.IsNullOrWhiteSpace(fila.Nombre))
                fila.Error = "El nombre es obligatorio";
            else if (fila.PrecioVenta <= 0)
                fila.Error = "El precio de venta debe ser mayor a cero";
            else if (fila.Stock < 0)
                fila.Error = "El stock no puede ser negativo";

            filas.Add(fila);
        }

        return filas;
    }

    public async Task<(ServiceStatus, PreviewImportDto?, string)> PrevisualizarImportacion(ImportProductosPayload payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload.Csv))
                return (ServiceStatus.FailedValidation, null, "El contenido CSV es obligatorio");

            var filas = ParseCsv(payload.Csv);

            if (filas.Count == 0)
                return (ServiceStatus.FailedValidation, null, "No se encontraron filas en el archivo");

            var preview = new PreviewImportDto
            {
                TotalFilas = filas.Count,
                Validas = filas.Count(f => string.IsNullOrEmpty(f.Error)),
                ConError = filas.Count(f => !string.IsNullOrEmpty(f.Error)),
                Filas = filas
            };

            return (ServiceStatus.Ok, preview, "Vista previa generada correctamente");
        }
        catch (Exception ex)
        {
            return (ServiceStatus.InternalError, null, $"Error al previsualizar importacion -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<(ServiceStatus, int, string)> ImportarProductos(ImportProductosPayload payload)
    {
        try
        {
            var filas = ParseCsv(payload.Csv).Where(f => string.IsNullOrEmpty(f.Error)).ToList();

            if (filas.Count == 0)
                return (ServiceStatus.FailedValidation, 0, "No hay filas validas para importar");

            await dbContext.Database.BeginTransactionAsync();

            var insertados = 0;

            foreach (var fila in filas)
            {
                var producto = new Producto
                {
                    Nombre = fila.Nombre.Trim(),
                    CodigoBarra = fila.Sku,
                    Precio = fila.PrecioCompra,
                    PrecioVentaConInpuesto = fila.PrecioVenta,
                    Stock = fila.Stock,
                    StockMinimo = fila.StockMinimo
                };

                if (!string.IsNullOrWhiteSpace(fila.Categoria))
                {
                    var categoria = await dbContext.Categoria.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Nombre.ToLower() == fila.Categoria.Trim().ToLower());

                    if (categoria == null)
                    {
                        var nueva = new Categoria { Nombre = fila.Categoria.Trim() };
                        await dbContext.Categoria.AddAsync(nueva);
                        await dbContext.SaveChangesAsync();
                        producto.CategoriaId = nueva.Id;
                    }
                    else
                    {
                        producto.CategoriaId = categoria.Id;
                    }
                }

                await dbContext.Producto.AddAsync(producto);
                await dbContext.SaveChangesAsync();

                if (fila.Stock > 0)
                {
                    dbContext.InventoryMovement.Add(new InventoryMovement
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = (int)TipoMovimientoInventario.AjusteEntrada,
                        Cantidad = fila.Stock,
                        StockAnterior = 0,
                        StockPosterior = fila.Stock,
                        ReferenciaTipo = "Importacion"
                    });
                }

                insertados++;
            }

            await dbContext.SaveChangesAsync();
            await dbContext.Database.CommitTransactionAsync();

            return (ServiceStatus.Ok, insertados, $"{insertados} producto(s) importados correctamente");
        }
        catch (Exception ex)
        {
            await dbContext.Database.RollbackTransactionAsync();
            return (ServiceStatus.InternalError, 0, $"Error al importar productos -> {ex.InnerException?.Message ?? ex.Message}");
        }
    }

}