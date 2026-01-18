using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Domain.Entities
{
    public abstract class StructuralElement
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GlobalId { get; set; } = string.Empty;

        // Location
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }

        // Dimensions (mm)
        public double Width { get; set; }
        public double Depth { get; set; }
        public double Height { get; set; }

        // Floor
        public int FloorLevel { get; set; }

        // Material
        public int? MaterialId { get; set; }
        public virtual Material? Material { get; set; }

        // Calculations
        public abstract double CalculateVolume();
        public abstract double CalculateWeight();

        // IFC Metadata
        public string IFCType { get; set; } = string.Empty;
        public DateTime ImportDate { get; set; } = DateTime.Now;
    }
}
