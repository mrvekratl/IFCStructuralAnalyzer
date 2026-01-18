using IFCStructuralAnalyzer.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Infrastructure.Repositories.Interfaces
{
    public interface IMaterialRepository : IGenericRepository<Material>
    {
        Task<Material?> GetByNameAsync(string name);
        Task<IEnumerable<Material>> GetByCategoryAsync(string category);
        Task<Material?> GetDefaultConcreteAsync();
    }
}
