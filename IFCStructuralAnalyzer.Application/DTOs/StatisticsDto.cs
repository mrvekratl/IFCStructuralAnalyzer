using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.DTOs
{
    public class StatisticsDto
    {
        // Element counts
        public int TotalElements { get; set; }
        public int ColumnCount { get; set; }
        public int BeamCount { get; set; }
        public int SlabCount { get; set; }

        // Volume statistics
        public double TotalVolume { get; set; }
        public double ColumnVolume { get; set; }
        public double BeamVolume { get; set; }
        public double SlabVolume { get; set; }

        // Weight statistics
        public double TotalWeight { get; set; }

        // Floor statistics
        public Dictionary<int, int> ElementCountByFloor { get; set; } = new();
        public Dictionary<int, double> VolumeByFloor { get; set; } = new();

        // Material statistics
        public Dictionary<string, int> ElementCountByMaterial { get; set; } = new();
    }
}
