using BibliotecaAPI.DTOs;

namespace BibliotecaAPI.PRUEBAS
{
    public interface IServicioHash
    {
        ResultadoHashDTO Hash(string input);
        ResultadoHashDTO Hash(string input, byte[] sal);
    }
}