using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearCategoria([FromBody] CreateCategoryPayload payload) => Ok(await categoryService.CrearCategoria(payload));
        [HttpPut("modificar")]
        public async Task<IActionResult> ModificarProducto(UpdateCategoryPayload payload) => Ok(await categoryService.ModificarCategoria(payload));
        [HttpDelete("eliminar")]
        public async Task<IActionResult> EliminarProducto([FromQuery] int IdCategoria) => Ok(await categoryService.EliminarCategoria(IdCategoria));

        [HttpGet("listar")]
        public async Task<IActionResult> GetListarCateforia([FromQuery] CategoryPayload payload) => Ok(await categoryService.GetCategoria(payload));

    }


}

