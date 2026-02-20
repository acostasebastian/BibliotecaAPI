using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroValidacionLibro : IAsyncActionFilter
    {
        private readonly ApplicationDbContext dbContext;

        public FiltroValidacionLibro(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ActionArguments.TryGetValue("libroCreacionDTO", out var value)
                || value is not LibroCreacionDTO libroCreacionDTO)
            {
                context.ModelState.AddModelError(string.Empty, "El modelo enviado no es valido");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            if (libroCreacionDTO.AutoresId is null || libroCreacionDTO.AutoresId.Count == 0)
            {
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresId),
                    "No se puede crear un libro sin autores");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            var autoresIdsExisten = await dbContext.Autores
                                    .Where(x => libroCreacionDTO.AutoresId.Contains(x.Id))
                                    .Select(x => x.Id)
                                    .ToListAsync();

            if (autoresIdsExisten.Count != libroCreacionDTO.AutoresId.Count)
            {
                //Para detectar los registros que tienen un autor que no existe, hago el except
                var autoresNoExisten = libroCreacionDTO.AutoresId.Except(autoresIdsExisten);
                var autoresNoExistenString = string.Join(", ", autoresNoExisten);//1, 2,3
                var mensajeDeError = $"Los siguientes autores no existen: {autoresNoExistenString}";

                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresId), mensajeDeError);
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            await next();
        }
    }
}
