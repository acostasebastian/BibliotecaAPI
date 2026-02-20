using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Identity.Client;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace BibliotecaAPITests.PruebasUnitarias.Servicios
{
    [TestClass]
    public class ServiciosUsuariosPruebas
    {
        private UserManager<Usuario> userManager = null!;
        private IHttpContextAccessor contextAccessor = null!;
        private ServiciosUsuarios serviciosUsuarios = null!;

        [TestInitialize]
        public void Setup()
        {

            //Simular el constructor de la clase ServiciosUsuarios

            //el IUserStore es una clase que permite indicar el comportamiento de las distintas acciones de Identity
            //el cual funciona "solo" con el Identity.. pero con el substitute lo "suplantamos" para hacer pruebas
            userManager = Substitute.For<UserManager<Usuario>>(
               Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);

            contextAccessor = Substitute.For<IHttpContextAccessor>();
            serviciosUsuarios = new ServiciosUsuarios(userManager, contextAccessor);


        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoNoHayClaimEmail()
        {
            //Preparación
            var httpContext = new DefaultHttpContext();
            contextAccessor.HttpContext.Returns(httpContext);

            //Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario();

            //Verificación
            Assert.IsNull(usuario);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarUsuario_CuandoHayClaimEmail()
        {
            //Preparación
            var email = "prueba@hotmail.com";
            var usuarioEsperado = new Usuario { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult(usuarioEsperado));

            //Secuencia de instancias de clases para tener un claim de Email de Identity
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                
                new Claim("email",email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims};
            contextAccessor.HttpContext.Returns(httpContext);

            //Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario();

            //Verificación
            Assert.IsNotNull(usuario);
            Assert.AreEqual(expected: email, actual: usuario.Email);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornaNulo_CuandoUsuarioNoExiste()
        {
            //Preparación
            var email = "prueba@hotmail.com";
            var usuarioEsperado = new Usuario { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult<Usuario>(null!));

            //Secuencia de instancias de clases para tener un claim de Email de Identity
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {

                new Claim("email",email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            contextAccessor.HttpContext.Returns(httpContext);

            //Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario();

            //Verificación
            Assert.IsNull(usuario);
            
        }
    }
}
