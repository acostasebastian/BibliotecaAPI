using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static StackExchange.Redis.Role;

namespace BibliotecaAPI.Controllers.V1
{
    [Route("api/v1/restriccionesdominio")]
    [Authorize]
    [ApiController]
    [DeshabilitarLimitarPeticiones]
    public class RestriccionesDominioController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IServiciosUsuarios serviciosUsuarios;

        public RestriccionesDominioController(ApplicationDbContext context, IServiciosUsuarios serviciosUsuarios)
        {
            this.context = context;
            this.serviciosUsuarios = serviciosUsuarios;
        }

        [HttpPost]
        public async Task<ActionResult> Post(RestriccionDominioCreacionDTO restriccionDominioCreacionDTO)
        {
            
            var llaveBD = await context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == restriccionDominioCreacionDTO.LlaveId);

            if (llaveBD is null)
            {
                return NotFound();
            }

            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            if (llaveBD.UsuarioId != usuarioId)
            {
                return Forbid();
            }

            var restriccionDominio = new RestriccionDominio
            {
                LlaveId = restriccionDominioCreacionDTO.LlaveId,
                Dominio = restriccionDominioCreacionDTO.Dominio,
            };


            context.Add(restriccionDominio);
            await context.SaveChangesAsync();
            return NoContent();
        }


        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, RestriccionDominioActualizacionDTO restriccionDominioActualizacionDTO)
        {

            var restriccionBD = await context.RestriccionesDominio.Include(x => x.Llave)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (restriccionBD is null)
            {
                return NotFound();
            }

            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            if (restriccionBD.Llave!.UsuarioId != usuarioId)
            {
                return Forbid();
            }

           restriccionBD.Dominio = restriccionDominioActualizacionDTO.Dominio;
            
            await context.SaveChangesAsync();
            return NoContent();
        }


        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {

            var restriccionBD = await context.RestriccionesDominio.Include(x => x.Llave)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (restriccionBD is null)
            {
                return NotFound();
            }

            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            if (restriccionBD.Llave!.UsuarioId != usuarioId)
            {
                return Forbid();
            }

            context.Remove(restriccionBD);

            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
