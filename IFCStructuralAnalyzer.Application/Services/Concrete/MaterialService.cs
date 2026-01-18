using AutoMapper;
using IFCStructuralAnalyzer.Application.DTOs;
using IFCStructuralAnalyzer.Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.Services.Concrete
{
    public class MaterialService : IMaterialService
    {
        private readonly IMaterialRepository _repository;
        private readonly IMapper _mapper;

        public MaterialService(IMaterialRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MaterialDto>> GetAllAsync()
        {
            var materials = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<MaterialDto>>(materials);
        }

        public async Task<MaterialDto?> GetByIdAsync(int id)
        {
            var material = await _repository.GetByIdAsync(id);
            return material != null ? _mapper.Map<MaterialDto>(material) : null;
        }

        public async Task<MaterialDto?> GetByNameAsync(string name)
        {
            var material = await _repository.GetByNameAsync(name);
            return material != null ? _mapper.Map<MaterialDto>(material) : null;
        }

        public async Task<MaterialDto?> GetDefaultConcreteAsync()
        {
            var material = await _repository.GetDefaultConcreteAsync();
            return material != null ? _mapper.Map<MaterialDto>(material) : null;
        }
    }
}
