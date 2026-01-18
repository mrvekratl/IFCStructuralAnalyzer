using AutoMapper;
using IFCStructuralAnalyzer.Application.DTOs;
using IFCStructuralAnalyzer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // StructuralElement mappings
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

            // Reverse mappings
            CreateMap<StructuralElementDto, StructuralColumn>()
                .ForMember(dest => dest.Material, opt => opt.Ignore());

            CreateMap<StructuralElementDto, StructuralBeam>()
                .ForMember(dest => dest.Material, opt => opt.Ignore());

            CreateMap<StructuralElementDto, StructuralSlab>()
                .ForMember(dest => dest.Material, opt => opt.Ignore());

            // Material mappings
            CreateMap<Material, MaterialDto>().ReverseMap();
        }
    }
}
