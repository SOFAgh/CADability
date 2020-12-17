using CADability.Curve2D;
using CADability.Shapes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CADability.GeoObject
{
    /// <summary>
    /// Interface for surfaces with non rectangular definition space. Normally surfaces are either defined for all 2d values (infinite)
    /// or they are restricted to a rectangular domain (e.g. <see cref="NurbsSurface"/>). The <see cref="NonPeriodicSurface"/> is only defined on a
    /// circular area or on a ring. Maybe other surfaces with irregular domains will follow. 
    /// </summary>
    public interface IRestrictedDomain
    {
        /// <summary>
        /// True, if the provided <paramref name="uv"/> value is inside the definition domain
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        bool IsInside(GeoPoint2D uv);
        /// <summary>
        /// Clip the provided <paramref name="curve"/> at the bounds of the domain. The result are pairs of parameters of the curve, which define segments, that are inside the domain.
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        double[] Clip(ICurve2D curve);
    }
    /// <summary>
    /// Non-periodic surface made from an underlying periodic surface. The underlying periodic surface must be periodic in u or in v and may have a pole 
    /// at the minimum or maximum of the non periodic parameter. The underlying periodic surface may also be non periodic but have one singularity, which will be removed.
    /// </summary>
    [Serializable]
    public class NonPeriodicSurface : ISurfaceImpl, ISerializable, IDeserializationCallback, IRestrictedDomain, IJsonSerialize, IJsonSerializeDone
    {
        ISurface periodicSurface;
        BoundingRect periodicBounds;
        bool hasPole, fullPeriod;
        ModOp2D toNonPeriodicBounds, toPeriodicBounds;
        GeoPoint extendedPole; // when there is no pole, the definition area is a annulus (circular ring). extendedPole is the point at (0,0)
        /// <summary>
        /// </summary>
        /// <param name="periodicSurface"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        public NonPeriodicSurface(ISurface periodicSurface, BoundingRect bounds)
        {
            this.periodicSurface = periodicSurface;
            this.periodicBounds = bounds;
            if (periodicSurface.IsUPeriodic && periodicSurface.IsVPeriodic)
            {
                throw new NotImplementedException("NonPeriodicSurface: both u and v are periodic");
            }
            else if (periodicSurface.IsUPeriodic)
            {
                periodicBounds.Left = 0.0;
                periodicBounds.Right = periodicSurface.UPeriod;
            }
            else if (periodicSurface.IsVPeriodic)
            {
                periodicBounds.Bottom = 0.0;
                periodicBounds.Top = periodicSurface.VPeriod;
            }
            Init();
        }
        private void Init()
        {
            hasPole = false;
            if (periodicSurface.IsUPeriodic && periodicSurface.IsVPeriodic)
            {
                throw new NotImplementedException("NonPeriodicSurface: both u and v are periodic");
            }
            else if (periodicSurface.IsUPeriodic)
            {
                fullPeriod = true;
                double[] sv = periodicSurface.GetVSingularities();
                if (sv != null && sv.Length > 0)
                {
                    if (sv.Length == 1)
                    {
                        hasPole = true;
                        if (sv[0] == periodicBounds.Bottom)
                        {
                            // bounds.Bottom => 0.0, bounds.Top => 1.0
                            // bounds.Left => 0, bountd.Right => 2*pi
                            double fvu = 2.0 * Math.PI / periodicBounds.Width;
                            double fuv = 1.0 / periodicBounds.Height;
                            toNonPeriodicBounds = new ModOp2D(fvu, 0, -fvu * periodicBounds.Left, 0, fuv, -fuv * periodicBounds.Bottom);
                        }
                        else if (sv[0] == periodicBounds.Top)
                        {
                            // bounds.Bottom => 0.0, bounds.Top => 1.0
                            // bounds.Left => 2*pi, bountd.Right => 0
                            double fvu = 2.0 * Math.PI / periodicBounds.Width;
                            double fuv = 1.0 / periodicBounds.Height;
                            toNonPeriodicBounds = new ModOp2D(-fvu, 0, fvu * periodicBounds.Right, 0, fuv, -fuv * periodicBounds.Bottom);
                        }
                        else throw new ApplicationException("pole must be on border");
                    }
                    else throw new ApplicationException("more than one pole");
                }
                else
                {
                    // bounds.Bottom => 0.5, bounds.Top => 1.5
                    // bounds.Left => 0, bountd.Right => 2*pi
                    double fvu = 2.0 * Math.PI / periodicBounds.Width;
                    double fuv = 1.0 / periodicBounds.Height;
                    toNonPeriodicBounds = new ModOp2D(fvu, 0, -fvu * periodicBounds.Left - Math.PI, 0, fuv, -fuv * periodicBounds.Bottom + 0.5);
                }
            }
            else if (periodicSurface.IsVPeriodic)
            {
                fullPeriod = true;
                double[] su = periodicSurface.GetUSingularities();
                if (su != null && su.Length > 0)
                {
                    if (su.Length == 1)
                    {
                        hasPole = true;
                        if (su[0] == periodicBounds.Left)
                        {
                            double fvu = 2.0 * Math.PI / periodicBounds.Height;
                            double fuv = 1.0 / periodicBounds.Width;
                            toNonPeriodicBounds = new ModOp2D(0, fvu, -fvu * periodicBounds.Bottom, fuv, 0, -fuv * periodicBounds.Left);
                        }
                        else if (su[0] == periodicBounds.Right)
                        {
                            double fvu = 2.0 * Math.PI / periodicBounds.Height;
                            double fuv = 1.0 / periodicBounds.Width;
                            toNonPeriodicBounds = new ModOp2D(0, -fvu, fvu * periodicBounds.Top, fuv, 0, -fuv * periodicBounds.Left);
                        }
                        else throw new ApplicationException("pole must be on border");
                    }
                    else throw new ApplicationException("more than one pole");
                }
                else
                {
                    double fvu = 2.0 * Math.PI / periodicBounds.Height;
                    double fuv = 1.0 / periodicBounds.Width;
                    toNonPeriodicBounds = new ModOp2D(0, fvu, -fvu * periodicBounds.Bottom, fuv, 0, -fuv * periodicBounds.Left + 0.5);
                }
            }
            else
            {   // not periodic, only pole removal
                // map the "periodic" parameter (where there is a singularity) onto [0..pi/2], the other parameter to [0..1]
                bool done = false;
                double[] su = periodicSurface.GetUSingularities();
                if (su != null && su.Length == 1)
                {   // the "period" to be resolved is in v, when u==su[0] you can change v and the point stays fixed
                    hasPole = true;
                    if (su[0] == periodicBounds.Left)
                    {   // we have to map the v interval of the periodic surface onto [0..pi/2] and the u interval onto [0..1]
                        // and we also have to exchange u and v, so the non periodic bounds are [0..pi/2] in u and [0..1] in v
                        double fvu = Math.PI / 2.0 / periodicBounds.Height;
                        double fuv = 1.0 / periodicBounds.Width;
                        toNonPeriodicBounds = new ModOp2D(0, fvu, -fvu * periodicBounds.Bottom, fuv, 0, -fuv * periodicBounds.Left);
                    }
                    else if (su[0] == periodicBounds.Right)
                    {   // here we additionally have tor reverse the v interval
                        double fvu = Math.PI / 2.0 / periodicBounds.Height;
                        double fuv = 1.0 / periodicBounds.Width;
                        toNonPeriodicBounds = new ModOp2D(0, -fvu, fvu * periodicBounds.Top, -fuv, 0, fuv * periodicBounds.Right);
                    }
                    else throw new ApplicationException("the pole must be on the border of the bounds");
                    done = true;
                }
                if (!done)
                {
                    double[] sv = periodicSurface.GetVSingularities();
                    if (sv != null && sv.Length == 1)
                    {   // the "period" to be resolved is in u
                        hasPole = true;
                        if (sv[0] == periodicBounds.Bottom)
                        {   // we have to map the u interval of the periodic surface onto [0..pi/2] and the v interval onto [0..1]
                            double fvu = Math.PI / 2.0 / periodicBounds.Width;
                            double fuv = 1.0 / periodicBounds.Height;
                            toNonPeriodicBounds = new ModOp2D(fvu, 0, -fvu * periodicBounds.Left, 0, fuv, -fuv * periodicBounds.Bottom);
                        }
                        else if (sv[0] == periodicBounds.Top)
                        {
                            double fvu = Math.PI / 2.0 / periodicBounds.Width;
                            double fuv = 1.0 / periodicBounds.Height;
                            toNonPeriodicBounds = new ModOp2D(-fvu, 0, fvu * periodicBounds.Right, 0, fuv, -fuv * periodicBounds.Bottom);
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
            GeoVector2D f = toPeriodicBounds * new GeoVector2D(-uv.y / l, uv.x / Math.Sqrt(l));
            return (f.x * dirU + f.y * dirV);
        }
        public override GeoVector VDirection(GeoPoint2D uv)
        {
            periodicSurface.DerivationAt(toPeriodic(uv), out GeoPoint location, out GeoVector dirU, out GeoVector dirV);
            double l = uv.x * uv.x + uv.y * uv.y;
            GeoVector2D f = toPeriodicBounds * new GeoVector2D(uv.x / l, uv.y / Math.Sqrt(l));
            return (f.x * dirU + f.y * dirV);
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
        public override void GetSafeParameterSteps(double umin, double umax, double vmin, double vmax, out double[] intu, out double[] intv)
        {   // here we want to provide parameter steps so that there are invalid patches, which are completely outside the annulus or circle.
            // The boxed surface ignores patches which return false for "IsInside" for all its vertices. And with this segmentation, 
            // these pathological patches are avoided.
            List<double> usteps = new List<double>();
            List<double> vsteps = new List<double>();
            if (hasPole)
            {
                // a unit circle: the four corner patches are outside of the circular area
                if (umin > -1.0) usteps.Add(umin);
                if (vmin > -1.0) vsteps.Add(vmin);
                for (int i = 0; i < 8; i++)
                {
                    double d = i * 2 / 7.0 - 1.0;
                    if (d >= umin && d <= umax) usteps.Add(d);
                    if (d >= vmin && d <= vmax) vsteps.Add(d);
                }
                if (umax < 1.0) usteps.Add(umax);
                if (vmax < 1.0) vsteps.Add(vmax);
                intu = usteps.ToArray();
                intv = vsteps.ToArray();
            }
            else
            {
                // a annulus from -1.5 to 1.5: the four corner patches and the central patch are totally outside of the annulus
                // The central patch may contain an infinite pole
                if (umin > -1.5) usteps.Add(umin);
                if (vmin > -1.5) vsteps.Add(vmin);
                for (int i = 0; i < 8; i++)
                {
                    double d = i * 3 / 7.0 - 1.5;
                    if (d >= umin && d <= umax) usteps.Add(d);
                    if (d >= vmin && d <= vmax) vsteps.Add(d);
                }
                if (umax < 1.5) usteps.Add(umax);
                if (vmax < 1.5) vsteps.Add(vmax);
                intu = usteps.ToArray();
                intv = vsteps.ToArray();
            }
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
            return toPeriodicBounds * new GeoPoint2D(Math.Atan2(uv.y, uv.x), Math.Sqrt(uv.x * uv.x + uv.y * uv.y));
        }
        private GeoPoint2D fromPeriodic(GeoPoint2D uv)
        {
            GeoPoint2D np = toNonPeriodicBounds * uv;
            return new GeoPoint2D(np.y * Math.Cos(np.x), np.y * Math.Sin(np.x));
        }
        private GeoVector2D toPeriodic(GeoVector2D uv)
        {
            return toPeriodicBounds * new GeoVector2D(Math.Atan2(uv.y, uv.x), Math.Sqrt(uv.x * uv.x + uv.y * uv.y));
        }
        private GeoVector2D fromPeriodic(GeoVector2D uv)
        {
            GeoVector2D np = toNonPeriodicBounds * uv;
            return new GeoVector2D(np.y * Math.Cos(np.x), np.y * Math.Sin(np.x));
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
        public bool IsInside(GeoPoint2D uv)
        {
            double l = (uv - GeoPoint2D.Origin).Length;
            if (fullPeriod)
            {
                if (hasPole) return l >= 1.0;
                else return l >= 0.5 && l <= 1.5;
            }
            else
            {
                double a = Math.Atan2(uv.y, uv.x);
                return l <= 1.0 && a >= 0.0 && a <= Math.PI / 2.0;
            }
        }
        public double[] Clip(ICurve2D curve)
        {
            SimpleShape ss;
            if (fullPeriod)
            {
                if (hasPole) ss = new SimpleShape(Border.MakeCircle(GeoPoint2D.Origin, 1.0));
                else ss = new SimpleShape(Border.MakeCircle(GeoPoint2D.Origin, 1.5), Border.MakeCircle(GeoPoint2D.Origin, 0.5));
            }
            else
            {
                ss = new SimpleShape(new Border(new ICurve2D[] { new Line2D(GeoPoint2D.Origin, new GeoPoint2D(1, 0)), new Arc2D(GeoPoint2D.Origin, 1, 0, Math.PI / 2.0), new Line2D(new GeoPoint2D(0, 1), new GeoPoint2D(0, 0)) }));
            }
            return ss.Clip(curve, true);
        }
        /// <summary>
        /// Empty constructor for JSON serialization
        /// </summary>
        protected NonPeriodicSurface() { }
        void IJsonSerialize.GetObjectData(IJsonWriteData data)
        {
            data.AddProperty("PeriodicSurface", periodicSurface);
            data.AddProperty("PeriodicBounds", periodicBounds);
        }
        void IJsonSerialize.SetObjectData(IJsonReadData data)
        {
            periodicSurface = data.GetProperty<ISurface>("PeriodicSurface");
            periodicBounds = data.GetProperty<BoundingRect>("PeriodicBounds");
            data.RegisterForSerializationDoneCallback(this);
        }
        void IJsonSerializeDone.SerializationDone()
        {
            Init();
        }
    }
}
