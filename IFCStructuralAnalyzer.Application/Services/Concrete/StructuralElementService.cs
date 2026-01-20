using AutoMapper;
using IFCStructuralAnalyzer.Application.Abstractions.Repositories.Interfaces;
using IFCStructuralAnalyzer.Application.DTOs;
using IFCStructuralAnalyzer.Application.Services.Interfaces;
using IFCStructuralAnalyzer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.Services.Concrete
{
    public class StructuralElementService : IStructuralElementService
    {
        private readonly IStructuralElementRepository _repository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IMapper _mapper;

        public StructuralElementService(
            IStructuralElementRepository repository,
            IMaterialRepository materialRepository,
            IMapper mapper)
        {
            _repository = repository;
            _materialRepository = materialRepository;
            _mapper = mapper;
        }

        public async Task<StructuralElementDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity != null ? _mapper.Map<StructuralElementDto>(entity) : null;
        }

        public async Task<IEnumerable<StructuralElementDto>> GetAllAsync()
        {
            var entities = await _repository.GetWithMaterialAsync();
            return _mapper.Map<IEnumerable<StructuralElementDto>>(entities);
        }

        public async Task<IEnumerable<StructuralElementDto>> GetByFloorAsync(int floorLevel)
        {
            var entities = await _repository.GetByFloorLevelAsync(floorLevel);
            return _mapper.Map<IEnumerable<StructuralElementDto>>(entities);
        }

        public async Task<IEnumerable<StructuralElementDto>> GetByTypeAsync(string elementType)
        {
            var entities = await _repository.GetByTypeAsync(elementType);
            return _mapper.Map<IEnumerable<StructuralElementDto>>(entities);
        }

        public async Task ImportElementsAsync(IEnumerable<StructuralElementDto> elements)
        {
            try
            {
                var defaultMaterial = await _materialRepository.GetDefaultConcreteAsync();
                var entities = new List<StructuralElement>();
                var processedGlobalIds = new HashSet<string>();
                int duplicateCount = 0;

                foreach (var dto in elements)
                {
                    // GlobalId benzersizliğini kontrol et
                    string uniqueGlobalId = dto.GlobalId;

                    if (processedGlobalIds.Contains(dto.GlobalId))
                    {
                        uniqueGlobalId = Guid.NewGuid().ToString();
                        duplicateCount++;
                        Console.WriteLine($"⚠️  Duplicate GlobalId for '{dto.Name}'. New ID: {uniqueGlobalId}");
                    }

                    processedGlobalIds.Add(uniqueGlobalId);

                    StructuralElement entity;

                    // AutoMapper ile map et (GlobalId ignore edilecek)
                    switch (dto.ElementType.ToLower())
                    {
                        case "column":
                            entity = _mapper.Map<StructuralColumn>(dto);
                            break;
                        case "beam":
                            entity = _mapper.Map<StructuralBeam>(dto);
                            if (entity is StructuralBeam beam && dto.Length.HasValue)
                                beam.Length = dto.Length.Value;
                            break;
                        case "slab":
                            entity = _mapper.Map<StructuralSlab>(dto);
                            if (entity is StructuralSlab slab)
                            {
                                slab.Area = dto.Area ?? 0;
                                slab.Thickness = dto.Thickness ?? 0;
                            }
                            break;
                        default:
                            Console.WriteLine($"⚠️  Unknown element type: {dto.ElementType}");
                            continue;
                    }

                    // Manuel olarak GlobalId ata (AutoMapper ignore etti)
                    entity.GlobalId = uniqueGlobalId;

                    // Default material
                    if (entity.MaterialId == null && defaultMaterial != null)
                    {
                        entity.MaterialId = defaultMaterial.Id;
                    }

                    entities.Add(entity);
                }

                if (entities.Any())
                {
                    await _repository.AddRangeAsync(entities);
                    await _repository.SaveChangesAsync();

                    Console.WriteLine($"✅ Successfully imported {entities.Count} elements");
                    if (duplicateCount > 0)
                    {
                        Console.WriteLine($"🔧 Fixed {duplicateCount} duplicate GlobalId(s)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ INNER: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task DeleteAllAsync()
        {
            await _repository.DeleteAllAsync();
        }

        public async Task<StatisticsDto> GetStatisticsAsync()
        {
            var elements = await _repository.GetWithMaterialAsync();
            var elementList = elements.ToList();

            var stats = new StatisticsDto
            {
                TotalElements = elementList.Count,
                ColumnCount = elementList.Count(e => e is StructuralColumn),
                BeamCount = elementList.Count(e => e is StructuralBeam),
                SlabCount = elementList.Count(e => e is StructuralSlab),

                TotalVolume = elementList.Sum(e => e.CalculateVolume()),
                ColumnVolume = elementList.OfType<StructuralColumn>().Sum(c => c.CalculateVolume()),
                BeamVolume = elementList.OfType<StructuralBeam>().Sum(b => b.CalculateVolume()),
                SlabVolume = elementList.OfType<StructuralSlab>().Sum(s => s.CalculateVolume()),

                TotalWeight = elementList.Sum(e => e.CalculateWeight()),

                ElementCountByFloor = await _repository.GetElementCountByFloorAsync(),
                ElementCountByMaterial = elementList
                    .Where(e => e.Material != null)
                    .GroupBy(e => e.Material!.Name)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Calculate volume by floor
            var volumeByFloor = new Dictionary<int, double>();
            foreach (var floor in stats.ElementCountByFloor.Keys)
            {
                var floorElements = elementList.Where(e => e.FloorLevel == floor);
                volumeByFloor[floor] = floorElements.Sum(e => e.CalculateVolume());
            }
            stats.VolumeByFloor = volumeByFloor;

            return stats;
        }

        public async Task<StatisticsDto> GetStatisticsByFloorAsync(int floorLevel)
        {
            var elements = (await _repository.GetByFloorLevelAsync(floorLevel)).ToList();

            var stats = new StatisticsDto
            {
                TotalElements = elements.Count,
                ColumnCount = elements.Count(e => e is StructuralColumn),
                BeamCount = elements.Count(e => e is StructuralBeam),
                SlabCount = elements.Count(e => e is StructuralSlab),

                TotalVolume = elements.Sum(e => e.CalculateVolume()),
                ColumnVolume = elements.OfType<StructuralColumn>().Sum(c => c.CalculateVolume()),
                BeamVolume = elements.OfType<StructuralBeam>().Sum(b => b.CalculateVolume()),
                SlabVolume = elements.OfType<StructuralSlab>().Sum(s => s.CalculateVolume()),

                TotalWeight = elements.Sum(e => e.CalculateWeight())
            };

            return stats;
        }
    }
}
