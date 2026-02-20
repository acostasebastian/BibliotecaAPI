using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Entidades
{
    public class Libro
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido.")]
        [StringLength(250, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos.")]
        public required string Titulo { get; set; }
        //public int AutorId { get; set; }

        //public Autor? Autor { get; set; } //Propiedad navegable


        public List<AutorLibro> Autores { get; set; } = []; //las [] son otra forma de hacer el new para evitar tener una referencia nula
        public List<Comentario> Comentarios { get; set; } = []; //las [] son otra forma de hacer el new para evitar tener una referencia nula

    }
}
