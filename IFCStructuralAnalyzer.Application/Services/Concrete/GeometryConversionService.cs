using IFCStructuralAnalyzer.Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.Interfaces;

namespace IFCStructuralAnalyzer.Application.Services.Concrete
{
    public class GeometryConversionService : IGeometryConversionService
    {
        public (double X, double Y, double Z) ConvertLocation(IIfcObjectPlacement? placement)
        {
            if (placement == null)
                return (0, 0, 0);

            try
            {
                var localPlacement = placement as IIfcLocalPlacement;
                if (localPlacement?.RelativePlacement is IIfcAxis2Placement3D axis3D)
                {
                    var location = axis3D.Location;
                    return (
                        location.X,
                        location.Y,
                        location.Z
                    );
                }
            }
            catch
            {
                // If extraction fails, return origin
            }

            return (0, 0, 0);
        }

        public (double Width, double Depth, double Height) ExtractDimensions(IIfcProduct product)
        {
            // Default values
            double width = 300;   // mm
            double depth = 300;   // mm
            double height = 3000; // mm

            try
            {
                // Try to get dimensions from property sets
                var dimensions = ExtractDimensionsFromPropertySets(product);
                if (dimensions.HasValue)
                    return dimensions.Value;

                // Fallback: Try to get from geometry bounding box
                dimensions = ExtractDimensionsFromBoundingBox(product);
                if (dimensions.HasValue)
                    return dimensions.Value;
            }
            catch
            {
                // Use default values
            }

            return (width, depth, height);
        }

        public double CalculateVolume(IIfcProduct product)
        {
            try
            {
                // Try to get volume from quantity sets
                var volume = GetVolumeFromQuantitySets(product);
                if (volume > 0)
                    return volume;

                // Fallback: Calculate from dimensions
                var dimensions = ExtractDimensions(product);
                return (dimensions.Width * dimensions.Depth * dimensions.Height) / 1_000_000_000.0; // mm³ to m³
            }
            catch
            {
                return 0;
            }
        }

        public int GetFloorLevel(IIfcProduct product)
        {
            try
            {
                // Find the building storey
                var spatialElement = product.ContainedInStructure.FirstOrDefault()?.RelatingStructure;

                if (spatialElement is IIfcBuildingStorey storey)
                {
                    // Try to get elevation or name
                    var elevation = storey.Elevation;
                    if (elevation.HasValue)
                    {
                        // Convert elevation to floor number (assuming 3m per floor)
                        return (int)Math.Round(elevation.Value / 3000.0);
                    }

                    // Try to parse from name (e.g., "Level 1", "Floor 2")
                    var name = storey.Name?.ToString() ?? "";
                    var digits = new string(name.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out int floorNumber))
                        return floorNumber;
                }
            }
            catch
            {
                // Default to ground floor
            }

            return 0;
        }

        #region Private Helper Methods

        private (double Width, double Depth, double Height)? ExtractDimensionsFromPropertySets(IIfcProduct product)
        {
            try
            {
                var psets = product.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet)
                    .Select(r => r.RelatingPropertyDefinition as IIfcPropertySet);

                double? width = null;
                double? depth = null;
                double? height = null;

                foreach (var pset in psets)
                {
                    if (pset?.HasProperties == null) continue;

                    foreach (var prop in pset.HasProperties)
                    {
                        if (prop is not IIfcPropertySingleValue singleValue) continue;

                        var propName = prop.Name.ToString()?.ToLower() ?? "";
                        var value = GetNumericValue(singleValue);

                        if (value == null) continue;

                        // Match common property names
                        if (propName.Contains("width") || propName.Contains("w"))
                            width = value;
                        else if (propName.Contains("depth") || propName.Contains("d") || propName.Contains("thickness"))
                            depth = value;
                        else if (propName.Contains("height") || propName.Contains("h") || propName.Contains("length"))
                            height = value;
                    }
                }

                if (width.HasValue && depth.HasValue && height.HasValue)
                    return (width.Value, depth.Value, height.Value);
            }
            catch
            {
                // Continue to next method
            }

            return null;
        }

        private (double Width, double Depth, double Height)? ExtractDimensionsFromBoundingBox(IIfcProduct product)
        {
            try
            {
                // This is a simplified approach - in production, you'd use Xbim.Geometry.Engine
                // For now, return null to use defaults
                return null;
            }
            catch
            {
                return null;
            }
        }

        private double GetVolumeFromQuantitySets(IIfcProduct product)
        {
            try
            {
                var qsets = product.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcElementQuantity)
                    .Select(r => r.RelatingPropertyDefinition as IIfcElementQuantity);

                foreach (var qset in qsets)
                {
                    if (qset?.Quantities == null) continue;

                    foreach (var quantity in qset.Quantities)
                    {
                        if (quantity is IIfcQuantityVolume volumeQty)
                        {
                            return volumeQty.VolumeValue;
                        }
                    }
                }
            }
            catch
            {
                // Return 0 if extraction fails
            }

            return 0;
        }

        private double? GetNumericValue(IIfcPropertySingleValue property)
        {
            try
            {
                var nominalValue = property.NominalValue;
                if (nominalValue == null) return null;

                // Try to convert to double
                if (double.TryParse(nominalValue.ToString(), out double result))
                    return result;

                // Handle different IFC value types
                var valueType = nominalValue.GetType();
                var underlyingValue = valueType.GetProperty("Value")?.GetValue(nominalValue);

                if (underlyingValue != null && double.TryParse(underlyingValue.ToString(), out result))
                    return result;
            }
            catch
            {
                // Return null if conversion fails
            }

            return null;
        }

        #endregion
    }
}
