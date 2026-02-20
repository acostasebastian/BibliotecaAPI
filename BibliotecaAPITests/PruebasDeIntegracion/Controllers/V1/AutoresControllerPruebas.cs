using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPITests.Utilidades;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace BibliotecaAPITests.PruebasDeIntegracion.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas : BasePruebas
    {
        private static readonly string url = "/api/v1/autores";
        private string nombreBD = Guid.NewGuid().ToString();

        [TestMethod]
        public async Task Get_Devuelve404_CuandoAutorNoExiste()
        {
            //Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            //Prueba 
            var respuesta = await cliente.GetAsync($"{url}/1");

            //Verificación
            var statusCode = respuesta.StatusCode;
            Assert.AreEqual(expected: HttpStatusCode.NotFound, actual: respuesta.StatusCode);
        }


        [TestMethod]
        public async Task Get_DevuelveAutor_CuandoAutorExiste()
        {
            //Preparación

            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor() { Nombres = "Sebastian", Apellidos = "Acosta" });
            context.Autores.Add(new Autor() { Nombres = "Flavia", Apellidos = "Ceballos" });
            await context.SaveChangesAsync();

            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            //Prueba 
            var respuesta = await cliente.GetAsync($"{url}/1");

            //Verificación
            respuesta.EnsureSuccessStatusCode();
            var autor = JsonSerializer.Deserialize<AutorConLibrosDTO>(
                await respuesta.Content.ReadAsStringAsync(), jsonSerializerOptions)!;

            
            Assert.AreEqual(expected: 1, actual: autor.Id);
        }



        [TestMethod]
        public async Task Post_Devuelve401_CuandoUsuarioNoEstaAutenticado()
        {
            //Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var cliente = factory.CreateClient();
            var autorCreacionDTO = new AutorCreacionDTO
            { 
                Nombres = "Homero",
                Apellidos = "Simpsons",
                Identificacion = "123"
            };

            //Prueba 
            // se usa PostAsJsonAsync en vez de Post para enviar el autor como un Json
            var respuesta = await cliente.PostAsJsonAsync(url,autorCreacionDTO);

            //Verificación            
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelve403_CuandoUsuarioNoEsAdmin()
        {
            //Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            //se crea un token, sin importar claims y Email
            var token = await CrearUsuario(nombreBD, factory);

            var cliente = factory.CreateClient();

            //Envio en la cabecera, el token generado
            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Homero",
                Apellidos = "Simpsons",
                Identificacion = "123"
            };

            //Prueba 
            // se usa PostAsJsonAsync en vez de Post para enviar el autor como un Json
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            //Verificación            
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }


        [TestMethod]
        public async Task Post_Devuelve201_CuandoUsuarioEsAdmin()
        {
            //Preparación
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var claims = new List<Claim> { adminClaim };

            //se crea un token, sin importar con claim y Email
            var token = await CrearUsuario(nombreBD, factory,claims);

            var cliente = factory.CreateClient();

            //Envio en la cabecera, el token generado
            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Homero",
                Apellidos = "Simpsons",
                Identificacion = "123"
            };

            //Prueba 
            // se usa PostAsJsonAsync en vez de Post para enviar el autor como un Json
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            //Verificación
            respuesta.EnsureSuccessStatusCode();    

            Assert.AreEqual(expected: HttpStatusCode.Created, actual: respuesta.StatusCode);
        }



    }
}
