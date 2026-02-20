namespace BibliotecaAPI.DTOs
{
    public class ColeccionDeRecursosDTO<T> : RecursoDTO where T : RecursoDTO // para poder usarlo desde HATEOAS
    {
        public IEnumerable<T> Valores { get; set; } = [];
    }
}
