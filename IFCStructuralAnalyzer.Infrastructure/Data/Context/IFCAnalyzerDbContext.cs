using IFCStructuralAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Infrastructure.Data.Context
{
    public class IFCAnalyzerDbContext : DbContext
    {
        // Constructor - parametreli (DI için tutuyoruz)
        public IFCAnalyzerDbContext(DbContextOptions<IFCAnalyzerDbContext> options)
            : base(options)
        {
        }

        // Parametresiz constructor - Migration için gerekli
        public IFCAnalyzerDbContext()
        {
        }

        // DbSets
        public DbSet<StructuralElement> StructuralElements { get; set; }
        public DbSet<StructuralColumn> StructuralColumns { get; set; }
        public DbSet<StructuralBeam> StructuralBeams { get; set; }
        public DbSet<StructuralSlab> StructuralSlabs { get; set; }
        public DbSet<Material> Materials { get; set; }

        // Connection String direkt burada
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=.;Database=IFCStructuralAnalyzerDb;Integrated Security=true;TrustServerCertificate=true;"
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPH (Table Per Hierarchy) Strategy
            modelBuilder.Entity<StructuralElement>()
                .HasDiscriminator<string>("ElementType")
                .HasValue<StructuralColumn>("Column")
                .HasValue<StructuralBeam>("Beam")
                .HasValue<StructuralSlab>("Slab");

            // Apply configurations
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(IFCAnalyzerDbContext).Assembly
            );

            // Seed initial materials
            SeedMaterials(modelBuilder);
        }

        private void SeedMaterials(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Material>().HasData(
                new Material
                {
                    Id = 1,
                    Name = "C30/37 Concrete",
                    Category = "Concrete",
                    Density = 2500, // kg/m³
                    CompressiveStrength = 30 // MPa
                },
                new Material
                {
                    Id = 2,
                    Name = "C35/45 Concrete",
                    Category = "Concrete",
                    Density = 2500,
                    CompressiveStrength = 35
                },
                new Material
                {
                    Id = 3,
                    Name = "S420 Steel",
                    Category = "Steel",
                    Density = 7850,
                    CompressiveStrength = 420
                },
                new Material
                {
                    Id = 4,
                    Name = "S500 Steel",
                    Category = "Steel",
                    Density = 7850,
                    CompressiveStrength = 500
                }
            );
        }
    }
}
