using HelixToolkit.Wpf;
using IFCStructuralAnalyzer.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace IFCStructuralAnalyzer.Presentation.Services
{
    public class Rendering3DService
    {
        private const double MM_TO_METERS = 1.0 / 1000.0;
        private const double MIN_DIMENSION = 0.1; // meters

        // NORMALIZE EDILMIŞ KOORDİNATLAR İÇİN OFFSET
        private Point3D _modelCenter = new Point3D(0, 0, 0);

        public Model3DGroup CreateSceneModel(IEnumerable<StructuralElementDto> elements)
        {
            var modelGroup = new Model3DGroup();

            if (elements == null || !elements.Any())
                return modelGroup;

            var elementList = elements.ToList();

            // ÖNEMLİ: Model merkezini hesapla
            _modelCenter = CalculateModelCenter(elementList);

            foreach (var element in elementList)
            {
                try
                {
                    var model = CreateElementModel(element);
                    if (model != null)
                        modelGroup.Children.Add(model);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating element {element.Name}: {ex.Message}");
                }
            }

            return modelGroup;
        }

        private GeometryModel3D? CreateElementModel(StructuralElementDto element)
        {
            var meshBuilder = new MeshBuilder(false, false);

            // Koordinatları metre cinsine çevir ve NORMALIZE ET
            double x = (element.LocationX * MM_TO_METERS) - _modelCenter.X;
            double y = (element.LocationY * MM_TO_METERS) - _modelCenter.Y;
            double z = (element.LocationZ * MM_TO_METERS) - _modelCenter.Z;

            double width = Math.Max(element.Width * MM_TO_METERS, MIN_DIMENSION);
            double depth = Math.Max(element.Depth * MM_TO_METERS, MIN_DIMENSION);
            double height = Math.Max(element.Height * MM_TO_METERS, MIN_DIMENSION);

            // Eğer normalize edilmiş pozisyon çok uzaktaysa skip et
            double distance = Math.Sqrt(x * x + y * y + z * z);
            if (distance > 1000) // 1km'den uzaksa
            {
                Console.WriteLine($"⚠️  {element.ElementType} '{element.Name}' too far ({distance:F0}m) - SKIPPED");
                return null;
            }

            Point3D centerPoint;

            switch (element.ElementType)
            {
                case "Column":
                    centerPoint = new Point3D(x, y, z + height / 2);
                    meshBuilder.AddBox(centerPoint, width, depth, height);
                    break;

                case "Beam":
                    double length = element.Length.HasValue
                        ? Math.Max(element.Length.Value * MM_TO_METERS, MIN_DIMENSION)
                        : height;

                    centerPoint = new Point3D(x + length / 2, y, z + depth / 2);
                    meshBuilder.AddBox(centerPoint, length, width, depth);
                    break;

                case "Slab":
                    double thickness = element.Thickness.HasValue
                        ? Math.Max(element.Thickness.Value * MM_TO_METERS, 0.05)
                        : 0.2;

                    double slabWidth = Math.Max(element.Width * MM_TO_METERS, 1.0);
                    double slabDepth = Math.Max(element.Depth * MM_TO_METERS, 1.0);

                    if (element.Area.HasValue && element.Area.Value > 0)
                    {
                        double area = element.Area.Value;
                        double aspectRatio = slabWidth / slabDepth;
                        slabDepth = Math.Sqrt(area / aspectRatio);
                        slabWidth = area / slabDepth;
                    }

                    centerPoint = new Point3D(x + slabWidth / 2, y + slabDepth / 2, z + thickness / 2);
                    meshBuilder.AddBox(centerPoint, slabWidth, slabDepth, thickness);
                    break;

                default:
                    return null;
            }

            var mesh = meshBuilder.ToMesh();
            var material = GetMaterialForElementType(element.ElementType);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }

        private Material GetMaterialForElementType(string elementType)
        {
            Color color = elementType switch
            {
                "Column" => Color.FromArgb(200, 144, 202, 249), // Mavi
                "Beam" => Color.FromArgb(200, 255, 183, 77),    // Turuncu
                "Slab" => Color.FromArgb(180, 129, 199, 132),   // Yeşil
                _ => Color.FromArgb(200, 128, 128, 128)
            };

            return new DiffuseMaterial(new SolidColorBrush(color));
        }



        // YENİ METOD: Model merkezini hesapla
        private Point3D CalculateModelCenter(List<StructuralElementDto> elements)
        {
            if (!elements.Any())
                return new Point3D(0, 0, 0);

            double sumX = 0, sumY = 0, sumZ = 0;

            foreach (var element in elements)
            {
                sumX += element.LocationX * MM_TO_METERS;
                sumY += element.LocationY * MM_TO_METERS;
                sumZ += element.LocationZ * MM_TO_METERS;
            }

            int count = elements.Count;
            return new Point3D(sumX / count, sumY / count, sumZ / count);
        }

        public void UpdateCamera(HelixViewport3D viewport, IEnumerable<StructuralElementDto> elements)
        {
            if (viewport == null || elements == null || !elements.Any())
                return;

            try
            {
                var elementList = elements.ToList();
                var bounds = CalculateNormalizedBoundingBox(elementList);

                double centerX = (bounds.MinX + bounds.MaxX) / 2;
                double centerY = (bounds.MinY + bounds.MaxY) / 2;
                double centerZ = (bounds.MinZ + bounds.MaxZ) / 2;

                double rangeX = bounds.MaxX - bounds.MinX;
                double rangeY = bounds.MaxY - bounds.MinY;
                double rangeZ = bounds.MaxZ - bounds.MinZ;
                double maxRange = Math.Max(Math.Max(rangeX, rangeY), rangeZ);

                if (maxRange < 1) maxRange = 10;

                // Mesafeyi artır - daha uzaktan bak
                double distance = maxRange * 3.0; // 2.5'ten 3.0'a çıkardık

                if (viewport.Camera is PerspectiveCamera camera)
                {
                    // İzometrik açıdan bak
                    var cameraPosition = new Point3D(
                        centerX + distance * 0.7,
                        centerY - distance * 0.7,
                        centerZ + distance * 0.7  // Z'yi de artırdık
                    );

                    camera.Position = cameraPosition;
                    camera.LookDirection = new Vector3D(
                        centerX - cameraPosition.X,
                        centerY - cameraPosition.Y,
                        centerZ - cameraPosition.Z
                    );
                    camera.UpDirection = new Vector3D(0, 0, 1); // Z yukarı
                    camera.FieldOfView = 50; // 45'ten 50'ye
                }

                Console.WriteLine($"Camera: Center=({centerX:F2},{centerY:F2},{centerZ:F2}), Range={maxRange:F2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Camera update error: {ex}");
            }
        }

        // NORMALIZE EDİLMİŞ Bounding Box hesapla
        private BoundingBox CalculateNormalizedBoundingBox(List<StructuralElementDto> elements)
        {
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            double minZ = double.MaxValue, maxZ = double.MinValue;

            foreach (var element in elements)
            {
                // Normalize edilmiş koordinatlar
                double x = (element.LocationX * MM_TO_METERS) - _modelCenter.X;
                double y = (element.LocationY * MM_TO_METERS) - _modelCenter.Y;
                double z = (element.LocationZ * MM_TO_METERS) - _modelCenter.Z;

                double width = Math.Max(element.Width * MM_TO_METERS, MIN_DIMENSION);
                double depth = Math.Max(element.Depth * MM_TO_METERS, MIN_DIMENSION);
                double height = Math.Max(element.Height * MM_TO_METERS, MIN_DIMENSION);

                switch (element.ElementType)
                {
                    case "Column":
                        minX = Math.Min(minX, x - width / 2);
                        maxX = Math.Max(maxX, x + width / 2);
                        minY = Math.Min(minY, y - depth / 2);
                        maxY = Math.Max(maxY, y + depth / 2);
                        minZ = Math.Min(minZ, z);
                        maxZ = Math.Max(maxZ, z + height);
                        break;

                    case "Beam":
                        double length = (element.Length ?? height) * MM_TO_METERS;
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x + length);
                        minY = Math.Min(minY, y - width / 2);
                        maxY = Math.Max(maxY, y + width / 2);
                        minZ = Math.Min(minZ, z);
                        maxZ = Math.Max(maxZ, z + depth);
                        break;

                    case "Slab":
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x + width);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y + depth);
                        double thickness = (element.Thickness ?? 200) * MM_TO_METERS;
                        minZ = Math.Min(minZ, z);
                        maxZ = Math.Max(maxZ, z + thickness);
                        break;
                }
            }

            return new BoundingBox
            {
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY,
                MinZ = minZ,
                MaxZ = maxZ
            };
        }

        private class BoundingBox
        {
            public double MinX { get; set; }
            public double MaxX { get; set; }
            public double MinY { get; set; }
            public double MaxY { get; set; }
            public double MinZ { get; set; }
            public double MaxZ { get; set; }
        }
    }
}