namespace BibliotecaAPI.Utilidades
{
    //Indica donde se puede usar este atributo >> metodos (acciones) y clases (Controladores).
    //El multiple = false es para no poder hacerlo 2 veces en el mismo lugar
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class DeshabilitarLimitarPeticionesAttribute : Attribute
    {
    }
}
