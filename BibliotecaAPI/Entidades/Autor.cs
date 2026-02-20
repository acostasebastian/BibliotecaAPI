using BibliotecaAPI.Validaciones;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Entidades
{
    public class Autor
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido.")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos.")]
        [PrimeraLetraMayuscula]
        public required string Nombres { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido.")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos.")]
        [PrimeraLetraMayuscula]
        public required string Apellidos { get; set; }

        [StringLength(20, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos.")]
        public string? Identificacion { get; set; }

        [Unicode(false)]
        public string? Foto { get; set; }


        public List<AutorLibro> Libros { get; set; } = new List<AutorLibro>(); //el New se usa para evitar tener una referencia nula
        //public List<Libro> Libros { get; set; } = new List<Libro>(); //el New se usa para evitar tener una referencia nula
    }
}
