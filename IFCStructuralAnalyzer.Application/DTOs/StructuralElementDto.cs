using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.DTOs
{
    public class StructuralElementDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GlobalId { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty; // "Column", "Beam", "Slab"

        // Location
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
        public double RotationZ { get; set; } = 0;

        // Dimensions (mm)
        public double Width { get; set; }
        public double Depth { get; set; }
        public double Height { get; set; }
        public double? Length { get; set; } // For beams
        public double? Area { get; set; }   // For slabs
        public double? Thickness { get; set; } // For slabs

        // Floor
        public int FloorLevel { get; set; }

        // Material
        public int? MaterialId { get; set; }
        public string? MaterialName { get; set; }

        // Calculated properties
        public double Volume { get; set; }
        public double Weight { get; set; }

        // IFC Metadata
        public string IFCType { get; set; } = string.Empty;
        public DateTime ImportDate { get; set; }
    }
}
