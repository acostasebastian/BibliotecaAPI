using BibliotecaAPI.Datos;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.PRUEBAS;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Utilidades;
using BibliotecaAPI.Utilidades.V1;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;



var builder = WebApplication.CreateBuilder(args);

//Area de servicios


// +++++++++++++ INICIO TIEMPO DE VIDA DE LOS SERVICIOS *****************************************************************

////Teniendo como "clase" una Interfaz, el llamado puede ser más abstracta/flexible (PRINCIPIO DE INVERSIÓN DE DEPENDENCIAS)
////puede ser repositorioValores o repositorioValoresOracle e implementará ObtenerValores
////USO SINGLETON PARA COMPARTIR ESTADO ENTRE LAS PETICIONES, POR EJEMPLO, ACTUALIZO LA BASE DE DATOS Y QUE SE REFRESQUEN LOS DATOS
//builder.Services.AddSingleton<IRepositorioValores,RepositorioValoresOracle>(); //Inyecto una instancia de la clase Repositorio Valores,mediante la Interfaz
//Con solo cambiar la clase RepositorioValores por RepositorioValoresOracle ya se obtienen distintos valores 


//// * TRANSIENT >> Menor tiempo de vida, cada vez que se instancia se regenera
//// * SCOPED >> Tiempo de vida intermedio, se genera solo cuando hay una petición HTTP, se entrega la misma instancia si está dentro del mismo contexto
//// * SINGLETON >> Tiempo de vida LARGO, se entrega la misma petición sin importar cuantas veces se solicite el permiso. Sirve para el cache por ejemplo.

//builder.Services.AddTransient<ServicioTransientPrueba>();
//builder.Services.AddScoped<ServicioScopedPrueba>();
//builder.Services.AddSingleton<ServicioSingletonPrueba>();

// +++++++++++++ FIN TIEMPO DE VIDA DE LOS SERVICIOS *****************************************************************

////Activamos el cache y configuramos cuanto tiempo dura - Para usarlo en el propio cache de la web
builder.Services.AddOutputCache(opciones =>
{
    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);

});

//Activamos el uso del cache en redis 
//builder.Services.AddStackExchangeRedisOutputCache(opciones =>
//{
//    opciones.Configuration = builder.Configuration.GetConnectionString("redis"); //este redis sale del appsettings

//});

builder.Services.AddDataProtection(); //servicio que protege nuestros datos mediante encriptación 

var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(opcionesCORS =>
    {
        opcionesCORS.WithOrigins(origenesPermitidos).AllowAnyMethod().AllowAnyHeader()
        .WithExposedHeaders("cantidad-total-registros");
    });
});

builder.Services.AddAutoMapper(typeof(Program)); //en el curso se usa la version 13 de Automapper y asi funciona.

//Para la 16 que es la ultima en 2026, se deberia usar asi: //builder.Services.AddAutoMapper(cfg => { cfg.AddProfile<AutoMapperProfiles>();}, typeof(Program));

//permite habilitar la funcionalidad de controladores
builder.Services.AddControllers(opciones =>
{
    //opciones.Filters.Add<FiltroTiempoEjecucion>();
    opciones.Conventions.Add(new ConvencionAgrupaPorVersion());
})
    //esta opcion ignoraba los ciclos de autor-libro-autor hasta que creamos un DTO
    //.AddJsonOptions(opciones => opciones.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles) 
    .AddNewtonsoftJson();

//Agrego los servicios a usar, por ejemplo aca que uso el ApplicationDbContext y en sus opciones, que usaré SQL Server para la BD
builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
        opciones.UseSqlServer("name=defaultConnection"));

//Servicios àra el uso de Identity

builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<UserManager<Usuario>>(); //para registrar usuarios
builder.Services.AddScoped<SignInManager<Usuario>>(); // para autenticar usuarios
builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();// creado por nosotros y para poder obtener el usuario logueado de manera segura
//builder.Services.AddTransient<IServicioHash, ServicioHash>();

//para el guardado de fotos local
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();

//para usar el servicio de filtros
//builder.Services.AddScoped<MiFiltroDeAccion>(); 
builder.Services.AddScoped<FiltroValidacionLibro>();

//Configuramos el servicio de la version 1 que hace que no dupliquemos el codigo
builder.Services.AddScoped<BibliotecaAPI.Servicios.V1.IServicioAutores, BibliotecaAPI.Servicios.V1.ServicioAutores>();

builder.Services.AddScoped<BibliotecaAPI.Servicios.V1.IGeneradorEnlaces, BibliotecaAPI.Servicios.V1.GeneradorEnlaces>();

builder.Services.AddScoped<HATEOASAutorAttribute>();
builder.Services.AddScoped<HATEOASAutoresAttribute>();

builder.Services.AddHttpContextAccessor();


builder.Services.AddHttpContextAccessor(); //Para acceder al contexto HTTP desde cualquier clase
builder.Services.AddAuthentication().AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;
    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esadmin", politica => politica.RequireClaim("esadmin"));
});

// para poder usar Swagger
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Biblioteca API",
        Description = "Este es un web API para trabajar con datos de auotres y libros"
    });

    opciones.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v2",
        Title = "Biblioteca API",
        Description = "Este es un web API para trabajar con datos de auotres y libros"
    });

    //Le paso a Swagger que usaré como seguridad un Token JWT y especifico como se lo paso (en Cabecera)
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    opciones.OperationFilter<FiltroAutorizacion>();

    //se indica que el tipo de seguridad arriba configurada, se usará para todos los endpoints
    //Al final, se usa la clase Filtro que esta mas arriba
    //opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        new string[] { }
    //    }
    //});
});

var app = builder.Build();

//area de middlewares

//PRUEBA
//app.Use(async(contexto, next) =>
//{
//    contexto.Response.Headers.Append("mi-cabecera", "valor");
//    await next();
//});

app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
     {
         var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
         var excepcion = exceptionHandlerFeature?.Error!;

         var error = new Error()
         {
             MensajeDeError = excepcion.Message,
             StrackTrace = excepcion.StackTrace,
             Fecha = DateTime.UtcNow
         };

         var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
         dbContext.Add(error);
         await dbContext.SaveChangesAsync();
         await Results.InternalServerError(new
         {
             tipo = "error",
             mensaje = "Ha ocurrido un error inesperado",
             estatus = 500

         }).ExecuteAsync(context);
     }));

//para poder servir el Swagerr.. un documento de Json con las distintas rutas del WebApi
app.UseSwagger();

// es una interfaz para poder visualizar el documento de Swaggger
//se permite elegir entre la version 1 y la 2 (da las opciones del desplegable de arriba a la derecha
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API V1");
    opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API V2");
});

//para poder servir archivos estaticos del wwwroot donde guardar las imagenes
app.UseStaticFiles();

//permite el uso del cache
app.UseOutputCache();


app.UseCors();

//esto habilita que cuando venga una peticion HTTP, e la envia a los controladores para dar respuesta
app.MapControllers();

app.Run();

//para poder usar ConstruirWebApplicationFactory en BasePruebas
public partial class Program { }
