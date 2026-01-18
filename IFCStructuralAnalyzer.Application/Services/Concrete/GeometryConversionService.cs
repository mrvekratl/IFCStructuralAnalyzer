using IFCStructuralAnalyzer.Application.Services.Interfaces;
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

                // Fallback: use defaults
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
                // IFC4 API için spatial structure bulma
                // Yöntem 1: Model üzerinden IIfcRelContainedInSpatialStructure arama
                var model = product.Model;
                if (model != null)
                {
                    var spatialRels = model.Instances
                        .OfType<IIfcRelContainedInSpatialStructure>()
                        .Where(r => r.RelatedElements != null && r.RelatedElements.Contains(product));

                    foreach (var rel in spatialRels)
                    {
                        if (rel.RelatingStructure is IIfcBuildingStorey storey)
                        {
                            return GetFloorNumberFromStorey(storey);
                        }
                    }
                }

                // Yöntem 2: Decomposes ilişkisinden
                var decomposes = product.Decomposes?.FirstOrDefault();
                if (decomposes?.RelatingObject is IIfcBuildingStorey decompStorey)
                {
                    return GetFloorNumberFromStorey(decompStorey);
                }

                // Yöntem 3: Location'dan tahmin et (fallback)
                var location = ConvertLocation(product.ObjectPlacement);
                if (location.Z > 0)
                {
                    // Z koordinatından kat tahmini (3000mm = 1 kat)
                    return (int)Math.Round(location.Z / 3000.0);
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error getting floor level: {ex.Message}");
            }

            return 0; // Default to ground floor
        }

        #region Private Helper Methods

        private int GetFloorNumberFromStorey(IIfcBuildingStorey storey)
        {
            try
            {
                // Elevation'dan kat numarası
                var elevation = storey.Elevation;
                if (elevation.HasValue)
                {
                    // 3000mm (3m) per floor assumption
                    return (int)Math.Round(elevation.Value / 3000.0);
                }

                // İsimden parse et (e.g., "Level 1", "Floor 2", "Kat 3")
                var name = storey.Name?.ToString() ?? "";
                var digits = new string(name.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int floorNumber))
                    return floorNumber;
            }
            catch
            {
                // Return 0 if parsing fails
            }

            return 0;
        }

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
                        if (propName.Contains("width") || propName == "w")
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