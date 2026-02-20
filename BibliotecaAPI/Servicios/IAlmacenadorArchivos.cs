namespace BibliotecaAPI.Servicios
{
    public interface IAlmacenadorArchivos
    {
        //Permite que cuando se borre un autor, se borre tambien su foto
        Task Borrar(string? ruta, string contenedor);

        Task<string> Almacenar(string contenedor, IFormFile archivo);

        async Task<string> Editar(string? ruta, string contenedor, IFormFile archivo)
        {
            await Borrar(ruta, contenedor);
            return await Almacenar(contenedor,archivo);
        }
    }
}
