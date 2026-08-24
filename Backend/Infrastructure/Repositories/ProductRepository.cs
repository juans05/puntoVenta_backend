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

    public async Task<(ServiceStatus, Producto?, string)> CreateProduct(CreateProductPayload payload)
    {
        try
        {
        //    var goods = new Producto { Nombre = goodsDto.Nombre, Precio = goodsDto.Precio };

            var entity = mapper.Map<Producto>(payload);

            if (entity.CategoriaId == 0) entity.CategoriaId = null;
            if (entity.GrupoId == 0) entity.GrupoId = null;
            if (entity.ProveedorId == 0) entity.Proveedor = null;

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

            var producto = await dbContext.Producto.AsNoTracking()
                            .FirstAsync(p => p.Id == payload.ProductoId);

            var entity = mapper.Map(payload, producto);

            dbContext.Entry(entity).State = EntityState.Modified;

            //dbContext.Add(entity);

            //await dbContext.SaveChangesAsync();

            //await dbContext.Producto.AddAsync(entity);

            ////borramos los comentarios
            //var comentarios = await dbContext.Comentario.AsNoTracking().Where(p => p.ProductoId == payload.ProductoId).ToListAsync();
            // dbContext.RemoveRange(comentarios);


            await dbContext.SaveChangesAsync();


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

            return (ServiceStatus.Ok, entity, "Success");
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


            if (payload.Value is null)
            {

                lista = await dbContext.Producto.AsNoTracking()
                    .Where( p => p.Estado == true && 
                                payload.CategoriaId == null? 
                                p.CategoriaId == p.CategoriaId :
                                p.CategoriaId == payload.CategoriaId && 
                                payload.GrupoId == null ?
                                p.GrupoId == p.GrupoId :
                                p.GrupoId == payload.GrupoId)
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
                           payload.CategoriaId == null ?
                                p.CategoriaId == p.CategoriaId :
                                p.CategoriaId == payload.CategoriaId &&
                                payload.GrupoId == null ?
                                p.GrupoId == p.GrupoId :
                                p.GrupoId == payload.GrupoId
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
                                payload.CategoriaId == null ?
                                p.CategoriaId == p.CategoriaId :
                                p.CategoriaId == payload.CategoriaId &&
                                payload.GrupoId == null ?
                                p.GrupoId == p.GrupoId :
                                p.GrupoId == payload.GrupoId
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