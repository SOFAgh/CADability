using CADability.Curve2D;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CADability.GeoObject
{
    /// <summary>
    /// A 3d curve which is generated when a point moves along a 2d curve on a certain surface
    /// </summary>
    [Serializable]
    class SurfaceCurve : GeneralCurve, ISerializable
    {
        ICurve2D curve2D;
        ISurface surface;

        public SurfaceCurve(ISurface surface, ICurve2D curve2D)
        {
            this.surface = surface;
            this.curve2D = curve2D;
        }
        public override IGeoObject Clone()
        {
            return new SurfaceCurve(surface, curve2D);
        }

        public override void CopyGeometry(IGeoObject ToCopyFrom)
        {
            if (ToCopyFrom is SurfaceCurve sc)
            {
                sc.curve2D = this.curve2D;
                sc.surface = this.surface;
            }
        }

        public override GeoVector DirectionAt(double pos)
        {
            GeoPoint2D p = curve2D.PointAt(pos);
            GeoVector2D dir = curve2D.DirectionAt(pos);
            surface.DerivationAt(p, out GeoPoint location, out GeoVector diru, out GeoVector dirv);
            return dir.x * diru + dir.y * dirv;
        }

        public override void Modify(ModOp m)
        {
            surface = surface.GetModified(m);
        }

        public override GeoPoint PointAt(double pos)
        {
            return surface.PointAt(curve2D.PointAt(pos));
        }

        public override void Reverse()
        {
            curve2D = curve2D.CloneReverse(true);
        }

        public override ICurve[] Split(double Position)
        {
            ICurve2D[] parts = curve2D.Split(Position);
            ICurve[] res = new ICurve[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                res[i] = new SurfaceCurve(surface, parts[i]);
            }
            return res;
        }

        public override void Trim(double StartPos, double EndPos)
        {
            curve2D = curve2D.Clone();
            curve2D.Trim(StartPos, EndPos);
        }

        protected override double[] GetBasePoints()
        {
            List<double> ips = new List<double>(curve2D.GetInflectionPoints());
            ips.Add(0.0);
            ips.Add(0.25);
            ips.Add(0.5);
            ips.Add(0.75);
            ips.Add(1.0);
            ips.Sort();
            return ips.ToArray();
        }

        protected SurfaceCurve(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
            curve2D = info.GetValue("Curve2D", typeof(ICurve2D)) as ICurve2D;
            surface = info.GetValue("Surface", typeof(ISurface)) as ISurface;
        }
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Curve2D", curve2D);
            info.AddValue("Surface", surface);
        }
    }
}
