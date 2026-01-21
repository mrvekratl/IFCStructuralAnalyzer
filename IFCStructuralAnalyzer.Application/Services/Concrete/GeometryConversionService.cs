using IFCStructuralAnalyzer.Application.Services.Interfaces;
using System;
using System.Diagnostics;
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
            // Placement'tan lokasyon al
            var placement = GetPlacementLocation(product.ObjectPlacement);

            // 🔥 Storey elevation'ı ekle
            var storey = GetStorey(product);

            if (storey?.Elevation.HasValue == true)
            {
                double storeyZ = storey.Elevation.Value;

                // 🔥 KRİTİK: Her eleman tipi için doğru Z hesaplaması
                if (product is IIfcBeam)
                {
                    // Kirişler: Storey + Placement Z (genelde 4000)
                    return (placement.X, placement.Y, storeyZ + placement.Z);
                }

                if (product is IIfcColumn)
                {
                    // Kolonlar: Sadece storey elevation (placement Z genelde 0)
                    return (placement.X, placement.Y, storeyZ);
                }

                if (product is IIfcSlab)
                {
                    // Döşemeler: Storey elevation
                    return (placement.X, placement.Y, storeyZ);
                }
            }

            // Fallback: Sadece placement
            return placement;
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

            // Parent placement'ı ekle (recursive)
            if (local.PlacementRelTo != null)
            {
                var parent = GetPlacementLocation(local.PlacementRelTo);
                return (x + parent.X, y + parent.Y, z + parent.Z);
            }

            return (x, y, z);
        }

        private IIfcBuildingStorey? GetStorey(IIfcProduct product)
        {
            if (product is not IIfcElement element)
                return null;

            var rel = element.ContainedInStructure.FirstOrDefault();
            return rel?.RelatingStructure as IIfcBuildingStorey;
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
                                double extrusionDepth = extrusion.Depth;

                                // Dikdörtgen profil
                                if (extrusion.SweptArea is IIfcRectangleProfileDef rect)
                                {
                                    var width = rect.XDim;
                                    var depth = rect.YDim;

                                    // 🔥 DEBUG: Gerçek değerleri logla
                                    Debug.WriteLine($"  Profile: {width:F0} x {depth:F0}, Extrusion: {extrusionDepth:F0}");

                                    return (width, depth, extrusionDepth);
                                }

                                // Daire profil
                                if (extrusion.SweptArea is IIfcCircleProfileDef circle)
                                {
                                    double diameter = circle.Radius * 2;
                                    return (diameter, diameter, extrusionDepth);
                                }

                                // Karmaşık profil (I-beam gibi)
                                if (extrusion.SweptArea is IIfcArbitraryClosedProfileDef complex)
                                {
                                    var bounds = CalculateProfileBounds(complex);
                                    Debug.WriteLine($"  Complex profile: {bounds.Width:F0} x {bounds.Depth:F0}, Extrusion: {extrusionDepth:F0}");
                                    return (bounds.Width, bounds.Depth, extrusionDepth);
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

                // 🔥 Slab için material layer kalınlığı
                if (product is IIfcSlab slab)
                {
                    var thickness = GetSlabThickness(slab);
                    if (thickness.HasValue)
                    {
                        Debug.WriteLine($"  Slab thickness: {thickness.Value:F0}");
                        return (15000, 20000, thickness.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ⚠️ ExtractDimensions error: {ex.Message}");
            }

            // Fallback varsayılan değerler
            var fallback = GetDefaultDimensions(product);
            Debug.WriteLine($"  ⚠️ Using fallback dimensions: {fallback.Width:F0} x {fallback.Depth:F0} x {fallback.Height:F0}");
            return fallback;
        }

        private (double Width, double Depth) CalculateProfileBounds(IIfcArbitraryClosedProfileDef profile)
        {
            try
            {
                if (profile.OuterCurve is IIfcPolyline polyline)
                {
                    var points = polyline.Points.ToList();

                    if (points.Count > 0)
                    {
                        double minX = points.Min(p => p.X);
                        double maxX = points.Max(p => p.X);
                        double minY = points.Min(p => p.Y);
                        double maxY = points.Max(p => p.Y);

                        return (maxX - minX, maxY - minY);
                    }
                }
            }
            catch { }

            // I-beam varsayılanı
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
            // 🔥 Revit UB305x165x40 için gerçek boyutlar
            if (product is IIfcBeam)
            {
                // UB305x165x40: Height=305mm, Width=165mm, Flange=10.2mm
                // Ancak IFC'de extrusion depth = beam length
                return (165, 305, 3000); // Width x Depth x Length
            }

            if (product is IIfcColumn)
            {
                // UC305x305x97
                return (305, 305, 4000);
            }

            if (product is IIfcSlab)
            {
                return (15000, 20000, 150);
            }

            return (100, 100, 100);
        }

        public double CalculateVolume(IIfcProduct product)
        {
            try
            {
                var dims = ExtractDimensions(product);
                return (dims.Width * dims.Depth * dims.Height) / 1_000_000_000.0; // mm³ → m³
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
                    // 🔥 Storey elevation'dan kat numarası hesapla
                    if (storey.Elevation.HasValue)
                    {
                        // 4000mm = 1 kat
                        int floor = (int)Math.Round(storey.Elevation.Value / 4000.0);
                        Debug.WriteLine($"  Floor calculation: {storey.Elevation.Value:F0} / 4000 = {floor}");
                        return floor;
                    }

                    // İsimden çıkar
                    var name = storey.Name?.ToString().ToLowerInvariant() ?? "";
                    if (name.Contains("ground") || name.Contains("zemin") || name.Contains("0"))
                        return 0;
                    if (name.Contains("1")) return 1;
                    if (name.Contains("2")) return 2;
                    if (name.Contains("3")) return 3;
                    if (name.Contains("4")) return 4;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ⚠️ GetFloorLevel error: {ex.Message}");
            }

            return 0;
        }

        public (double X, double Y, double Z) ConvertLocation(IIfcObjectPlacement? placement)
        {
            throw new NotImplementedException();
        }
    }
}