using IFCStructuralAnalyzer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.Abstractions.Repositories.Interfaces
{
    public interface IStructuralElementRepository : IGenericRepository<StructuralElement>
    {
        // Custom queries for IFC operations
        Task<IEnumerable<StructuralElement>> GetByFloorLevelAsync(int floorLevel);
        Task<IEnumerable<StructuralElement>> GetByTypeAsync(string elementType);
        Task<IEnumerable<StructuralElement>> GetByGlobalIdAsync(string globalId);
        Task<IEnumerable<StructuralElement>> GetWithMaterialAsync();
        Task DeleteAllAsync();

        // Statistics
        Task<Dictionary<string, int>> GetElementCountByTypeAsync();
        Task<Dictionary<int, int>> GetElementCountByFloorAsync();
        Task<double> GetTotalVolumeAsync();
        Task<double> GetTotalVolumeByFloorAsync(int floorLevel);
    }
}
