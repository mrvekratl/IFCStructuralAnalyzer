using AutoMapper;
using IFCStructuralAnalyzer.Application.Mapping;
using IFCStructuralAnalyzer.Application.Services.Concrete;
using IFCStructuralAnalyzer.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // AutoMapper - Assembly'den otomatik profile bulma
            services.AddAutoMapper(typeof(DependencyInjection).Assembly); 

            // Services
            services.AddScoped<IIFCParserService, IFCParserService>();
            services.AddScoped<IGeometryConversionService, GeometryConversionService>();
            services.AddScoped<IStructuralElementService, StructuralElementService>();
            services.AddScoped<IMaterialService, MaterialService>();

            return services;
        }
    }
}
