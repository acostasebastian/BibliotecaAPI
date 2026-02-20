using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Runtime.CompilerServices;

namespace BibliotecaAPI.Utilidades
{
    public static class ModelStateDictionaryExtensions
    {
        public static BadRequestObjectResult ConstruirProblemDetail(this ModelStateDictionary modelState)
        {
            var problemDetails = new ValidationProblemDetails(modelState)
            {
                Title = "One or more validation errors ocurred", //esto es lo que viene por defecto, podemos poner lo que quieramos
                Status = StatusCodes.Status400BadRequest

            };

            return new BadRequestObjectResult(problemDetails);
        }
    }
}
