using IFCStructuralAnalyzer.Application.Services.Interfaces;
using System;
using System.Linq;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc;

namespace IFCStructuralAnalyzer.Application.Services.Concrete
{
    public class GeometryConversionService : IGeometryConversionService
    {
        public void SetGeometryContext(IfcStore model)
        {
            // Basit IFC okuma için geometry engine'e ihtiyaç yok
        }

        public (double X, double Y, double Z) GetRealWorldLocation(IIfcProduct product)
        {
            return GetPlacementLocation(product.ObjectPlacement);
        }

        private (double X, double Y, double Z) GetPlacementLocation(IIfcObjectPlacement placement)
        {
            if (placement is not IIfcLocalPlacement local)
                return (0, 0, 0);

            double x = 0, y = 0, z = 0;

            if (local.RelativePlacement is IIfcAxis2Placement3D axis)
            {
                var p = axis.Location;
                x = p.X;
                y = p.Y;
                z = p.Z;
            }

            // Parent placement'ı ekle
            if (local.PlacementRelTo != null)
            {
                var parent = GetPlacementLocation(local.PlacementRelTo);
                return (x + parent.X, y + parent.Y, z + parent.Z);
            }

            return (x, y, z);
        }

        public (double Width, double Depth, double Height) ExtractDimensions(IIfcProduct product)
        {
            try
            {
                var rep = product.Representation;
                if (rep != null)
                {
                    foreach (var r in rep.Representations)
                    {
                        foreach (var item in r.Items)
                        {
                            // Extruded solid - en yaygın tip
                            if (item is IIfcExtrudedAreaSolid extrusion)
                            {
                                // Dikdörtgen profil
                                if (extrusion.SweptArea is IIfcRectangleProfileDef rect)
                                {
                                    return (rect.XDim, rect.YDim, extrusion.Depth);
                                }

                                // Daire profil
                                if (extrusion.SweptArea is IIfcCircleProfileDef circle)
                                {
                                    double diameter = circle.Radius * 2;
                                    return (diameter, diameter, extrusion.Depth);
                                }

                                // Karmaşık profil için bounding box
                                if (extrusion.SweptArea is IIfcArbitraryClosedProfileDef complex)
                                {
                                    var bounds = CalculateProfileBounds(complex);
                                    return (bounds.Width, bounds.Depth, extrusion.Depth);
                                }
                            }

                            // Bounding box
                            if (item is IIfcBoundingBox bb)
                            {
                                return (bb.XDim, bb.YDim, bb.ZDim);
                            }
                        }
                    }
                }

                // Slab için material layer kalınlığı
                if (product is IIfcSlab slab)
                {
                    var thickness = GetSlabThickness(slab);
                    if (thickness.HasValue)
                    {
                        return (10000, 10000, thickness.Value);
                    }
                }
            }
            catch { }

            // Fallback varsayılan değerler
            return GetDefaultDimensions(product);
        }

        private (double Width, double Depth) CalculateProfileBounds(IIfcArbitraryClosedProfileDef profile)
        {
            try
            {
                if (profile.OuterCurve is IIfcPolyline polyline)
                {
                    var points = polyline.Points.ToList();

                    double minX = points.Min(p => p.X);
                    double maxX = points.Max(p => p.X);
                    double minY = points.Min(p => p.Y);
                    double maxY = points.Max(p => p.Y);

                    return (maxX - minX, maxY - minY);
                }
            }
            catch { }

            return (300, 600);
        }

        private double? GetSlabThickness(IIfcSlab slab)
        {
            try
            {
                var relAssociates = slab.HasAssociations
                    .OfType<IIfcRelAssociatesMaterial>()
                    .FirstOrDefault();

                if (relAssociates?.RelatingMaterial is IIfcMaterialLayerSetUsage layerSetUsage)
                {
                    return layerSetUsage.ForLayerSet.MaterialLayers
                        .Sum(layer => layer.LayerThickness);
                }
            }
            catch { }

            return null;
        }

        private (double Width, double Depth, double Height) GetDefaultDimensions(IIfcProduct product)
        {
            if (product is IIfcColumn)
                return (300, 300, 3000);

            if (product is IIfcBeam)
                return (300, 600, 5000);

            if (product is IIfcSlab)
                return (10000, 10000, 200);

            return (100, 100, 100);
        }

        public double CalculateVolume(IIfcProduct product)
        {
            try
            {
                var dims = ExtractDimensions(product);
                return (dims.Width * dims.Depth * dims.Height) / 1_000_000_000.0;
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
                if (product is not IIfcElement element)
                    return 0;

                // Storey'yi bul
                var rel = element.ContainedInStructure.FirstOrDefault();
                if (rel?.RelatingStructure is IIfcBuildingStorey storey)
                {
                    // Storey elevation'dan kat numarası hesapla
                    if (storey.Elevation.HasValue)
                    {
                        return (int)Math.Round(storey.Elevation.Value / 3000.0);
                    }

                    // İsimden çıkar
                    var name = storey.Name?.ToString().ToLowerInvariant() ?? "";
                    if (name.Contains("ground") || name.Contains("zemin") || name.Contains("0"))
                        return 0;
                    if (name.Contains("1")) return 1;
                    if (name.Contains("2")) return 2;
                    if (name.Contains("3")) return 3;
                }
            }
            catch { }

            return 0;
        }
    }
}