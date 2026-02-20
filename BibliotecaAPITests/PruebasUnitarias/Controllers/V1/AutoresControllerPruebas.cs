using Azure;
using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Servicios.V1;
using BibliotecaAPITests.Utilidades;
using BibliotecaAPITests.Utilidades.Dobles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;

namespace BibliotecaAPITests.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas : BasePruebas
    {
        //estas aunque no se usen en el metodo GET se deben pasar nulas
        IAlmacenadorArchivos almacenadorArchivos = null!;
        ILogger<AutoresController> logger = null!;
        IOutputCacheStore ouputCacheStore = null!;
        IServicioAutores servicioAutoresV1 = null!;
        private string nombreBD = Guid.NewGuid().ToString();
        private AutoresController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            //variables que se pasan en el constructor del controlador            
            var context = ConstruirContext(nombreBD);
            var mapper = ConfigurarAutoMapper();

            //con esta libreria agregada en Nugget, se simula llamar al servicio de autores
            almacenadorArchivos = Substitute.For<IAlmacenadorArchivos>();
            logger = Substitute.For<ILogger<AutoresController>>();
            ouputCacheStore = Substitute.For<IOutputCacheStore>();   
            servicioAutoresV1 = Substitute.For<IServicioAutores>();

            controller = new AutoresController(context, mapper, almacenadorArchivos, logger, ouputCacheStore, servicioAutoresV1);

        }



        [TestMethod]
        public async Task Get_Retorna404_CuandoAutorConIdNoExiste()
        {
            //Preparación 
            //Se crean en el metodo Setup

            

            //Prueba
            var respuesta = await controller.Get(1);


            //Verificacion
            var resultado = respuesta.Result as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Get_RetornaAutor_CuandoAutorConIdExiste()
        {
            //Preparación
            var context = ConstruirContext(nombreBD);            
            //creo un autor cualquiera, para saber que si falla el metodo no es porque falló el metodo POST por ejemplo
            context.Autores.Add(new Autor { Nombres = "Homero", Apellidos = "Simpsons" });
            context.Autores.Add(new Autor { Nombres = "March", Apellidos = "Bubbie" });

            await context.SaveChangesAsync();

            ////Creo otro contexto, para que sea 1 para crear los autores y otro para probar el metodo (el testing)
            //var context2 = ConstruirContext(nombreBD);

            //var controller = new AutoresController(context2, mapper, almacenadorArchivos, logger, ouputCacheStore, servicioAutoresV1);
            
            //Prueba
            var respuesta = await controller.Get(1);


            //Verificacion
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
        }

        [TestMethod]
        public async Task Get_RetornaAutorConLibros_CuandoAutorTieneLibros()
        {
            //Preparación

            //variables que se pasan en el constructor del controlador         
            var context = ConstruirContext(nombreBD);            
            var libro1 = new Libro { Titulo = "Libro 1" };
            var libro2 = new Libro { Titulo = "Libro 2" };

            var autor = new Autor()
            { 
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Libros = new List<AutorLibro>
                {
                    new AutorLibro { Libro = libro1},
                    new AutorLibro { Libro = libro2}
                }
            };

            context.Add(autor);

            await context.SaveChangesAsync();           

            //Prueba
            var respuesta = await controller.Get(1);

            //Verificacion
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);

            Assert.AreEqual(expected:2, actual: resultado.Libros.Count);
        }

        [TestMethod]
        public async Task Get_DebeLlamarGetDelServicioAutores()
        {
            //Preparación           
            var paginacionDTO = new PaginacionDTO(2,3);

            //Prueba
            var respuesta = await controller.Get(paginacionDTO);


            //Verificacion
            await servicioAutoresV1.Received(1).Get(paginacionDTO);
        }

        [TestMethod]
        public async Task Post_DebeCrearAutor_CuandoEnviamosAutor()
        {
            //Preparación

            //variables que se pasan en el constructor del controlador            
            var context = ConstruirContext(nombreBD);            
            var nuevoAutor = new AutorCreacionDTO { Nombres = "Bart", Apellidos = "Simpsons" };                   

            //Prueba
            var respuesta = await controller.Post(nuevoAutor);


            //Verificacion
            var resultado = respuesta as CreatedAtRouteResult;
            Assert.IsNotNull(resultado);

            var context2 = ConstruirContext(nombreBD);
            var cantidad = await context2.Autores.CountAsync();

            Assert.AreEqual(expected: 1, actual: cantidad);
        }

        [TestMethod]
        public async Task Put_Retorna404_CuandoAutorNoExiste()
        {                 

            //Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO:null);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404,resultado!.StatusCode);
            
        }

        private const string contenedor = "autores"; //carpeta por defecto donde guardar las imagenes o archivos
        private const string cache = "autores-obtener"; //llave de chache para poder limpiarlo manualmente

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoEnviamosAutorSinFoto()
        {
            //Preparación
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor 
                            { Nombres = "Bart",
                              Apellidos = "Simpsons", 
                              Identificacion = "Id"
                            });
            await context.SaveChangesAsync();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Bart2",
                Apellidos = "Simpsons2",
                Identificacion = "Id2"
            };

            //Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context2 = ConstruirContext(nombreBD);
            var autorActualizado = await context2.Autores.SingleAsync();

            Assert.AreEqual(expected: "Bart2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Simpsons2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);

            await ouputCacheStore.Received(1).EvictByTagAsync(cache,default); //para verificar que se limpie el cache
            await almacenadorArchivos.DidNotReceiveWithAnyArgs().Editar(default, default!, default!); //para verificar que no se ejecute el editar de la foto

        }

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoEnviamosAutorConFoto()
        {
            //Preparación
            var context = ConstruirContext(nombreBD);

            var urlAnterior = "URL-1";
            var urlNueva = "URL-2";
            almacenadorArchivos.Editar(default, default!, default!).ReturnsForAnyArgs(urlNueva);


            context.Autores.Add(new Autor
            {
                Nombres = "Bart",
                Apellidos = "Simpsons",
                Identificacion = "Id",
                Foto = urlAnterior,
            });
            await context.SaveChangesAsync();

            var formFile = Substitute.For<IFormFile>();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Bart2",
                Apellidos = "Simpsons2",
                Identificacion = "Id2",
                Foto = formFile
            };

            //Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context2 = ConstruirContext(nombreBD);
            var autorActualizado = await context2.Autores.SingleAsync();

            Assert.AreEqual(expected: "Bart2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Simpsons2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            Assert.AreEqual(expected: urlNueva, actual: autorActualizado.Foto);

            await ouputCacheStore.Received(1).EvictByTagAsync(cache, default); //para verificar que se limpie el cache
            await almacenadorArchivos.Received(1).Editar(urlAnterior, contenedor, formFile); //para verificar que no se ejecute el editar de la foto

        }

        [TestMethod]
        public async Task Patch_Retorna400_CuandoPatchDocEsNulo()
        {

            //Prueba
            var respuesta = await controller.Patch(1, patchDoc: null!);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(400, resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Patch_Retorna404_CuandoAutorNoExiste()
        {
            //Preparación
            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            //Prueba
            var respuesta = await controller.Patch(1, patchDoc);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Patch_RetornaValidationProblem_CuandoHayErrorDeValidacion()
        {
            //Preparación
            var context = ConstruirContext(nombreBD);         

            context.Autores.Add(new Autor
            {
                Nombres = "Bart",
                Apellidos = "Simpsons",
                Identificacion = "Id"                
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var mensajeDeError = "mensaje de error";
            controller.ModelState.AddModelError("", mensajeDeError);


            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            //Prueba
            var respuesta = await controller.Patch(1, patchDoc);


            //Verificacion
            var resultado = respuesta as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected:1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, actual: problemDetails.Errors.Values.First().First());

        }

        [TestMethod]
        public async Task Patch_ActualizaUnCampo_CuandoSeLeEnviaUnaOperacion()
        {
            //Preparación
            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor
            {
                Nombres = "Bart",
                Apellidos = "Simpsons",
                Identificacion = "Id",
                Foto = "URL-1"
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;
                       


            //var patchDoc = new JsonPatchDocument<AutorPatchDTO>();
            //patchDoc.Operations.Add(new Operation<AutorPatchDTO>("replace", "/nombres", null, "Bart2"));


            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Esta es la forma correcta y recomendada:
            patchDoc.Replace(x => x.Nombres, "Bart2");

            //Prueba
            var respuesta = await controller.Patch(1, patchDoc);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;            
            Assert.AreEqual(expected: 204, actual: resultado!.StatusCode);

            await ouputCacheStore.Received(1).EvictByTagAsync(cache, default);

            var context2 = ConstruirContext(nombreBD);
            var autorBD = await context2.Autores.SingleAsync();

            Assert.AreEqual(expected: "Bart2", actual: autorBD.Nombres);
            Assert.AreEqual(expected: "Simpsons", actual: autorBD.Apellidos);
            Assert.AreEqual(expected: "Id", actual: autorBD.Identificacion);
            Assert.AreEqual(expected: "URL-1", actual: autorBD.Foto);

        }

        [TestMethod]
        public async Task Delete_Retorna404_CuandoAutorNoExiste()
        {         

            //Prueba
            var respuesta = await controller.Delete(1);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Delete_BorraAutor_CuandoAutorExiste()
        {
            //Preparación
            var urlFoto = "URL-1";
            
            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor {Nombres = "Autor1",Apellidos = "Autor1",Foto = urlFoto});
            context.Autores.Add(new Autor {Nombres = "Autor2", Apellidos = "Autor2" });

            await context.SaveChangesAsync();


            //Prueba
            var respuesta = await controller.Delete(1);


            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);


            var context2 = ConstruirContext(nombreBD);
            var cantidadAutores = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidadAutores); //porque despues de borrar un autor, solo me debe quedar 1

            var autor2Existe = await context2.Autores.AnyAsync(x => x.Nombres == "Autor2");
            Assert.IsTrue(autor2Existe);

            await ouputCacheStore.Received(1).EvictByTagAsync(cache, default); //para verificar que se limpie el cache
            await almacenadorArchivos.Received(1).Borrar(urlFoto, contenedor);

        }


    }
}
