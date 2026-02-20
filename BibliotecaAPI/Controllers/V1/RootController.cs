using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/V1")]
    [Authorize] //(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)
    public class RootController : ControllerBase
    {
        private readonly IAuthorizationService authorizationService;

           
        public RootController(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }

        [HttpGet(Name = "ObtenerRootV1")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<DatosHATEOASDTO>>> Get()
        {
            var datosHateoas = new List<DatosHATEOASDTO>();

            var esAdmin = await authorizationService.AuthorizeAsync(User, "esAdmin"); //esAdmin es el nombre de la politica que hemos creado antes

            //acciones que cualquiera puede realizar

            datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("ObtenerRootV1", new { })!,
                Descripcion: "self", Metodo: "GET")); //self es la ruta donde nos encontramos, en este caso localhost/api


            datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("ObtenerAutoresV1", new { })!,
                Descripcion: "autores-obtener", Metodo: "GET")); // en esta caso, la descripción de Autores es para ir a otro controller

            datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("RegistroUsuarioV1", new { })!, Descripcion: "usuario-registrar",
            Metodo: "POST"));

            datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("LoginUsuarioV1", new { })!, Descripcion: "usuario-login",
            Metodo: "POST"));

            //AQUI DEBAJO IRIAN TODAS LAS RUTAS DE NUESTROS CONTROLLADORES CON SUS METODOS GET O POST, LAS DE ACTUALIZAR O BORRAR UN AUTOR, LE PERTENECEN AL PROPIO AUTOR

            if (User.Identity!.IsAuthenticated)
            {
                //Acciones para usuarios logueados
                datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("ActualizarUsuarioV1", new { })!, Descripcion: "usuario-actualizar",
            Metodo: "POST"));

                datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("RenovarTokenV1", new { })!, Descripcion: "token-renovar",
            Metodo: "POST"));
            }

            if (esAdmin.Succeeded)
            {
               // Acciones que solo los administradores pueden realizar
                datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("CrearAutorV1", new { })!, Descripcion: "autor-crear",
               Metodo: "POST"));

                datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("CrearAutoresV1", new { })!, Descripcion: "autores-crear",
               Metodo: "POST"));

                datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("CrearLibroV1", new { })!, Descripcion: "libro-crear",
                    Metodo: "POST"));

                datosHateoas.Add(new DatosHATEOASDTO(Enlace: Url.Link("ObtenerUsuariosV1", new { })!, Descripcion: "usuarios-obtener",
                    Metodo: "GET"));
            }

            return datosHateoas;
        }
    }
}
