using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class LibroCreacionDTO
    {
        [Required(ErrorMessage = "El campo {0} es requerido.")]
        [StringLength(250, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos.")]
        public required string Titulo { get; set; }
        public List<int> AutoresId { get; set; } = [];
    }
}
