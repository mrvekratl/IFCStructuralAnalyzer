using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Domain.Entities
{
    public class StructuralColumn : StructuralElement
    {
        public override double CalculateVolume()
        {
            // Width * Depth * Height (mm³ to m³)
            return (Width * Depth * Height) / 1_000_000_000.0;
        }

        public override double CalculateWeight()
        {
            if (Material == null) return 0;
            return CalculateVolume() * Material.Density;
        }
    }
}
