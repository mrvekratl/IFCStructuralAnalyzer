using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Domain.Entities
{
    public class Material
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Concrete"; // Concrete, Steel, Wood
        public double Density { get; set; } // kg/m³
        public double CompressiveStrength { get; set; } // MPa

        // Navigation property
        public virtual ICollection<StructuralElement> StructuralElements { get; set; }
            = new List<StructuralElement>();
    }
}
