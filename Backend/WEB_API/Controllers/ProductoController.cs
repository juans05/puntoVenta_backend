using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers;

[Route("api/productos")]
[ApiController]
public class ProductoController : ControllerBase
{
    private readonly IProductService productService;

    public ProductoController(IProductService productService)
    {
        this.productService = productService;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> CrearProducto([FromBody] CreateProductPayload payload) => Ok(await productService.CrearProducto(payload));

    [HttpPut("modificar")]
    public async Task<IActionResult> ModificarProducto([FromBody] UpdateProductPayload payload) => Ok(await productService.ModificarProducto(payload));
    
    [HttpDelete("eliminar")]
    public async Task<IActionResult> EliminarProducto([FromQuery] int IdProducto) => Ok(await productService.EliminarProducto(IdProducto));

    [HttpGet("listar")]
    public async Task<IActionResult> GetListarProducto([FromQuery] ProductPayload payload) => Ok(await productService.ListarProductos(payload));

    [HttpPost("importar/previsualizar")]
    public async Task<IActionResult> PrevisualizarImportacion([FromBody] ImportProductosPayload payload) => Ok(await productService.PrevisualizarImportacion(payload));

    [HttpPost("importar/confirmar")]
    public async Task<IActionResult> ImportarProductos([FromBody] ImportProductosPayload payload) => Ok(await productService.ImportarProductos(payload));

}


