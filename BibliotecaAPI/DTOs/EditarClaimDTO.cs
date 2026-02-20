using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class EditarClaimDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
