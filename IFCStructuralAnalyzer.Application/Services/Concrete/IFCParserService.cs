using AutoMapper;
using IFCStructuralAnalyzer.Application.DTOs;
using IFCStructuralAnalyzer.Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IFCStructuralAnalyzer.Application.Services.Concrete
{
    public class IFCParserService : IIFCParserService
    {
        private readonly IGeometryConversionService _geometryService;
        private readonly IMapper _mapper;

        public IFCParserService(IGeometryConversionService geometryService, IMapper mapper)
        {
            _geometryService = geometryService;
            _mapper = mapper;
        }

        public async Task<IFCModelDto> ParseIFCFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                if (!ValidateIFCFile(filePath))
                    throw new InvalidOperationException("Invalid IFC file");

                using var model = IfcStore.Open(filePath);

                Debug.WriteLine("\n" + new string('═', 60));
                Debug.WriteLine("📊 IFC FILE INVENTORY");
                Debug.WriteLine(new string('═', 60));

                var totalColumns = model.Instances.OfType<IIfcColumn>().Count();
                var totalBeams = model.Instances.OfType<IIfcBeam>().Count();
                var totalSlabs = model.Instances.OfType<IIfcSlab>().Count();

                Debug.WriteLine($"Total Columns in IFC: {totalColumns}");
                Debug.WriteLine($"Total Beams in IFC:   {totalBeams}");
                Debug.WriteLine($"Total Slabs in IFC:   {totalSlabs}");
                Debug.WriteLine(new string('═', 60) + "\n");

                var ifcModel = new IFCModelDto
                {
                    FileName = Path.GetFileName(filePath),
                    ProjectName = GetProjectName(model),
                    Description = GetProjectDescription(model),
                    ParseDate = DateTime.Now
                };

                // Elementleri çek
                ifcModel.Columns = ExtractColumns(model).ToList();
                ifcModel.Beams = ExtractBeams(model).ToList();
                ifcModel.Slabs = ExtractSlabs(model).ToList();

                Debug.WriteLine("\n" + new string('═', 60));
                Debug.WriteLine("📈 EXTRACTION RESULTS");
                Debug.WriteLine(new string('═', 60));
                Debug.WriteLine($"Extracted Columns: {ifcModel.Columns.Count}/{totalColumns}");
                Debug.WriteLine($"Extracted Beams:   {ifcModel.Beams.Count}/{totalBeams}");
                Debug.WriteLine($"Extracted Slabs:   {ifcModel.Slabs.Count}/{totalSlabs}");

                if (ifcModel.Columns.Count < totalColumns)
                    Debug.WriteLine($"⚠️ WARNING: {totalColumns - ifcModel.Columns.Count} columns FAILED");
                if (ifcModel.Beams.Count < totalBeams)
                    Debug.WriteLine($"⚠️ WARNING: {totalBeams - ifcModel.Beams.Count} beams FAILED");
                if (ifcModel.Slabs.Count < totalSlabs)
                    Debug.WriteLine($"⚠️ WARNING: {totalSlabs - ifcModel.Slabs.Count} slabs FAILED");

                Debug.WriteLine(new string('═', 60) + "\n");

                // İstatistikler
                ifcModel.TotalVolume = ifcModel.Columns.Sum(c => c.Volume) +
                                      ifcModel.Beams.Sum(b => b.Volume) +
                                      ifcModel.Slabs.Sum(s => s.Volume);

                ifcModel.FloorCount = new[] { ifcModel.Columns, ifcModel.Beams, ifcModel.Slabs }
                    .SelectMany(list => list)
                    .Select(e => e.FloorLevel)
                    .Distinct()
                    .Count();

                return ifcModel;
            });
        }

        public bool ValidateIFCFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".ifc";
        }

        public async Task<(string ProjectName, int ElementCount)> GetQuickInfoAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var model = IfcStore.Open(filePath);
                var projectName = GetProjectName(model);
                var elementCount = model.Instances.OfType<IIfcColumn>().Count() +
                                  model.Instances.OfType<IIfcBeam>().Count() +
                                  model.Instances.OfType<IIfcSlab>().Count();

                return (projectName, elementCount);
            });
        }

        #region Private Methods

        private string GetProjectName(IfcStore model)
        {
            var project = model.Instances.OfType<IIfcProject>().FirstOrDefault();
            return project?.Name?.ToString() ?? "Unnamed Project";
        }

        private string? GetProjectDescription(IfcStore model)
        {
            var project = model.Instances.OfType<IIfcProject>().FirstOrDefault();
            return project?.Description?.ToString();
        }

        private IEnumerable<StructuralElementDto> ExtractColumns(IfcStore model)
        {
            var columns = new List<StructuralElementDto>();
            var allColumns = model.Instances.OfType<IIfcColumn>().ToList();
            int successCount = 0;
            int failedCount = 0;

            Debug.WriteLine("\n" + new string('─', 60));
            Debug.WriteLine("🔵 EXTRACTING COLUMNS");
            Debug.WriteLine(new string('─', 60));

            foreach (var ifcColumn in allColumns)
            {
                try
                {
                    var location = _geometryService.GetRealWorldLocation(ifcColumn);
                    var dimensions = _geometryService.ExtractDimensions(ifcColumn);

                    if (successCount < 3)
                    {
                        Debug.WriteLine($"\n[Column {successCount + 1}] {ifcColumn.Name}");
                        Debug.WriteLine($"  Location: ({location.X:F1}, {location.Y:F1}, {location.Z:F1})");
                        Debug.WriteLine($"  Dimensions: {dimensions.Width:F0} x {dimensions.Depth:F0} x {dimensions.Height:F0}");
                    }

                    var column = new StructuralElementDto
                    {
                        Name = ifcColumn.Name?.ToString() ?? "Column",
                        GlobalId = ifcColumn.GlobalId.ToString(),
                        ElementType = "Column",
                        IFCType = "IfcColumn",

                        LocationX = location.X,
                        LocationY = location.Y,
                        LocationZ = location.Z,

                        Width = dimensions.Width,
                        Depth = dimensions.Depth,
                        Height = dimensions.Height,

                        FloorLevel = _geometryService.GetFloorLevel(ifcColumn),
                        Volume = _geometryService.CalculateVolume(ifcColumn),
                        ImportDate = DateTime.Now
                    };

                    columns.Add(column);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    if (failedCount <= 3)
                    {
                        Debug.WriteLine($"❌ FAILED: {ifcColumn.Name}");
                        Debug.WriteLine($"   Error: {ex.Message}");
                        Debug.WriteLine($"   Stack: {ex.StackTrace}");
                    }
                }
            }

            Debug.WriteLine($"\n✓ Success: {successCount}/{allColumns.Count}");
            Debug.WriteLine($"✗ Failed:  {failedCount}/{allColumns.Count}");
            Debug.WriteLine(new string('─', 60));

            return columns;
        }

        private IEnumerable<StructuralElementDto> ExtractBeams(IfcStore model)
        {
            var beams = new List<StructuralElementDto>();
            var allBeams = model.Instances.OfType<IIfcBeam>().ToList();
            int successCount = 0;
            int failedCount = 0;

            Debug.WriteLine("\n" + new string('─', 60));
            Debug.WriteLine("🟠 EXTRACTING BEAMS");
            Debug.WriteLine(new string('─', 60));
            Debug.WriteLine($"Found {allBeams.Count} beams in IFC");

            foreach (var ifcBeam in allBeams)
            {
                try
                {
                    Debug.WriteLine($"\n[Beam {successCount + 1}] Processing: {ifcBeam.Name}");

                    var location = _geometryService.GetRealWorldLocation(ifcBeam);
                    Debug.WriteLine($"  Location OK: ({location.X:F1}, {location.Y:F1}, {location.Z:F1})");

                    var dimensions = _geometryService.ExtractDimensions(ifcBeam);
                    Debug.WriteLine($"  Dimensions OK: {dimensions.Width:F0} x {dimensions.Depth:F0} x {dimensions.Height:F0}");

                    var floorLevel = _geometryService.GetFloorLevel(ifcBeam);
                    Debug.WriteLine($"  Floor OK: {floorLevel}");

                    var beam = new StructuralElementDto
                    {
                        Name = ifcBeam.Name?.ToString() ?? "Beam",
                        GlobalId = ifcBeam.GlobalId.ToString(),
                        ElementType = "Beam",
                        IFCType = "IfcBeam",

                        LocationX = location.X,
                        LocationY = location.Y,
                        LocationZ = location.Z,

                        Width = dimensions.Width,
                        Depth = dimensions.Depth,
                        Length = dimensions.Height,

                        FloorLevel = floorLevel,
                        Volume = _geometryService.CalculateVolume(ifcBeam),
                        ImportDate = DateTime.Now
                    };

                    beams.Add(beam);
                    successCount++;

                    Debug.WriteLine($"  ✓ Beam added successfully");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    Debug.WriteLine($"❌ BEAM FAILED: {ifcBeam.Name}");
                    Debug.WriteLine($"   Error: {ex.Message}");
                    Debug.WriteLine($"   Stack: {ex.StackTrace}");
                }
            }

            Debug.WriteLine($"\n✓ Success: {successCount}/{allBeams.Count}");
            Debug.WriteLine($"✗ Failed:  {failedCount}/{allBeams.Count}");
            Debug.WriteLine(new string('─', 60));

            return beams;
        }

        private IEnumerable<StructuralElementDto> ExtractSlabs(IfcStore model)
        {
            var slabs = new List<StructuralElementDto>();
            var allSlabs = model.Instances.OfType<IIfcSlab>().ToList();
            int successCount = 0;
            int failedCount = 0;

            Debug.WriteLine("\n" + new string('─', 60));
            Debug.WriteLine("🟢 EXTRACTING SLABS");
            Debug.WriteLine(new string('─', 60));
            Debug.WriteLine($"Found {allSlabs.Count} slabs in IFC");

            foreach (var ifcSlab in allSlabs)
            {
                try
                {
                    Debug.WriteLine($"\n[Slab {successCount + 1}] Processing: {ifcSlab.Name}");

                    var location = _geometryService.GetRealWorldLocation(ifcSlab);
                    Debug.WriteLine($"  Location OK: ({location.X:F1}, {location.Y:F1}, {location.Z:F1})");

                    var dimensions = _geometryService.ExtractDimensions(ifcSlab);
                    Debug.WriteLine($"  Dimensions OK: {dimensions.Width:F0} x {dimensions.Depth:F0} x {dimensions.Height:F0}");

                    var floorLevel = _geometryService.GetFloorLevel(ifcSlab);
                    Debug.WriteLine($"  Floor OK: {floorLevel}");

                    var slab = new StructuralElementDto
                    {
                        Name = ifcSlab.Name?.ToString() ?? "Slab",
                        GlobalId = ifcSlab.GlobalId.ToString(),
                        ElementType = "Slab",
                        IFCType = "IfcSlab",

                        LocationX = location.X,
                        LocationY = location.Y,
                        LocationZ = location.Z,

                        Width = dimensions.Width,
                        Depth = dimensions.Depth,
                        Thickness = dimensions.Height,
                        Area = dimensions.Width * dimensions.Depth / 1_000_000.0,

                        FloorLevel = floorLevel,
                        Volume = _geometryService.CalculateVolume(ifcSlab),
                        RotationZ = -90, // 🔄 Döşemeleri 90° döndür
                        ImportDate = DateTime.Now
                    };

                    slabs.Add(slab);
                    successCount++;

                    Debug.WriteLine($"  ✓ Slab added (Rotation: {slab.RotationZ}°)");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    Debug.WriteLine($"❌ SLAB FAILED: {ifcSlab.Name}");
                    Debug.WriteLine($"   Error: {ex.Message}");
                    Debug.WriteLine($"   Stack: {ex.StackTrace}");
                }
            }

            Debug.WriteLine($"\n✓ Success: {successCount}/{allSlabs.Count}");
            Debug.WriteLine($"✗ Failed:  {failedCount}/{allSlabs.Count}");
            Debug.WriteLine(new string('─', 60));

            return slabs;
        }

        #endregion
    }
}