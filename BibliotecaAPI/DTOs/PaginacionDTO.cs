using System.Runtime.Intrinsics.X86;

namespace BibliotecaAPI.DTOs
{
    //un record en vez de class hace que permanezca inmutable
    public record PaginacionDTO ( int Pagina = 1, int RecordsPorPagina = 10)
    {
        private const int cantidadMaximaRecordsPorPagina = 50;

        //este uno hace que ese sea el valor por defecto
        public int Pagina { get; init; } = Math.Max(1, Pagina); // el init hace que no se pueda cambiar luego de ser inicializado. El Math que no pueda poner -1

        //el Clamp hace que el numero si o si este entre 1 y 50.. si pones -1 muestra 1 si pone 51 pone 50
        public int RecordsPorPagina { get; init; } = Math.Clamp(RecordsPorPagina,1, cantidadMaximaRecordsPorPagina);
       
    }
}
