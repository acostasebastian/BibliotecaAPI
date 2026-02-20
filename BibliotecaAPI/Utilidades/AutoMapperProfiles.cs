using AutoMapper;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;

namespace BibliotecaAPI.Utilidades
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Autor, AutorDTO>()
                .ForMember(dto => dto.NombreCompleto,
                config => config.MapFrom(autor => MapearNombreYApellidoAutor(autor)));//Para el metodo Get de AutoresController

            CreateMap<Autor, AutorConLibrosDTO>()
                .ForMember(dto => dto.NombreCompleto,
                config => config.MapFrom(autor => MapearNombreYApellidoAutor(autor))); //Para el metodo Get con Id de AutoresController


            CreateMap<AutorCreacionDTO, Autor>(); //Para el metodo Post de AutoresController
            CreateMap<AutorCreacionDTOConFoto, Autor>()
                .ForMember(ent => ent.Foto, config => config.Ignore());//mapea de AutorCreacionDTOConFoto a Autor, pero ignorando la propiedad Foto

            //Para el metodo Patch de AutoresController >> pasandole un autor mapeo a AutorPatchDTO (variable autorPatchDTO)
            //y el reverse es para pasando autorPatchDTO crear un autorDB ( mapper.Map(autorPatchDTO, autorDB) );
            CreateMap<Autor, AutorPatchDTO>().ReverseMap();


            CreateMap<AutorLibro, LibroDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.LibroId))
                .ForMember(dto => dto.Titulo, config => config.MapFrom(ent => ent.Libro!.Titulo));

            CreateMap<Libro, LibroDTO>();
            CreateMap<LibroCreacionDTO, Libro>()
                .ForMember(ent => ent.Autores, config => 
                config.MapFrom(dto => dto.AutoresId.Select(id => new AutorLibro { AutorId = id})));


            CreateMap<Libro, LibroConAutoresDTO>();

            CreateMap<AutorLibro, AutorDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.AutorId))
                .ForMember(dto => dto.NombreCompleto,
                config => config.MapFrom(ent => MapearNombreYApellidoAutor(ent.Autor!)));

            CreateMap<LibroCreacionDTO, AutorLibro>()
                .ForMember(ent => ent.Libro,
                config => config.MapFrom(dto => new Libro { Titulo = dto.Titulo }));

           

            CreateMap<ComentarioCreacionDTO, Comentario>();
            CreateMap<Comentario,ComentarioDTO>()
                .ForMember(dto => dto.UsuarioEmail, config => config.MapFrom(ent => ent.Usuario!.Email));
            CreateMap<ComentarioPatchDTO, Comentario>().ReverseMap();

            CreateMap<Usuario, UsuarioDTO>();

        }
        private string MapearNombreYApellidoAutor(Autor autor) => $"{autor.Nombres} {autor.Apellidos}";
    }
}
