using CADability.Curve2D;
using System;
using System.Runtime.Serialization;

namespace CADability.GeoObject
{
    /// <summary>
    /// Non-periodic surface made from an underlying periodic surface. The underlying periodic surface must be periodic in u or in v and may have a pole 
    /// at the minimum or maximum of the non periodic parameter. The underlying periodic surface may also be non periodic but have one singularity, which will be removed.
    /// </summary>
    [Serializable]
    public class NonPeriodicSurface : ISurfaceImpl, ISerializable, IDeserializationCallback
    {
        ISurface periodicSurface;
        bool isPeriodicInU;
        double a, b; // offset and factor for mapping a + b*par => [0, 1] when there is a pole or [0.5, 1.5] without pole
        bool hasPole;
        bool fullPeriod;
        BoundingRect periodicBounds;
        ModOp2D toNonPeriodicBounds, toPeriodicBounds;
        /// <summary>
        /// </summary>
        /// <param name="periodicSurface"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        public NonPeriodicSurface(ISurface periodicSurface, BoundingRect bounds)
        {
            this.periodicSurface = periodicSurface;
            this.periodicBounds = bounds;
            Init();
        }
        private void Init()
        {
            hasPole = false;
            if (periodicSurface.IsUPeriodic && periodicSurface.IsVPeriodic)
            {
                isPeriodicInU = periodicBounds.Width - periodicSurface.UPeriod > periodicBounds.Height - periodicSurface.VPeriod;
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
                        if (sv[0] == periodicBounds.Bottom)
                        {
                            // bounds.Bottom => 0.0, bounds.Top => 1.0
                            b = 1.0 / periodicBounds.Height;
                            a = -periodicBounds.Bottom * b;
                        }
                        else if (sv[0] == periodicBounds.Top)
                        {
                            b = -1.0 / periodicBounds.Height;
                            a = periodicBounds.Top * b;
                        }
                        else throw new ApplicationException("pole must be on border");
                    }
                    else throw new ApplicationException("more than one pole");
                }
                else
                {
                    // bounds.Bottom => 0, bounds.Top => 1
                    // bounds.Left => 0, bountd.Right => 2*pi
                    toNonPeriodicBounds = ModOp2D.Scale(2.0 * Math.PI / periodicBounds.Width, 1.0 / periodicBounds.Height);
                    GeoVector2D move = toNonPeriodicBounds * new GeoVector2D(periodicBounds.Left, periodicBounds.Bottom);
                    toNonPeriodicBounds = ModOp2D.Translate(-move) * toNonPeriodicBounds;
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
                        if (su[0] == periodicBounds.Left)
                        {
                            b = 1.0 / periodicBounds.Width;
                            a = -periodicBounds.Left * b;
                        }
                        else if (su[0] == periodicBounds.Right)
                        {
                            b = -1.0 / periodicBounds.Width;
                            a = periodicBounds.Right * b;
                        }
                        else throw new ApplicationException("pole must be on border");
                    }
                    else throw new ApplicationException("more than one pole");
                }
                else
                {
                    // bounds.Bottom => 0.5, bounds.Top => 1.5
                    b = 1.0 / periodicBounds.Width;
                    a = 0.5 - periodicBounds.Left / periodicBounds.Width;
                }
            }
            else
            {   // not periodic, only pole removal
                // map the "periodic" parameter (where there is a singularity) onto [0..pi/2], the other parameter to [0..1]
                bool done = false;
                double[] su = periodicSurface.GetUSingularities();
                if (su != null && su.Length == 1)
                {   // the "period" to be resolved is in v, when u==su[0] you can change v and the point stays fixed
                    if (su[0] == periodicBounds.Left)
                    {
                        toNonPeriodicBounds = ModOp2D.Scale(1.0 / periodicBounds.Width, Math.PI / 2.0 / periodicBounds.Height);
                        GeoVector2D move = toNonPeriodicBounds * new GeoVector2D(periodicBounds.Left, 0);
                        toNonPeriodicBounds = ModOp2D.Translate(-move) * toNonPeriodicBounds;
                    }
                    else if (su[0] == periodicBounds.Right)
                    {   // here we reflect the u scale, because we want to map su[0] (the right side) to 0
                        toNonPeriodicBounds = ModOp2D.Scale(-1.0 / periodicBounds.Width, Math.PI / 2.0 / periodicBounds.Height);
                        GeoVector2D move = toNonPeriodicBounds * new GeoVector2D(periodicBounds.Right, 0);
                        toNonPeriodicBounds = ModOp2D.Translate(-move) * toNonPeriodicBounds;
                    }
                    else throw new ApplicationException("the pole must be on the border of the bounds");
                    done = true;
                }
                if (!done)
                {
                    double[] sv = periodicSurface.GetVSingularities();
                    if (sv != null && sv.Length == 1)
                    {
                        if (sv[0] == periodicBounds.Bottom)
                        {
                            toNonPeriodicBounds = ModOp2D.Scale(1.0 / periodicBounds.Height, Math.PI / 2.0 / periodicBounds.Width);
                            GeoVector2D move = toNonPeriodicBounds * new GeoVector2D(periodicBounds.Bottom, 0);
                            toNonPeriodicBounds = ModOp2D.Translate(-move) * toNonPeriodicBounds;
                        }
                        else if (sv[0] == periodicBounds.Top)
                        {
                            toNonPeriodicBounds = ModOp2D.Scale(-1.0 / periodicBounds.Height, Math.PI / 2.0 / periodicBounds.Width);
                            GeoVector2D move = toNonPeriodicBounds * new GeoVector2D(periodicBounds.Top, 0);
                            toNonPeriodicBounds = ModOp2D.Translate(-move) * toNonPeriodicBounds;
                        }
                        else throw new ApplicationException("the pole must be on the border of the bounds");
                        done = true;
                    }
                }
                if (!done) throw new ApplicationException("there must be a single pole on the border of the bounds");
            }
            toPeriodicBounds = toNonPeriodicBounds.GetInverse();
        }
        public override GeoVector UDirection(GeoPoint2D uv)
        {
            periodicSurface.DerivationAt(toPeriodic(uv), out GeoPoint location, out GeoVector dirU, out GeoVector dirV);
            double l = uv.x * uv.x + uv.y * uv.y;
            double fx = uv.x / (Math.Sqrt(l) * toNonPeriodicBounds.Item(0, 0));
            double fy = -uv.y / (l * toNonPeriodicBounds.Item(1, 1));
            return fx * dirU + fy * dirV;
        }
        public override GeoVector VDirection(GeoPoint2D uv)
        {
            periodicSurface.DerivationAt(toPeriodic(uv), out GeoPoint location, out GeoVector dirU, out GeoVector dirV);
            double l = uv.x * uv.x + uv.y * uv.y;
            double fx = uv.y / (Math.Sqrt(l) * toNonPeriodicBounds.Item(0, 0));
            double fy = uv.x / (l * toNonPeriodicBounds.Item(1, 1));
            return fx * dirU + fy * dirV;
        }
        public override GeoPoint PointAt(GeoPoint2D uv)
        {
            return periodicSurface.PointAt(toPeriodic(uv));
        }
        public override ICurve FixedU(double u, double vmin, double vmax)
        {
            Line2D l2d = new Line2D(new GeoPoint2D(u, vmin), new GeoPoint2D(u, vmax));
            return new SurfaceCurve(this, l2d);
        }

        public override ICurve FixedV(double v, double umin, double umax)
        {
            Line2D l2d = new Line2D(new GeoPoint2D(umin, v), new GeoPoint2D(umax, v));
            return new SurfaceCurve(this, l2d);
        }

        public override ISurface GetModified(ModOp m)
        {
            return new NonPeriodicSurface(periodicSurface.GetModified(m), periodicBounds);
        }

        public override void GetNaturalBounds(out double umin, out double umax, out double vmin, out double vmax)
        {   // The periodic surface (or the surface with a pole) comes with the periodic bounds: typically in the periodic direction this is [0..2*pi] or [0..1]
            // and in the non periodic direction it is some min and max value.
            // In this surface, this area is mapped onto the unit circle (when there is a pole) or onto a ring (no pole, ring radius 0.5 to 1.5). 
            // The ring or circle is full when fullPeriod is true, or only the 1. quadrant when fullPeriod is false.
            if (fullPeriod)
            {
                if (hasPole)
                {
                    umin = -1; umax = 1;
                    vmin = -1; vmax = 1;
                }
                else
                {
                    umin = -1.5; umax = 1.5;
                    vmin = -1.5; vmax = 1.5;
                }
            }
            else
            {
                if (hasPole)
                {
                    umin = 0; umax = 1;
                    vmin = 0; vmax = 1;
                }
                else
                {
                    umin = 0; umax = 1.5;
                    vmin = 0; vmax = 1.5;
                }
            }
        }
        public override bool IsUPeriodic => false;
        public override bool IsVPeriodic => false;
        private GeoPoint2D toPeriodic(GeoPoint2D uv)
        {
            return toPeriodicBounds * new GeoPoint2D(Math.Sqrt(uv.x * uv.x + uv.y * uv.y), Math.Atan2(uv.y, uv.x));
        }
        private GeoPoint2D fromPeriodic(GeoPoint2D uv)
        {
            GeoPoint2D np = toNonPeriodicBounds * uv;
            return new GeoPoint2D(np.x * Math.Cos(np.y), np.x * Math.Sin(np.y));
        }
        private GeoVector2D toPeriodic(GeoVector2D uv)
        {
            return toPeriodicBounds * new GeoVector2D(Math.Sqrt(uv.x * uv.x + uv.y * uv.y), Math.Atan2(uv.y, uv.x));
        }
        private GeoVector2D fromPeriodic(GeoVector2D uv)
        {
            GeoVector2D np = toNonPeriodicBounds * uv;
            return new GeoVector2D(uv.x * Math.Cos(uv.y), uv.x * Math.Sin(uv.y));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("PeriodicSurface", periodicSurface);
            info.AddValue("PeriodicBounds", periodicBounds);
        }
        protected NonPeriodicSurface(SerializationInfo info, StreamingContext context)
        {
            periodicSurface = info.GetValue("PeriodicSurface", typeof(ISurface)) as ISurface;
            periodicBounds = (BoundingRect)info.GetValue("PeriodicBounds", typeof(BoundingRect));
        }
        void IDeserializationCallback.OnDeserialization(object sender)
        {
            Init();
        }

    }
}
