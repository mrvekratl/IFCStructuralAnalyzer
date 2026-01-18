using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.DTOs
{
    public class IFCModelDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ParseDate { get; set; } = DateTime.Now;

        // Element collections
        public List<StructuralElementDto> Columns { get; set; } = new();
        public List<StructuralElementDto> Beams { get; set; } = new();
        public List<StructuralElementDto> Slabs { get; set; } = new();

        // Statistics
        public int TotalElementCount => Columns.Count + Beams.Count + Slabs.Count;
        public double TotalVolume { get; set; }
        public int FloorCount { get; set; }
    }
}
