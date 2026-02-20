using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BibliotecaAPITests.Utilidades
{
    //Clase base para probar metodos de un controlador, con una base de datos en memoria y no real
    public class BasePruebas
    {
        protected readonly JsonSerializerOptions jsonSerializerOptions 
            = new JsonSerializerOptions { PropertyNameCaseInsensitive = true};

        protected readonly Claim adminClaim = new Claim("esadmin", "1");

        protected ApplicationDbContext ConstruirContext(string nombreBD)
        {
            var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(nombreBD).Options;

            var dbContext = new ApplicationDbContext(opciones);

            return dbContext;
        }

        protected IMapper ConfigurarAutoMapper()
        {
            var config = new MapperConfiguration(opciones =>
            {
                opciones.AddProfile(new AutoMapperProfiles());
            });


            return config.CreateMapper();
        }

        //Me permite crear el Api en memoria para poder usarlo en las pruebas para hacer pruebas de integración
        // el ignorarSeguridad es para que no pregunte constantemente si es o no Admin
        protected WebApplicationFactory<Program> ConstruirWebApplicationFactory(string nombreBD,
            bool ignorarSeguridad = true)
        {
            var factory = new WebApplicationFactory<Program>();
            factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    //quitamos el proveedor actual de EntiTy Framework (el "real", el de SQL Server),
                    //ya que no pueden estar corriendo 2 a la vez
                    ServiceDescriptor descriptorDBContext = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))!;

                    if (descriptorDBContext is not null)
                    {
                        services.Remove(descriptorDBContext);
                    }

                    //y luego de removerlo, configuramos en memoria al proveedor de pruebas (un db local), una base de pruebas local
                    services.AddDbContext<ApplicationDbContext>(opciones =>
                    opciones.UseInMemoryDatabase(nombreBD));

                    if (ignorarSeguridad)
                    {
                        services.AddSingleton<IAuthorizationHandler, AllowAnonymousHandler>();
                        services.AddControllers(opciones =>
                        {
                            opciones.Filters.Add(new UsuarioFalsoFiltro());
                        });
                    }
                });
            });

            return factory;

        }

        //prueba de crear usuario sin claims y con cualquier Email
        protected async Task<string> CrearUsuario(string nombreBD, WebApplicationFactory<Program> factory)
            => await CrearUsuario(nombreBD, factory, [], "ejemplo@hotmail.com");

        //prueba de crear usuario con claims y con cualquier Email
        protected async Task<string> CrearUsuario(string nombreBD, WebApplicationFactory<Program> factory,
           IEnumerable<Claim> claims )
            => await CrearUsuario(nombreBD, factory, claims, "ejemplo@hotmail.com");

        protected async Task<string> CrearUsuario(string nombreBD, WebApplicationFactory<Program> factory,
            IEnumerable<Claim> claims, string email)
        {
            var urlRegistro = "api/v1/usuarios/registro";
            string token = string.Empty;

            token = await ObtenerToken(email, urlRegistro, factory);

            if (claims.Any())
            {
                var context = ConstruirContext(nombreBD);
                var usuario = await context.Users.Where(x => x.Email == email).FirstAsync();
                Assert.IsNotNull(usuario);

                var userClaims = claims.Select(x => new IdentityUserClaim<string>
                {
                    UserId = usuario.Id,
                    ClaimType = x.Type,
                    ClaimValue = x.Value,
                });

                context.UserClaims.AddRange(userClaims);    
                await context.SaveChangesAsync();

                var urlLogin = "api/v1/usuarios/login";
                token = await ObtenerToken(email,urlLogin, factory);

            }

            return token;
        }

        private async Task<string> ObtenerToken(string email, string url, WebApplicationFactory<Program> factory)
        {
            var password = "aA123456!";
            var credenciales= new CredencialesUsuarioDTO { Email = email, Password = password};
            var cliente = factory.CreateClient();
            var respuesta = await cliente.PostAsJsonAsync(url, credenciales);
            respuesta.EnsureSuccessStatusCode();

            var contenido = await respuesta.Content.ReadAsStringAsync();
            var respuestaAutenticacion = JsonSerializer.Deserialize<RespuestaAutenticacionDTO>(contenido, jsonSerializerOptions);

            Assert.IsNotNull(respuestaAutenticacion.Token);
            return respuestaAutenticacion.Token;
        }
    }
}
