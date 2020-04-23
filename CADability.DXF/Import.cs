using System;
using CADability.GeoObject;
using netDxf;

namespace CADability.DXF
{
    public class Import
    {
        public static GeoObjectList Load(string fileName)
        {
            DxfDocument doc = DxfDocument.Load(fileName);
            foreach (netDxf.Entities.Line line in doc.Lines)
            {
                System.Diagnostics.Trace.WriteLine("line: " + line.EndPoint.X.ToString());
            }
            return null;
        }
    }
}
