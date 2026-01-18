using IFCStructuralAnalyzer.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCStructuralAnalyzer.Application.Services.Interfaces
{
    public interface IIFCParserService
    {
        /// <summary>
        /// Parse an IFC file and extract structural elements
        /// </summary>
        Task<IFCModelDto> ParseIFCFileAsync(string filePath);

        /// <summary>
        /// Validate if file is a valid IFC file
        /// </summary>
        bool ValidateIFCFile(string filePath);

        /// <summary>
        /// Get quick file information without full parsing
        /// </summary>
        Task<(string ProjectName, int ElementCount)> GetQuickInfoAsync(string filePath);
    }
}
