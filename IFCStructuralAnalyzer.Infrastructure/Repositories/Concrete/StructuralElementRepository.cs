using IFCStructuralAnalyzer.Application.Abstractions.Repositories.Interfaces;
using IFCStructuralAnalyzer.Domain.Entities;
using IFCStructuralAnalyzer.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Infrastructure.Repositories.Concrete
{
    public class StructuralElementRepository : GenericRepository<StructuralElement>, IStructuralElementRepository
    {
        public StructuralElementRepository(IFCAnalyzerDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<StructuralElement>> GetByFloorLevelAsync(int floorLevel)
        {
            return await _dbSet
                .Include(e => e.Material)
                .Where(e => e.FloorLevel == floorLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<StructuralElement>> GetByTypeAsync(string elementType)
        {
            return await _dbSet
                .Include(e => e.Material)
                .Where(e => e.IFCType == elementType)
                .ToListAsync();
        }

        public async Task<IEnumerable<StructuralElement>> GetByGlobalIdAsync(string globalId)
        {
            return await _dbSet
                .Include(e => e.Material)
                .Where(e => e.GlobalId == globalId)
                .ToListAsync();
        }

        public async Task<IEnumerable<StructuralElement>> GetWithMaterialAsync()
        {
            return await _dbSet
                .Include(e => e.Material)
                .ToListAsync();
        }

        public async Task DeleteAllAsync()
        {
            var allElements = await _dbSet.ToListAsync();
            _dbSet.RemoveRange(allElements);
            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<string, int>> GetElementCountByTypeAsync()
        {
            return await _dbSet
                .GroupBy(e => e.IFCType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }

        public async Task<Dictionary<int, int>> GetElementCountByFloorAsync()
        {
            return await _dbSet
                .GroupBy(e => e.FloorLevel)
                .Select(g => new { Floor = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Floor, x => x.Count);
        }

        public async Task<double> GetTotalVolumeAsync()
        {
            var elements = await _dbSet.ToListAsync();
            return elements.Sum(e => e.CalculateVolume());
        }

        public async Task<double> GetTotalVolumeByFloorAsync(int floorLevel)
        {
            var elements = await _dbSet
                .Where(e => e.FloorLevel == floorLevel)
                .ToListAsync();

            return elements.Sum(e => e.CalculateVolume());
        }
    }
}
