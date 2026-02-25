using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Jobs;
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
using System.Threading.RateLimiting;



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

//Rate Limiting >> para limitar la cantidad de peticiones al web API
builder.Services.AddRateLimiter(opciones =>
{
//Politica global, para todo el wb API (todo controlador, todo endpoint, etc)
//opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
//        RateLimitPartition.GetFixedWindowLimiter(
//            //Esto filtra por IP y si no la encuentra, pone desconocido
//            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Desconociodo",
//            //Opciones de RateLimiter, cada cuanto tiempo y cantidad. En este caso, 5 en 10 segundos
//            factory: _ => new FixedWindowRateLimiterOptions
//            {
//                PermitLimit = 5,
//                Window = TimeSpan.FromSeconds(10)
//            }));

    //Politica creada por nosotros, más "genérica" >> llamada Ventana fija
    opciones.AddPolicy("General", context =>
    {
            return RateLimitPartition.GetFixedWindowLimiter(
            //Esto filtra por IP y si no la encuentra, pone desconocido
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Desconociodo",
            //Opciones de RateLimiter, cada cuanto tiempo y cantidad. En este caso, 10 en 10 segundos
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(10)
            });
    });

    //Politica creada por nosotros, mucho más estrica y restrictiva >> llamada Ventana FIJA
    opciones.AddPolicy("Estricta", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
        //Esto filtra por IP y si no la encuentra, pone desconocido
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Desconociodo",
        //Opciones de RateLimiter, cada cuanto tiempo y cantidad. En este caso, 10 en 10 segundos
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(5)
        });
    });


    //Politica creada por nosotros >> llamada Ventana MOVIL
    opciones.AddPolicy("Movil", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(        
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Desconociodo",        
        factory: _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            SegmentsPerWindow = 2,
            QueueLimit = 1, // esto lo pone en cola (1 peticion), para que se procese si hay recursos luego, en lugar de recibir un error
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst // orden para procesarlas. Si de las más reciente a la más antigua o al reves
        });
    });

    //Politica creada por nosotros >> llamada Ventana TOKENBUCKET (cubeta de tokens)
    opciones.AddPolicy("Cubeta", context =>
    {
        return RateLimitPartition.GetTokenBucketLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Desconociodo",
        factory: _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = 2 
        });
    });

    //Politica creada por nosotros >> llamada Ventana CONCURRENCY (por concurrencia)
    opciones.AddPolicy("Concurrencia", context =>
    {
        return RateLimitPartition.GetConcurrencyLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Desconociodo",
        factory: _ => new ConcurrencyLimiterOptions
        {
            PermitLimit = 1
        });
    });

    //voy a permitir peticiones, solo para usuarios autenticados (que tienen un JWT) y no por la IP, porque esa puede cambiar o puede haber más de un usuario en la misma red
    opciones.AddPolicy("prueba-usuario", context =>
    {
        var emailClaim = context.User.Claims.Where(x => x.Type == "email").FirstOrDefault()!;
        var email = emailClaim.Value;

        return RateLimitPartition.GetFixedWindowLimiter(        
        partitionKey: email
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(5)
        });

    });

    //Con esto personalizo el mensaje de error que sale cuando se Limita
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] = retryAfter.TotalSeconds.ToString();
        }
        await context.HttpContext.Response.WriteAsync("Límite excedido. Intente más tarde.",cancellationToken);
    };



});


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

builder.Services.AddHostedService<FacturasBackgroundService>();// para ejecutar el servicio de emitir las facturas en forma programada

builder.Services.AddScoped<IServicioLlaves, ServicioLlaves>();//agregamos el servicio  de llaves para la suscripción

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
        Title = "Biblioteca API - Hola GitHub Actions",
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

//configurar la opcion de que se puedan limitar las peticiones gratuitas por día
builder.Services.AddOptions<LimitarPeticionesDTO>()
    .Bind(builder.Configuration.GetSection(LimitarPeticionesDTO.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();
    
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Esto es vital para ver el error en la consola y en el navegador
}

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

//Para usar el middleware de RateLimiter
app.UseRateLimiter();

//permite el uso del cache
app.UseOutputCache();


app.UseCors();

//para poder usar la limitacion de peticiones por día
app.UseLimitarPeticiones();

//esto habilita que cuando venga una peticion HTTP, e la envia a los controladores para dar respuesta
app.MapControllers();

app.Run();

//para poder usar ConstruirWebApplicationFactory en BasePruebas
public partial class Program { }
