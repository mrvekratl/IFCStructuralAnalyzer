using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Domain.ValueObjects
{
    public class Dimension
    {
        public double Width { get; private set; }
        public double Depth { get; private set; }
        public double Height { get; private set; }

        // Constructor
        public Dimension(double width, double depth, double height)
        {
            if (width <= 0 || depth <= 0 || height <= 0)
                throw new ArgumentException("Dimensions must be positive values");

            Width = width;
            Depth = depth;
            Height = height;
        }

        // Parametresiz constructor (EF Core için)
        private Dimension()
        {
        }

        // Volume calculation
        public double CalculateVolume()
        {
            return (Width * Depth * Height) / 1_000_000_000.0; // mm³ to m³
        }

        // Cross-sectional area
        public double CalculateCrossSectionArea()
        {
            return (Width * Depth) / 1_000_000.0; // mm² to m²
        }

        // Equality (Value Objects are compared by value, not reference)
        public override bool Equals(object? obj)
        {
            if (obj is not Dimension other)
                return false;

            return Width == other.Width &&
                   Depth == other.Depth &&
                   Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Depth, Height);
        }

        public override string ToString()
        {
            return $"{Width}×{Depth}×{Height} mm";
        }
    }
}
