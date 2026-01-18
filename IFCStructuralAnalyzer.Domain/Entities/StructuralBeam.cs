using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Domain.Entities
{
    public class StructuralBeam : StructuralElement
    {
        public double Length { get; set; }

        public override double CalculateVolume()
        {
            // Width * Depth * Length (mm³ to m³)
            return (Width * Depth * Length) / 1_000_000_000.0;
        }

        public override double CalculateWeight()
        {
            if (Material == null) return 0;
            return CalculateVolume() * Material.Density;
        }
    }
}
