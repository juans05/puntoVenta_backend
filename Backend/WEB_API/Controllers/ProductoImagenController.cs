using Application.Interfaces.IServices;
using Domain.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace WEB_API.Controllers;

[Route("api/productos/imagen")]
[ApiController]
public class ProductoImagenController : ControllerBase
{
    private const long MaximoBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IProductoImagenService productoImagenService;

    public ProductoImagenController(IProductoImagenService productoImagenService)
    {
        this.productoImagenService = productoImagenService;
    }

    [HttpPost("subir")]
    public async Task<IActionResult> SubirImagen([FromForm] int productoId, IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Selecciona una imagen para continuar." });

        if (archivo.Length > MaximoBytes)
            return BadRequest(new { message = "La imagen no puede superar los 5 MB." });

        using var memoryStream = new MemoryStream();
        await archivo.CopyToAsync(memoryStream);
        var contenido = memoryStream.ToArray();

        // El tipo se deduce de los bytes, no del ContentType que manda el cliente (se puede falsear).
        var tipo = TipoImagen(contenido);
        if (tipo is null)
            return BadRequest(new { message = "Solo se permiten imágenes JPG, PNG, WEBP o GIF." });

        var payload = new ProductoImagenPayload
        {
            ProductoId = productoId,
            NombreArchivo = Path.GetFileName(archivo.FileName),
            TipoContenido = tipo,
            Contenido = contenido
        };

        return Ok(await productoImagenService.SubirImagen(payload));
    }

    private static string? TipoImagen(byte[] b)
    {
        bool Empieza(params byte[] firma) => b.Length >= firma.Length && b.Take(firma.Length).SequenceEqual(firma);
        if (Empieza(0xFF, 0xD8, 0xFF)) return "image/jpeg";
        if (Empieza(0x89, 0x50, 0x4E, 0x47)) return "image/png";
        if (Empieza(0x47, 0x49, 0x46, 0x38)) return "image/gif";
        if (b.Length >= 12 && Empieza(0x52, 0x49, 0x46, 0x46) && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
        return null;
    }

    [HttpDelete("eliminar")]
    public async Task<IActionResult> EliminarImagen([FromQuery] int productoId)
    {
        return Ok(await productoImagenService.EliminarImagen(productoId));
    }
}