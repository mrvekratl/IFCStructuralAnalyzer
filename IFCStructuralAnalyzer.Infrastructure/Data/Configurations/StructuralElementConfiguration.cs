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
    public class StructuralElementConfiguration : IEntityTypeConfiguration<StructuralElement>
    {
        public void Configure(EntityTypeBuilder<StructuralElement> builder)
        {
            builder.ToTable("StructuralElements");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.GlobalId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.IFCType)
                .HasMaxLength(100);

            // Indexes for performance
            builder.HasIndex(e => e.GlobalId)
                .IsUnique();

            builder.HasIndex(e => e.FloorLevel);

            builder.HasIndex(e => e.IFCType);

            // Relationship with Material
            builder.HasOne(e => e.Material)
                .WithMany(m => m.StructuralElements)
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.SetNull);

            // Precision for location coordinates
            builder.Property(e => e.LocationX).HasPrecision(18, 6);
            builder.Property(e => e.LocationY).HasPrecision(18, 6);
            builder.Property(e => e.LocationZ).HasPrecision(18, 6);

            // Precision for dimensions
            builder.Property(e => e.Width).HasPrecision(18, 2);
            builder.Property(e => e.Depth).HasPrecision(18, 2);
            builder.Property(e => e.Height).HasPrecision(18, 2);
        }
    }
}
