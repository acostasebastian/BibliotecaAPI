using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BibliotecaAPI.Swagger
{
    public class ConvencionAgrupaPorVersion : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            // Ejemplo: "Controlllers.V1"
            var namespaceControlador = controller.ControllerType.Namespace; // me devuelve Controlllers.V1
            var versionAPI = namespaceControlador.Split('.').Last().ToLower(); //v1
            controller.ApiExplorer.GroupName = versionAPI; //agrupa los controladores por version
        }
    }
}
