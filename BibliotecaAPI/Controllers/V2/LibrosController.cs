using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController]
    [Route("api/v2/libros")]
    [Authorize(Policy = "esadmin")]
    public class LibrosController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore ouputCacheStore;
        private const string cache = "libros-obtener"; //llave de chache para poder limpiarlo manualmente

        //private readonly ITimeLimitedDataProtector protectorLimitadoPorTiempo;

        public LibrosController(ApplicationDbContext context, IMapper mapper
             //, IDataProtectionProvider protectionProvider
             , IOutputCacheStore ouputCacheStore
            )
        {
            this.context = context;
            this.mapper = mapper;
            this.ouputCacheStore = ouputCacheStore;
            //protectorLimitadoPorTiempo = protectionProvider.CreateProtector("LibroController").ToTimeLimitedDataProtector();
        }


        [HttpGet] // api/libros
        [AllowAnonymous] // permite a cualquiera acceder
        [OutputCache(Tags = [cache])] //para hacer uso del cache. Lo marco con una etiqueta para limpiarlo
        public async Task<IEnumerable<LibroDTO>> Get([FromQuery] PaginacionDTO paginacionDTO) //con paginado
        {
            var queryable = context.Libros.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            var libros = await queryable
                .OrderBy(x => x.Titulo)
                .Paginar(paginacionDTO).ToListAsync();

            var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);

            return librosDTO;
        }

        [HttpGet("{id:int}", Name = "ObtenerLibroV2")] // api/libros/id
        [AllowAnonymous] // permite a cualquiera acceder
        [OutputCache(Tags = [cache])] //para hacer uso del cache. Lo marco con una etiqueta para limpiarlo
        public async Task<ActionResult<LibroConAutoresDTO>> Get(int id)
        {
            var libro = await context.Libros
                .Include(x => x.Autores)
                .ThenInclude(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (libro is null)
            {
                return NotFound();
            }

            var libroDTO = mapper.Map<LibroConAutoresDTO>(libro);

            return libroDTO;
        }

        [HttpPost]
        //Creando libros con los distintos autores >> Relacion Muchos a Muchos con AutorLibro (tabla debil)
        [ServiceFilter<FiltroValidacionLibro>]
        public async Task<ActionResult> Post(LibroCreacionDTO libroCreacionDTO)
        {  //La validacion que habia paso a estar en la clase FiltroValidacionLibro

            var libro = mapper.Map<Libro>(libroCreacionDTO);
            AsignarOrdenAutores(libro);
      
            context.Add(libro);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor

            var libroDTO = mapper.Map<LibroDTO>(libro);

            return CreatedAtRoute("ObtenerLibroV2", new { id = libro.Id }, libroDTO);
        }

        private void AsignarOrdenAutores(Libro libro)
        {
            if (libro.Autores is not null)
            {
                for (int i = 0; i < libro.Autores.Count; i++)
                {
                    libro.Autores[i].Orden = i;
                }

            }
        }

        [HttpPut("{id:int}")]
        //Editando libros con los distintos autores >> Relacion Muchos a Muchos con AutorLibro (tabla debil)
        [ServiceFilter<FiltroValidacionLibro>] // para usar como filtro la clase FiltroValidacionLibro de Utilidades
        public async Task<ActionResult> Put(int id, LibroCreacionDTO libroCreacionDTO)
        {
            

            var libroDB = await context.Libros
                             .Include(x => x.Autores)
                             .FirstOrDefaultAsync(x => x.Id == id);

            if (libroDB is null) 
            {

                return NotFound();            
            }

            //Por estar tomando los datos del SQL (libroDB), no es necesario hacer el update, basta solo con el SaveChanges
            libroDB = mapper.Map(libroCreacionDTO, libroDB);
            AsignarOrdenAutores(libroDB);

            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            return NoContent();    //devuelve un 204, esta todo OK pero sin devolver contenido

        }

        [HttpDelete("{id:int}")] //api/libros/id...
        public async Task<ActionResult> Delete(int id)
        {
            var registrosBorrados = await context.Libros.Where(x => x.Id == id).ExecuteDeleteAsync();
            if (registrosBorrados == 0)
            {
                return NotFound();
            }

            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            //context.Remove(new Autor() { Id = id });
            //await context.SaveChangesAsync();
            return NoContent();    //devuelve un 204, esta todo OK pero sin devolver contenido
        }

        #region Pruebas
        //[HttpGet("listado/obtener-token")]
        //public IActionResult ObtenerTokenListado()
        //{
        //    var textoPlano = Guid.NewGuid().ToString();
        //    string token = protectorLimitadoPorTiempo.Protect(textoPlano, lifetime: TimeSpan.FromSeconds(30));
        //    var url = Url.RouteUrl("ObtenerListadoLibroUsandoToken", new { token }, "https");
        //    return Ok(new { url });
        //}

        //[HttpGet("listado/{token}", Name = "ObtenerListadoLibroUsandoToken")] // api/libros
        //[AllowAnonymous]
        //public async Task<ActionResult> ObtenerListadoUsandoToken(string token)
        //{
        //    try
        //    {
        //        protectorLimitadoPorTiempo.Unprotect(token);
        //    }
        //    catch
        //    {

        //        ModelState.AddModelError(nameof(token), "El token ha expirado");
        //        return ValidationProblem();
        //    }

        //    var libros = await context.Libros.ToListAsync();
        //    var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);

        //    return Ok(librosDTO);
        //}
        #endregion
    }
}
