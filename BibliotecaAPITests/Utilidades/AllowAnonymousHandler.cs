using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaAPITests.Utilidades
{
    public class AllowAnonymousHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            //damos por buenos todos los requerimientos de seguridad
            foreach (var requeriment in context.PendingRequirements)
            {
                context.Succeed(requeriment);
            }

            return Task.CompletedTask;
        }
    }
}
