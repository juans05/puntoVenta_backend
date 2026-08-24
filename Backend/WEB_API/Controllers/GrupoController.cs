using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IServices;
using Domain.Payloads;

namespace WEB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupoController : ControllerBase
    {
        private readonly IGrupoService grupoService;

        public GrupoController(IGrupoService grupoService)
        {
            this.grupoService = grupoService;
        }
        [HttpPost("crear")]
        public async Task<IActionResult> CrearGrupo([FromBody] CreateGrupoPayload payload) => Ok(await grupoService.CrearGrupo(payload));
        [HttpPut("modificar")]
        public async Task<IActionResult> ModificarGrupo(UpdateGrupoPayload payload) => Ok(await grupoService.ModificarGrupo(payload));
        [HttpDelete("eliminar")]
        public async Task<IActionResult> EliminarGrupo([FromQuery] int IdGrupo) => Ok(await grupoService.EliminarGrupo(IdGrupo));

        [HttpGet("listar")]
        public async Task<IActionResult> GetListarGrupo([FromQuery] GrupoPayload payload) => Ok(await grupoService.GetGrupo(payload));

    }


}

