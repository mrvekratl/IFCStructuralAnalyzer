using IFCStructuralAnalyzer.Domain.Entities;
using IFCStructuralAnalyzer.Infrastructure.Data.Context;
using IFCStructuralAnalyzer.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Infrastructure.Repositories.Concrete
{
    public class MaterialRepository : GenericRepository<Material>, IMaterialRepository
    {
        public MaterialRepository(IFCAnalyzerDbContext context) : base(context)
        {
        }

        public async Task<Material?> GetByNameAsync(string name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(m => m.Name == name);
        }

        public async Task<IEnumerable<Material>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(m => m.Category == category)
                .ToListAsync();
        }

        public async Task<Material?> GetDefaultConcreteAsync()
        {
            return await _dbSet
                .FirstOrDefaultAsync(m => m.Name == "C30/37 Concrete");
        }
    }
}
