using CADability.Curve2D;
using System;

namespace CADability.GeoObject
{
    internal class NonPeriodicSurface : ISurfaceImpl
    {
        ISurface periodicSurface;
        bool isPeriodicInU;
        double a, b; // offset and factor for mapping a + b*par => [0, 1] when there is a pole or [0.5, 1.5] without pole
        bool hasPole;
        /// <summary>
        /// Non-periodic surface made from a underlying periodic surface. The underlying periodic surface must be periodic in u or in v and may have a pole at the minimum or maximum of the
        /// non periodic parameter. The underlying periodic surface may also be non periodic but have one singularity, which will be removed
        /// </summary>
        /// <param name="periodicSurface"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        public NonPeriodicSurface(ISurface periodicSurface, BoundingRect bounds)
        {
            this.periodicSurface = periodicSurface;
            hasPole = false;
            if (periodicSurface.IsUPeriodic && periodicSurface.IsVPeriodic)
            {
                isPeriodicInU = bounds.Width - periodicSurface.UPeriod > bounds.Height - periodicSurface.VPeriod;
                throw new NotImplementedException("NonPeriodicSurface: both u and v are periodic");
            }
            else if (periodicSurface.IsUPeriodic)
            {
                isPeriodicInU = true;
                double[] sv = periodicSurface.GetVSingularities();
                if (sv != null && sv.Length > 0)
                {
                    if (sv.Length == 1)
                    {
                        hasPole = true;
                        if (sv[0] == bounds.Bottom)
                        {
                            // bounds.Bottom => 0.0, bounds.Top => 1.0
                            b = 1.0 / bounds.Height;
                            a = -bounds.Bottom * b;
                        }
                        else if (sv[0] == bounds.Top)
                        {
                            b = -1.0 / bounds.Height;
                            a = bounds.Top * b;
                        }
                        else throw new ApplicationException("pole must be on border");
                    }
                    else throw new ApplicationException("more than one pole");
                }
                else
                {
                    // bounds.Bottom => 0.5, bounds.Top => 1.5
                    b = 1.0 / bounds.Height;
                    a = 0.5 - bounds.Bottom / bounds.Height;
                }
            }
            else if (periodicSurface.IsVPeriodic)
            {
                isPeriodicInU = false;
                double[] su = periodicSurface.GetUSingularities();
                if (su != null && su.Length > 0)
                {
                    if (su.Length == 1)
                    {
                        hasPole = true;
                        if (su[0] == bounds.Left)
                        {
                            b = 1.0 / bounds.Width;
                            a = -bounds.Left * b;
                        }
                        else if (su[0] == bounds.Right)
                        {
                            b = -1.0 / bounds.Width;
                            a = bounds.Right * b;
                        }
                        else throw new ApplicationException("pole must be on border");
                    }
                    else throw new ApplicationException("more than one pole");
                }
                else
                {
                    // bounds.Bottom => 0.5, bounds.Top => 1.5
                    b = 1.0 / bounds.Width;
                    a = 0.5 - bounds.Left / bounds.Width;
                }
            }
            else
            {   // not periodic, only pole removal
                throw new NotImplementedException("NonPeriodicSurface: implement only pole removal");
            }
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
            if (isPeriodicInU) return new GeoPoint2D(Math.Atan2(uv.y, uv.x), (Math.Sqrt(uv.x * uv.x + uv.y * uv.y) - a) / b);
            else return new GeoPoint2D((Math.Sqrt(uv.x * uv.x + uv.y * uv.y) - a) / b, Math.Atan2(uv.y, uv.x));
        }
        private GeoPoint2D fromPeriodic(GeoPoint2D uv)
        {

            if (isPeriodicInU)
            {
                double r = a + b * uv.y;
                return new GeoPoint2D(r * Math.Cos(uv.x), r * Math.Sin(uv.x));
            }
            else
            {
                double r = a + b * uv.x;
                return new GeoPoint2D(r * Math.Cos(uv.y), r * Math.Sin(uv.y));
            }
        }
    }
}
