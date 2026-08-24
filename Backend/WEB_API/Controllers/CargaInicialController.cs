using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CargaInicialController : ControllerBase
    {
        private readonly ICargaInicialService cargaInicialService;

        public CargaInicialController(ICargaInicialService cargaInicialService)
        {
            this.cargaInicialService = cargaInicialService;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearCargaInicial([FromBody] int RubroId, string TenantId) => Ok(await cargaInicialService.CrearDataInicialRubro(RubroId, TenantId));
        [HttpGet("listar-categoria-rubro")]
        public async Task<IActionResult> GetCategoriaRubro([FromBody] int RubroId, string TenantId) => Ok(await cargaInicialService.GetCategoriaRubro(RubroId, TenantId));

    }


}

