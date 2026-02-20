using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Validaciones
{
    //clase de validacion para poder reutilizar en muchos campos y clases
    public class PrimeraLetraMayusculaAttribute : ValidationAttribute  //herreda de esa para que sea una regla de validacion como atributte
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null || string.IsNullOrEmpty(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var primeraLetra = value.ToString()![0].ToString();

            if (primeraLetra != primeraLetra.ToUpper())
            {
                return new ValidationResult("La primera letra debe ser mayúscula");
            }
            return ValidationResult.Success;


        }
    }
}
