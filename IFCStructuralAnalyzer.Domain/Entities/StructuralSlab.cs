using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Domain.Entities
{
    public class StructuralSlab : StructuralElement
    {
        public double Area { get; set; } // m²
        public double Thickness { get; set; } // mm

        public override double CalculateVolume()
        {
            // Area * Thickness (mm to m)
            return Area * (Thickness / 1000.0);
        }

        public override double CalculateWeight()
        {
            if (Material == null) return 0;
            return CalculateVolume() * Material.Density;
        }
    }
}
