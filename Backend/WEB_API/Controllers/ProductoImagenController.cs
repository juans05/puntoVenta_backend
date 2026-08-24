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

        var payload = new ProductoImagenPayload
        {
            ProductoId = productoId,
            NombreArchivo = archivo.FileName,
            TipoContenido = archivo.ContentType,
            Contenido = memoryStream.ToArray()
        };

        return Ok(await productoImagenService.SubirImagen(payload));
    }

    [HttpDelete("eliminar")]
    public async Task<IActionResult> EliminarImagen([FromQuery] int productoId)
    {
        return Ok(await productoImagenService.EliminarImagen(productoId));
    }
}