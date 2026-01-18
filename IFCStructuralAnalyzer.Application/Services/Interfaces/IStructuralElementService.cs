using IFCStructuralAnalyzer.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.Services.Interfaces
{
    public interface IStructuralElementService
    {
        // CRUD operations
        Task<StructuralElementDto?> GetByIdAsync(int id);
        Task<IEnumerable<StructuralElementDto>> GetAllAsync();
        Task<IEnumerable<StructuralElementDto>> GetByFloorAsync(int floorLevel);
        Task<IEnumerable<StructuralElementDto>> GetByTypeAsync(string elementType);

        // Bulk operations
        Task ImportElementsAsync(IEnumerable<StructuralElementDto> elements);
        Task DeleteAllAsync();

        // Statistics
        Task<StatisticsDto> GetStatisticsAsync();
        Task<StatisticsDto> GetStatisticsByFloorAsync(int floorLevel);
    }
}
