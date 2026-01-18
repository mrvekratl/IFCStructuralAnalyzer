using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.Interfaces;

namespace IFCStructuralAnalyzer.Application.Services.Interfaces
{
    public interface IGeometryConversionService
    {
        /// <summary>
        /// Convert IFC location to our coordinate system
        /// </summary>
        (double X, double Y, double Z) ConvertLocation(IIfcObjectPlacement? placement);

        /// <summary>
        /// Extract dimensions from IFC element
        /// </summary>
        (double Width, double Depth, double Height) ExtractDimensions(IIfcProduct product);

        /// <summary>
        /// Calculate volume using Xbim geometry engine
        /// </summary>
        double CalculateVolume(IIfcProduct product);

        /// <summary>
        /// Get floor level from storey
        /// </summary>
        int GetFloorLevel(IIfcProduct product);
    }
}
