using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace BibliotecaAPI.PRUEBAS
{
    public class ServicioHash : IServicioHash
    {
        public ResultadoHashDTO Hash(string input)
        {
            var sal = new byte[16];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(sal);
            }

            return Hash(input, sal);
        }

        public ResultadoHashDTO Hash(string input, byte[] sal)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: input,
                salt: sal, prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10_000,
                numBytesRequested: 256 / 8
                ));


            return new ResultadoHashDTO()
            {
                Hash = hashed,
                Sal = sal
            };
        }
    }
}
