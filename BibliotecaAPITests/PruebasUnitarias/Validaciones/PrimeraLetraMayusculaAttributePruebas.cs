using BibliotecaAPI.Validaciones;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BibliotecaAPITests.PruebasUnitarias.Validaciones
{
    [TestClass]
    public class PrimeraLetraMayusculaAttributePruebas //Nombre de la clase a probar + la palabra Pruebas
    {
        //Convencion para crear los nombres: Nombre del metodo a probar _ Lo que hace y espero que ocurra _ condiciones para que ocurra
        [TestMethod]
        //los DataRow son para poner los posibles valores a usar en los parámetros en las pruebas, que en este caso pasaran como string value
        [DataRow("")]  
        [DataRow("    ")]
        [DataRow(null)]
        [DataRow("Sebastián")]
        public void IsValid_RetornaExitoso_SiValueNoTieneLaPrimeraLetraMinuscula(string value)
        {

            //Preparación
            var primeraLetraMayusculaAttribute = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());
            
            
            //Prueba
            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext);

            //Verificación
            //(el Assert es una clase que me permite hacer verificaciones, y si estas no son satisfactorias dan error y por lo tanto no fue aprobada )
            Assert.AreEqual(expected:ValidationResult.Success, actual: resultado);
            
        }

       
        [TestMethod]       
        [DataRow("sebastián")]
        public void IsValid_RetornaError_SiValueTieneLaPrimeraLetraMayuscula(string value)
        {

            //Preparación
            var primeraLetraMayusculaAttribute = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());


            //Prueba
            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext);

            //Verificación
            //(el Assert es una clase que me permite hacer verificaciones, y si estas no son satisfactorias dan error y por lo tanto no fue aprobada )
            Assert.AreEqual(expected: "La primera letra debe ser mayúscula", actual: resultado!.ErrorMessage);

        }



    }
}
