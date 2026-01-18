using IFCStructuralAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Infrastructure.Data.Configurations
{
    public class MaterialConfiguration : IEntityTypeConfiguration<Material>
    {
        public void Configure(EntityTypeBuilder<Material> builder)
        {
            builder.ToTable("Materials");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.Category)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(m => m.Density)
                .HasPrecision(18, 2);

            builder.Property(m => m.CompressiveStrength)
                .HasPrecision(18, 2);

            // Index
            builder.HasIndex(m => m.Name);
        }
    }
}
