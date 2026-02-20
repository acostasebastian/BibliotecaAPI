namespace BibliotecaAPI.DTOs
{
    public class AutorConLibrosDTO : AutorDTO
    {
        public List<LibroDTO> Libros { get; set; } = new List<LibroDTO>();
    }
}
