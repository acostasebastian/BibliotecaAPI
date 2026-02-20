using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.PRUEBAS;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Servicios.V1;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Data;
using System.Linq.Dynamic.Core;

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController]
    [Route("api/v2/autores")]
    [Authorize(Policy = "esadmin")]
    //[FiltroAgregarCabeceras("controlador","autores")] //Filtro con parámetros
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly ILogger<AutoresController> logger;
        private readonly IOutputCacheStore ouputCacheStore;
        private readonly IServicioAutores servicioAutoresV1;
        private const string contenedor = "autores"; //carpeta por defecto donde guardar las imagenes o archivos
        private const string cache = "autores-obtener"; //llave de chache para poder limpiarlo manualmente


        public AutoresController(ApplicationDbContext context
            , IMapper mapper, IAlmacenadorArchivos almacenadorArchivos
            , ILogger<AutoresController> logger
            ,IOutputCacheStore ouputCacheStore, IServicioAutores servicioAutoresV1
            )
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.logger = logger;
            this.ouputCacheStore = ouputCacheStore;
            this.servicioAutoresV1 = servicioAutoresV1;
        }
        
        [HttpGet] // api/autores
        [AllowAnonymous] // permite a cualquiera acceder
        [OutputCache(Tags = [cache])] //para hacer uso del cache. Lo marco con una etiqueta para limpiarlo
       // [ServiceFilter<MiFiltroDeAccion>()] // para usar los filtros como un servicio
        //[FiltroAgregarCabeceras("accion", "obtener-autores")] //Filtro con parámetros
        public async Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO) //con paginado
        {          
            return await servicioAutoresV1.Get(paginacionDTO);
        }

        [HttpGet("{id:int}", Name = "ObtenerAutorV2")] // api/autores/id
        [AllowAnonymous] // permite a cualquiera acceder

        //FORMAS DE DOCUMENTACION EN SWAGGER LOS ENDPOINTS
        [EndpointSummary("Obtiene autor por Id")]
        [EndpointDescription("Obtiene un autor por su Id. Incluye sus libros. Si el autor no existe, retorna un 404")]
        [ProducesResponseType<AutorConLibrosDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [OutputCache(Tags = [cache])] //para hacer uso del cache. Lo marco con una etiqueta para limpiarlo
        public async Task<ActionResult<AutorConLibrosDTO>> Get([Description("El id del autor")]int id, bool incluirLibros = false)
        {
            //ejecucion diferida: es cuando yo voy armando la query
            var queryable = context.Autores.AsQueryable();

            if (incluirLibros)
            {
                queryable = queryable.Include(X => X.Libros)
                .ThenInclude(x => x.Libro);
            }

                
             var autor = await queryable.FirstOrDefaultAsync(x => x.Id == id);            

            if (autor is null)
            {
                return NotFound();
            }

            var autorDTO = mapper.Map<AutorConLibrosDTO>(autor);

            return autorDTO;
        }
        [HttpGet("filtrar")] 
        [AllowAnonymous] // permite a cualquiera acceder
        public async Task<ActionResult> Filtrar([FromQuery] AutorFiltroDTO autorFiltroDTO)
        {
           var queryable = context.Autores.AsQueryable();

            if (!string.IsNullOrEmpty(autorFiltroDTO.Nombres))
            {
                queryable = queryable.Where(x => x.Nombres.Contains(autorFiltroDTO.Nombres));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.Apellidos))
            {
                queryable = queryable.Where(x => x.Apellidos.Contains(autorFiltroDTO.Apellidos));
            }

            if (autorFiltroDTO.IncluirLibros)
            {
                queryable = queryable.Include(x => x.Libros).ThenInclude(x => x.Libro);
            }

            if (autorFiltroDTO.TieneFoto.HasValue)
            {
                if (autorFiltroDTO.TieneFoto.Value)
                {
                    queryable = queryable.Where(x => x.Foto != null);
                }
                else
                {
                    queryable = queryable.Where(x => x.Foto == null);
                }
            }

            if (autorFiltroDTO.TieneLibros.HasValue)
            {
                if (autorFiltroDTO.TieneLibros.Value)
                {
                    queryable = queryable.Where(x => x.Libros.Any());
                }
                else
                {
                    queryable = queryable.Where(x => !x.Libros.Any());
                }
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.TituloLibro))
            {
                queryable = queryable.Where(x =>
                                     x.Libros.Any(y => y.Libro!.Titulo.Contains(autorFiltroDTO.TituloLibro)));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.CampoOrdenar))
            {
                var tipoOrden = autorFiltroDTO.OrdenarAscendente ? "ascending" : "descending";


                try
                {
                    queryable = queryable.OrderBy($"{autorFiltroDTO.CampoOrdenar} {tipoOrden}");
                }
                catch (Exception ex)
                {
                    queryable = queryable.OrderBy(x => x.Nombres);
                    logger.LogError(ex.Message, ex);
                }
             
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Nombres);
            }

                var autores = await queryable                    
                    .Paginar(autorFiltroDTO.PaginacionDTO).ToListAsync();

            if (autorFiltroDTO.IncluirLibros)
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorConLibrosDTO>>(autores);
                return Ok(autoresDTO);
            }
            else
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
                return Ok(autoresDTO);
            }

         
        }

        [HttpPost]
        public async Task<ActionResult> Post(AutorCreacionDTO autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            context.Add(autor);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV2", new {id = autor.Id}, autorDTO); //devuelve un 201, Created
        }

        [HttpPost("con-foto")]
        public async Task<ActionResult> PostConFoto([FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);

            if (autorCreacionDTO.Foto is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;   
            }

            context.Add(autor);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV2", new { id = autor.Id }, autorDTO); //devuelve un 201, Created
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, [FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var existeAutor = await context.Autores.AnyAsync(x => x.Id == id);

            if (!existeAutor)
            {
                return NotFound();
            }

            var autor = mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;

            if (autorCreacionDTO.Foto is not null)
            {
                var fotoActual = await context.Autores.Where(x => x.Id == id).Select(x => x.Foto).FirstAsync();
                var url = await almacenadorArchivos.Editar(fotoActual,contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            context.Update(autor);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            return NoContent();    //devuelve un 204, esta todo OK pero sin devolver contenido

        }


        //El metodo Patch actualiza algunos campos y otros no
        // en el body >> [{ "op":"replace", "path":"nombres","value":"Actaulización"}] >>> puede ser mas de uno, y lo ponemos c/u entre {}
        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AutorPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var autorDB = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autorDB is null)
            {
                return NotFound();
            }

            var autorPatchDTO = mapper.Map<AutorPatchDTO>(autorDB);

            patchDoc.ApplyTo(autorPatchDTO, ModelState);

            var esValido = TryValidateModel(autorPatchDTO);

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(autorPatchDTO, autorDB);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            return NoContent(); 
        }

            [HttpDelete("{id:int}")] //api/autores/id...
        public async Task<ActionResult> Delete(int id)
        {
            var autor = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);
            if (autor is null )
            {
                return NotFound();
            }

            context.Remove(autor);
            await context.SaveChangesAsync();
            await ouputCacheStore.EvictByTagAsync(cache, default); //limpio el cache cuando creo un nuevo autor
            await almacenadorArchivos.Borrar(autor.Foto, contenedor);
            return NoContent();    //devuelve un 204, esta todo OK pero sin devolver contenido
        }
    }
}
