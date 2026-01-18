using AutoMapper;
using IFCStructuralAnalyzer.Application.DTOs;
using IFCStructuralAnalyzer.Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

                var ifcModel = new IFCModelDto
                {
                    FileName = Path.GetFileName(filePath),
                    ProjectName = GetProjectName(model),
                    Description = GetProjectDescription(model),
                    ParseDate = DateTime.Now
                };

                // Extract structural elements
                ifcModel.Columns = ExtractColumns(model).ToList();
                ifcModel.Beams = ExtractBeams(model).ToList();
                ifcModel.Slabs = ExtractSlabs(model).ToList();

                // Calculate statistics
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

        #region Private Helper Methods

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

            foreach (var ifcColumn in model.Instances.OfType<IIfcColumn>())
            {
                try
                {
                    var location = _geometryService.ConvertLocation(ifcColumn.ObjectPlacement);
                    var dimensions = _geometryService.ExtractDimensions(ifcColumn);
                    var floorLevel = _geometryService.GetFloorLevel(ifcColumn);

                    var column = new StructuralElementDto
                    {
                        Name = ifcColumn.Name?.ToString() ?? "Unnamed Column",
                        GlobalId = ifcColumn.GlobalId.ToString() ?? Guid.NewGuid().ToString(),
                        ElementType = "Column",
                        IFCType = "IfcColumn",

                        LocationX = location.X,
                        LocationY = location.Y,
                        LocationZ = location.Z,

                        Width = dimensions.Width,
                        Depth = dimensions.Depth,
                        Height = dimensions.Height,

                        FloorLevel = floorLevel,

                        Volume = _geometryService.CalculateVolume(ifcColumn),
                        ImportDate = DateTime.Now
                    };

                    columns.Add(column);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other elements
                    Console.WriteLine($"Error processing column {ifcColumn.GlobalId}: {ex.Message}");
                }
            }

            return columns;
        }

        private IEnumerable<StructuralElementDto> ExtractBeams(IfcStore model)
        {
            var beams = new List<StructuralElementDto>();

            foreach (var ifcBeam in model.Instances.OfType<IIfcBeam>())
            {
                try
                {
                    var location = _geometryService.ConvertLocation(ifcBeam.ObjectPlacement);
                    var dimensions = _geometryService.ExtractDimensions(ifcBeam);
                    var floorLevel = _geometryService.GetFloorLevel(ifcBeam);

                    var beam = new StructuralElementDto
                    {
                        Name = ifcBeam.Name?.ToString() ?? "Unnamed Beam",
                        GlobalId = ifcBeam.GlobalId.ToString() ?? Guid.NewGuid().ToString(),
                        ElementType = "Beam",
                        IFCType = "IfcBeam",

                        LocationX = location.X,
                        LocationY = location.Y,
                        LocationZ = location.Z,

                        Width = dimensions.Width,
                        Depth = dimensions.Depth,
                        Length = dimensions.Height, // For beams, height is actually length

                        FloorLevel = floorLevel,

                        Volume = _geometryService.CalculateVolume(ifcBeam),
                        ImportDate = DateTime.Now
                    };

                    beams.Add(beam);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing beam {ifcBeam.GlobalId}: {ex.Message}");
                }
            }

            return beams;
        }

        private IEnumerable<StructuralElementDto> ExtractSlabs(IfcStore model)
        {
            var slabs = new List<StructuralElementDto>();

            foreach (var ifcSlab in model.Instances.OfType<IIfcSlab>())
            {
                try
                {
                    var location = _geometryService.ConvertLocation(ifcSlab.ObjectPlacement);
                    var dimensions = _geometryService.ExtractDimensions(ifcSlab);
                    var floorLevel = _geometryService.GetFloorLevel(ifcSlab);

                    var slab = new StructuralElementDto
                    {
                        Name = ifcSlab.Name?.ToString() ?? "Unnamed Slab",
                        GlobalId = ifcSlab.GlobalId.ToString() ?? Guid.NewGuid().ToString(),
                        ElementType = "Slab",
                        IFCType = "IfcSlab",

                        LocationX = location.X,
                        LocationY = location.Y,
                        LocationZ = location.Z,

                        Thickness = dimensions.Height,
                        Area = dimensions.Width * dimensions.Depth / 1_000_000.0, // mm² to m²

                        FloorLevel = floorLevel,

                        Volume = _geometryService.CalculateVolume(ifcSlab),
                        ImportDate = DateTime.Now
                    };

                    slabs.Add(slab);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing slab {ifcSlab.GlobalId}: {ex.Message}");
                }
            }

            return slabs;
        }

        #endregion
    }
}
