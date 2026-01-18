using IFCStructuralAnalyzer.Infrastructure.Data.Context;
using IFCStructuralAnalyzer.Infrastructure.Repositories.Concrete;
using IFCStructuralAnalyzer.Infrastructure.Repositories.Interfaces;
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
            // DbContext - connection string DbContext içinde
            services.AddDbContext<IFCAnalyzerDbContext>();

            // Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IStructuralElementRepository, StructuralElementRepository>();
            services.AddScoped<IMaterialRepository, MaterialRepository>();

            return services;
        }
    }
}
