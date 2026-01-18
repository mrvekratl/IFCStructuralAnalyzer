using IFCStructuralAnalyzer.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.Services.Interfaces
{
    public interface IMaterialService
    {
        Task<IEnumerable<MaterialDto>> GetAllAsync();
        Task<MaterialDto?> GetByIdAsync(int id);
        Task<MaterialDto?> GetByNameAsync(string name);
        Task<MaterialDto?> GetDefaultConcreteAsync();
    }
}
