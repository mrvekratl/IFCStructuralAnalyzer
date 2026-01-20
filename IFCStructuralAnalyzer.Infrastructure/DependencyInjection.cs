using IFCStructuralAnalyzer.Application.Abstractions.Repositories.Interfaces;
using IFCStructuralAnalyzer.Infrastructure.Data.Context;
using IFCStructuralAnalyzer.Infrastructure.Repositories.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // DbContext
            services.AddDbContext<IFCAnalyzerDbContext>();

            // Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IStructuralElementRepository, StructuralElementRepository>();
            services.AddScoped<IMaterialRepository, MaterialRepository>();

            return services;
        }
    }
}
