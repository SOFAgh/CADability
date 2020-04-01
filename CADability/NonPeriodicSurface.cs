using CADability.Curve2D;
using System;

namespace CADability.GeoObject
{
    internal class NonPeriodicSurface : ISurfaceImpl
    {
        ISurface periodicSurface;
        double vmin, vmax;
        /// <summary>
        /// Non-periodic surface made from a periodic surface. The periodic surface must be peiodic in u and may have a pole at vmin or vmax
        /// </summary>
        /// <param name="periodicSurface"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        public NonPeriodicSurface(ISurface periodicSurface, double vmin, double vmax)
        {
            this.periodicSurface = periodicSurface;
            this.vmax = vmax;
            this.vmin = vmin;
        }
        public override GeoVector UDirection(GeoPoint2D uv)
        {
            throw new NotImplementedException();
        }
        public override GeoVector VDirection(GeoPoint2D uv)
        {
            throw new NotImplementedException();
        }
        public override GeoPoint PointAt(GeoPoint2D uv)
        {
            throw new NotImplementedException();
        }
        public override ICurve FixedU(double u, double vmin, double vmax)
        {
            throw new NotImplementedException();
        }

        public override ICurve FixedV(double u, double umin, double umax)
        {
            throw new NotImplementedException();
        }

        public override ISurface GetModified(ModOp m)
        {
            throw new NotImplementedException();
        }

        private GeoPoint2D toPeriodic(GeoPoint2D uv)
        {
            return new GeoPoint2D(Math.Atan2(uv.y, uv.x), vmin + Math.Sqrt(uv.x * uv.x + uv.y * uv.y));
        }
        private GeoPoint2D fromPeriodic(GeoPoint2D uv)
        {
            double r = uv.y - vmin;
            return new GeoPoint2D(r * Math.Cos(uv.x), r * Math.Sin(uv.x));
        }
        private ICurve2D fromPeriodic(ICurve2D curve2d)
        {
            // gegeben: Kurve im periodischen System, gesucht: Kurve im nichtperiodischen System
            //if (curve2d is Line2D)
            //{
            //    Line2D l2d = curve2d as Line2D;
            //    if (Math.Abs(l2d.StartDirection.y) < 1e-6)
            //    {   // horizontale Linie im periodischen System wird Kreisbogen um den Mittelpunkt im nichtperiodischen System
            //        Arc2D a2d = new Arc2D(GeoPoint2D.Origin, l2d.StartPoint.y - vmin, fromPeriodic(l2d.StartPoint), fromPeriodic(l2d.EndPoint), l2d.EndPoint.x > l2d.StartPoint.x);
            //        return a2d;
            //    }
            //    if (Math.Abs(l2d.StartDirection.x) < 1e-6)
            //    {   // vertikale Linie um periodischen System sollte Linie durch den Ursprung im nichtperiodischen System werden
            //        return new Line2D(fromPeriodic(l2d.StartPoint), fromPeriodic(l2d.EndPoint));
            //    }
            //}
            return null; // new TransformedCurve2D(curve2d, new NonPeriodicTransformation(vmin));
        }

    }
}
