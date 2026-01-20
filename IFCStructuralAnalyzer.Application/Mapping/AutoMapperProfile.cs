using AutoMapper;
using IFCStructuralAnalyzer.Application.DTOs;
using IFCStructuralAnalyzer.Domain.Entities;

namespace IFCStructuralAnalyzer.Application.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Entity -> DTO (okuma için)
            CreateMap<StructuralElement, StructuralElementDto>()
                .ForMember(dest => dest.ElementType, opt => opt.MapFrom(src =>
                    src is StructuralColumn ? "Column" :
                    src is StructuralBeam ? "Beam" :
                    src is StructuralSlab ? "Slab" : "Unknown"))
                .ForMember(dest => dest.MaterialName, opt => opt.MapFrom(src =>
                    src.Material != null ? src.Material.Name : null))
                .ForMember(dest => dest.Volume, opt => opt.MapFrom(src => src.CalculateVolume()))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.CalculateWeight()));

            CreateMap<StructuralColumn, StructuralElementDto>()
                .IncludeBase<StructuralElement, StructuralElementDto>();

            CreateMap<StructuralBeam, StructuralElementDto>()
                .IncludeBase<StructuralElement, StructuralElementDto>();

            CreateMap<StructuralSlab, StructuralElementDto>()
                .IncludeBase<StructuralElement, StructuralElementDto>();

            // DTO -> Entity BASE MAPPING (YENİ)
            CreateMap<StructuralElementDto, StructuralElement>()
                .ForMember(dest => dest.Material, opt => opt.Ignore())
                .ForMember(dest => dest.GlobalId, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .Include<StructuralElementDto, StructuralColumn>()
                .Include<StructuralElementDto, StructuralBeam>()
                .Include<StructuralElementDto, StructuralSlab>();

            // DTO -> Entity SPECIFIC MAPPINGS
            CreateMap<StructuralElementDto, StructuralColumn>()
                .IncludeBase<StructuralElementDto, StructuralElement>();

            CreateMap<StructuralElementDto, StructuralBeam>()
                .IncludeBase<StructuralElementDto, StructuralElement>();

            CreateMap<StructuralElementDto, StructuralSlab>()
                .IncludeBase<StructuralElementDto, StructuralElement>();

            // Material mappings
            CreateMap<Material, MaterialDto>().ReverseMap();
        }
    }
}