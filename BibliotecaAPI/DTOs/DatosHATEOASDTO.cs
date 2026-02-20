namespace BibliotecaAPI.DTOs
{
    //Es record porque los datos luego de crearse nunca cambiaran.
    //Los campos como string Enlace por ejemplo, son para crearlo como un record ordinal
    public record class DatosHATEOASDTO(string Enlace, string Descripcion, string Metodo);
    
}
