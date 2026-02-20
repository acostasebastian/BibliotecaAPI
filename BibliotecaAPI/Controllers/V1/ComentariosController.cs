using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1/libros/{libroId:int}/comentarios")]
    [Authorize]
    public class ComentariosController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IServiciosUsuarios serviciosUsuarios;
        private readonly IOutputCacheStore ouputCacheStore;
        private const string cache = "comentarios-obtener"; //llave de chache para poder limpiarlo manualmente

        public ComentariosController(ApplicationDbContext context, IMapper mapper, IServiciosUsuarios serviciosUsuarios, IOutputCacheStore ouputCacheStore)
        {
            this.context = context;
            this.mapper = mapper;
            this.serviciosUsuarios = serviciosUsuarios;
            this.ouputCacheStore = ouputCacheStore;
        }

        [HttpGet(Name = "ObtenerComentariosV1")] // api/comentarios
        [AllowAnonymous] // permite a cualquiera acceder
        [OutputCache(Tags = [cache])] //para hacer uso del cache. Lo marco con una etiqueta para limpiarlo
        public async Task<ActionResult<List<ComentarioDTO>>> Get(int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync();

            if (!existeLibro)
            {
                return NotFound();
            }

            var comentarios = await context.Comentarios
                              .Include(x => x.Usuario)
                              .Where(x => x.LibroId == libroId)
                              .OrderByDescending(x => x.FechaPublicacion)
                              .ToListAsync();

            return mapper.Map<List<ComentarioDTO>>(comentarios);               
        }

        [HttpGet("{id}",Name = "ObtenerComentarioV1")]
        [AllowAnonymous] // permite a cualquiera acceder
        [OutputCache(Tags = [cache])] //para hacer uso del cache. Lo marco con una etiqueta para limpiarlo
        public async Task<ActionResult<ComentarioDTO>> Get(Guid id)
        {
            var comentario = await context.Comentarios
                                    .Include(x => x.Usuario)

                                    .FirstOrDefaultAsync(x => x.Id == id);

            if (comentario is null)
            {
                return NotFound();
            }

            return mapper.Map<ComentarioDTO>(comentario);
        }

        [HttpPost(Name = "CrearComentariosV1")]
        public async Task<ActionResult>Post(int libroId, ComentarioCreacionDTO comentarioCreacionDTO)
        {
            var existeLibro = await context.Libros.AnyAsync();

            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await serviciosUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }


            var comentario = mapper.Map<Comentario>(comentarioCreacionDTO);
            comentario.LibroId = libroId;
            comentario.FechaPublicacion = DateTime.UtcNow;
            comentario.UsuarioId = usuario.Id;
            context.Add(comentario);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            var comentarioDTO = mapper.Map<ComentarioDTO>(comentario);

            return CreatedAtRoute("ObtenerComentarioV1", new {id = comentario.Id, libroId}, comentarioDTO);
        }

        //El metodo Patch actualiza algunos campos y otros no
        [HttpPatch("{id}", Name = "PatchComentarioV1")]
        public async Task<ActionResult> Patch(Guid id,int libroId, JsonPatchDocument<ComentarioPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var existeLibro = await context.Libros.AnyAsync();

            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await serviciosUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB is null)
            {
                return NotFound();
            }

            if (comentarioDB.UsuarioId != usuario.Id)
            {
                return Forbid(); //si no es el mismo usuario el que está editando el comentario, no lo dejo
            }

            var comentarioPatchDTO = mapper.Map<ComentarioPatchDTO>(comentarioDB);

            patchDoc.ApplyTo(comentarioPatchDTO, ModelState);

            var esValido = TryValidateModel(comentarioPatchDTO);

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(comentarioPatchDTO, comentarioDB);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            return NoContent();
        }

        [HttpDelete("{id}", Name = "BorrarComentarioV1")] //api/comentarios/id...
        public async Task<ActionResult> Delete(Guid id, int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync();

            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await serviciosUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB is null)
            {
                return NotFound();
            }

            if (comentarioDB.UsuarioId != usuario.Id)
            {
                return Forbid(); //si no es el mismo usuario el que está editando el comentario, no lo dejo
            }

            comentarioDB.EstaBorrado = true;

            context.Update(comentarioDB);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor

            return NoContent();    //devuelve un 204, esta todo OK pero sin devolver contenido
        }
    }
}
