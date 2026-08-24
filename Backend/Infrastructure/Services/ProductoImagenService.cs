using AutoMapper;
using Application.Abstractions;
using Application.Interfaces.IServices;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.DTO;
using Domain.Models;
using Domain.Payloads;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;

namespace Infrastructure.Services;

public class ProductoImagenService : IProductoImagenService
{
    private const long MaximoBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] TiposPermitidos = { "image/jpeg", "image/jpg", "image/png", "image/webp" };

    private readonly SpaContext dbContext;
    private readonly IMapper mapper;
    private readonly IOptions<CloudinarySettings> cloudinarySettings;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public ProductoImagenService(
        SpaContext dbContext,
        IMapper mapper,
        IOptions<CloudinarySettings> cloudinarySettings,
        IHttpContextAccessor httpContextAccessor,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
        this.cloudinarySettings = cloudinarySettings;
        this.httpContextAccessor = httpContextAccessor;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<MessageResult<object>> SubirImagen(ProductoImagenPayload payload)
    {
        ValidarArchivo(payload);

        // El empresaId/tenant proviene ÚNICAMENTE del contexto de autenticación.
        // Nunca se recibe ni se confía en un empresaId enviado por el frontend.
        var empresaId = ObtenerEmpresaDesdeToken();
        if (string.IsNullOrWhiteSpace(empresaId))
            throw new ErrorHandler(HttpStatusCode.Forbidden, "No se pudo determinar la empresa del usuario autenticado.");

        // El query filter global (TenantId + Sucursal) aísla el producto por empresa/sede.
        var producto = await dbContext.Producto.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == payload.ProductoId);

        if (producto is null)
            throw new ErrorHandler(HttpStatusCode.NotFound, "El producto no existe o no pertenece a su empresa.");

        var settings = cloudinarySettings.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName) || string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ApiSecret))
            throw new ErrorHandler(HttpStatusCode.InternalServerError, "Cloudinary no está configurado.");

        var cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));

        // Estructura: wuarikes/empresas/{empresaId}/productos/{productoId}/imagen
        var publicId = $"{Nivel(settings.BaseFolder)}/empresas/{Sanitizar(empresaId)}/productos/{producto.Id}/imagen";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(payload.NombreArchivo, new MemoryStream(payload.Contenido)),
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true
        };

        var uploadResult = await cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error is not null)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, $"No se pudo subir la imagen: {uploadResult.Error.Message}");

        var oldPublicId = producto.CloudinaryPublicId;

        try
        {
            var entry = dbContext.Producto.Attach(producto);
            entry.Entity.RutaImagen = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
            entry.Entity.CloudinaryPublicId = uploadResult.PublicId;
            entry.State = EntityState.Modified;

            await dbContext.SaveChangesAsync();
        }
        catch
        {
            // Devuelve el estado anterior; apaga la posible imagen huérfana subida arriba.
            await EjecutarEliminacionCloudinary(cloudinary, uploadResult.PublicId);
            throw new ErrorHandler(HttpStatusCode.InternalServerError, "Ocurrió un error al guardar la imagen del producto.");
        }

        // Recién después de confirmar que la nueva imagen quedó guardada, se elimina la anterior.
        if (!string.IsNullOrWhiteSpace(oldPublicId) && !string.Equals(oldPublicId, uploadResult.PublicId, StringComparison.Ordinal))
            await EjecutarEliminacionCloudinary(cloudinary, oldPublicId);

        var dto = mapper.Map<ProductoDto>(producto);

        return MessageResult<object>.Of("Imagen subida correctamente", dto);
    }

    public async Task<MessageResult<object>> EliminarImagen(int productoId)
    {
        var producto = await dbContext.Producto.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == productoId);

        if (producto is null)
            throw new ErrorHandler(HttpStatusCode.NotFound, "El producto no existe o no pertenece a su empresa.");

        var settings = cloudinarySettings.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName) || string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ApiSecret))
            throw new ErrorHandler(HttpStatusCode.InternalServerError, "Cloudinary no está configurado.");

        if (string.IsNullOrWhiteSpace(producto.CloudinaryPublicId))
        {
            producto.RutaImagen = null;
            producto.CloudinaryPublicId = null;

            var entrySinImagen = dbContext.Producto.Attach(producto);
            entrySinImagen.State = EntityState.Modified;
            await dbContext.SaveChangesAsync();

            return MessageResult<object>.Of("El producto no tenía imagen asociada", mapper.Map<ProductoDto>(producto));
        }

        var cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));

        await EjecutarEliminacionCloudinary(cloudinary, producto.CloudinaryPublicId);

        producto.RutaImagen = null;
        producto.CloudinaryPublicId = null;

        var entry = dbContext.Producto.Attach(producto);
        entry.State = EntityState.Modified;

        await dbContext.SaveChangesAsync();

        return MessageResult<object>.Of("Imagen eliminada correctamente", mapper.Map<ProductoDto>(producto));
    }

    private static async Task EjecutarEliminacionCloudinary(Cloudinary cloudinary, string publicId)
    {
        var deletionResult = await cloudinary.DestroyAsync(new DeletionParams(publicId));

        if (deletionResult.Error is not null)
            throw new ErrorHandler(HttpStatusCode.InternalServerError, $"No se pudo eliminar la imagen anterior: {deletionResult.Error.Message}");
    }

    private static void ValidarArchivo(ProductoImagenPayload payload)
    {
        if (payload.Contenido is null || payload.Contenido.Length == 0)
            throw new ErrorHandler(HttpStatusCode.BadRequest, "Selecciona una imagen para continuar.");

        if (payload.Contenido.Length > MaximoBytes)
            throw new ErrorHandler(HttpStatusCode.BadRequest, "La imagen no puede superar los 5 MB.");

        var extension = Path.GetExtension(payload.NombreArchivo).ToLowerInvariant();

        if (!ExtensionesPermitidas.Contains(extension))
            throw new ErrorHandler(HttpStatusCode.BadRequest, "La imagen debe ser JPG, PNG o WEBP.");

        if (!string.IsNullOrWhiteSpace(payload.TipoContenido)
            && !payload.TipoContenido.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            && !TiposPermitidos.Contains(payload.TipoContenido.ToLowerInvariant()))
            throw new ErrorHandler(HttpStatusCode.BadRequest, "La imagen debe ser JPG, PNG o WEBP.");
    }

    private string? ObtenerEmpresaDesdeToken()
    {
        var claim = httpContextAccessor?.HttpContext?.User?.FindFirstValue("empresa");

        if (!string.IsNullOrWhiteSpace(claim))
            return claim;

        return tenantContextAccessor?.CurrentContext?.TenantKey
               ?? tenantContextAccessor?.CurrentContext?.Name;
    }

    private static string Nivel(string baseFolder)
        => string.IsNullOrWhiteSpace(baseFolder) ? "wuarikes" : baseFolder.Trim();

    private static string Sanitizar(string valor)
    {
        var limpio = System.Text.RegularExpressions.Regex.Replace(valor, "[^A-Za-z0-9_-]", "_");
        return string.IsNullOrWhiteSpace(limpio) ? "empresa" : limpio;
    }
}