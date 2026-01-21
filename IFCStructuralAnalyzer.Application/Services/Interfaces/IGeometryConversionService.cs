using System;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc;

namespace IFCStructuralAnalyzer.Application.Services.Interfaces
{
    public interface IGeometryConversionService
    {
               /// <summary>
        /// Set geometry context for the IFC model
        /// </summary>
        void SetGeometryContext(IfcStore model);

        /// <summary>
        /// Get real world location using Geometry Engine
        /// </summary>
        (double X, double Y, double Z) GetRealWorldLocation(IIfcProduct product);

       
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