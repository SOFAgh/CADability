using CADability.Curve2D;
using CADability.Shapes;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CADability.GeoObject
{
    /// <summary>
    /// A cylindrical surface, defined by a point (location), and axis (zAxis). The axis (zAxis) and the two vectors xAxis and yAxis build an orthogonal coordinate system.
    /// xAxis and yAxis have the same length, which define the radius of the cylinder. The length of the zAxis is the domain of the cylindrical surface. points outside this
    /// domain are defined, but not used. The 2d u/v system is defined by the plane through "location" which is spanned by xAxis and yAxis. For each point on the surface, there 
    /// is a point in this plane, which is found by intersecting the line from (location-zAxis) to the surface point with the u/v plane. Thus all points of the domain on the surface
    /// are within an annulus with the cylinder radius and a hole of half the cylinder radius. The u/v system is not periodic and has no poles (except of the 2d origin, 
    /// which is at the infinite end of the axis). When the orthogonal system of xAxis, yAxis and zAxis is right handed, the normal vectors point to the outside, otherwise 
    /// they point to the inside. Circles, ellipses and lines in 3d correspond to circles, ellipses and lines in 2d. 3d ellipses with their center at (location-zAxis)
    /// degenerate to lines in 2d.
    /// </summary>
    [Serializable]
    public class CylindricalSurfaceNP : ISurfaceImpl, IJsonSerialize, ISerializable, IRestrictedDomain
    {
        GeoPoint location; // the location at the lowest (in the direction of zAxis) usable position of the cylinder
        GeoVector xAxis, yAxis, zAxis; // the system at the "bottom" of the cylinder. Should all have the same length
                                       // The projection goes from the point (location-zAxis) to the plane (location, xAxis, yAxis). the u/v system is the plane

        public CylindricalSurfaceNP(GeoPoint location, GeoVector xAxis, GeoVector yAxis, GeoVector zAxis)
        {
            this.location = location;
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.zAxis = zAxis;
        }
        public CylindricalSurfaceNP(GeoPoint center, double radius, GeoVector axis, bool outwardOriented, ICurve[] orientedCurves)
        {
            double minpos = double.MaxValue;
            double maxpos = double.MinValue;
            for (int i = 0; i < orientedCurves.Length; i++)
            {
                double[] extr = orientedCurves[i].GetExtrema(axis);
                double lp;
                for (int j = 0; j < extr.Length; j++)
                {
                    lp = Geometry.LinePar(center, axis, orientedCurves[i].PointAt(extr[j]));
                    minpos = Math.Min(minpos, lp);
                    maxpos = Math.Max(maxpos, lp);
                }
                lp = Geometry.LinePar(center, axis, orientedCurves[i].StartPoint);
                minpos = Math.Min(minpos, lp);
                maxpos = Math.Max(maxpos, lp);
                lp = Geometry.LinePar(center, axis, orientedCurves[i].EndPoint);
                minpos = Math.Min(minpos, lp);
                maxpos = Math.Max(maxpos, lp);
            }
            location = Geometry.LinePos(center, axis, minpos);
            axis.ArbitraryNormals(out xAxis, out yAxis);
            if (!outwardOriented) xAxis = -xAxis;
            zAxis = axis;
            xAxis.Length = radius;
            yAxis.Length = radius;
            zAxis.Length = (maxpos - minpos) * axis.Length;
        }

        public override ICurve FixedU(double u, double vmin, double vmax)
        {
            GeoPoint majax = PointAt(new GeoPoint2D(u, 0));
            Ellipse e = Ellipse.Construct();
            GeoPoint center = location - zAxis;
            e.SetEllipseCenterAxis(center, majax - center, yAxis);
            GeoPoint sp = PointAt(new GeoPoint2D(u, vmin));
            GeoPoint ep = PointAt(new GeoPoint2D(u, vmax));
            double spos = e.PositionOf(sp);
            double epos = e.PositionOf(ep);
            e.Trim(spos, epos);
            return e;
        }

        public override ICurve FixedV(double v, double umin, double umax)
        {
            GeoPoint majax = PointAt(new GeoPoint2D(0, v));
            Ellipse e = Ellipse.Construct();
            GeoPoint center = location - zAxis;
            e.SetEllipseCenterAxis(center, majax - center, xAxis);
            GeoPoint sp = PointAt(new GeoPoint2D(umin, v));
            GeoPoint ep = PointAt(new GeoPoint2D(umax, v));
            double spos = e.PositionOf(sp);
            double epos = e.PositionOf(ep);
            e.Trim(spos, epos);
            return e;
        }

        public override ISurface GetModified(ModOp m)
        {
            return new CylindricalSurfaceNP(m * location, m * xAxis, m * yAxis, m * zAxis);
        }

        public override GeoPoint PointAt(GeoPoint2D uv)
        {
            double l = Math.Sqrt(uv.x * uv.x + uv.y * uv.y);
            if (l == 0.0) return location + zAxis; // this is invalid and should never be used. 
            double r = xAxis.Length;
            double z = (r - l) / l;
            double a = Math.Atan2(uv.y, uv.x);
            return location + Math.Cos(a) * xAxis + Math.Sin(a) * yAxis + z * zAxis;
        }

        public override GeoVector UDirection(GeoPoint2D uv)
        {
            double l2 = uv.x * uv.x + uv.y * uv.y;
            if (l2 == 0.0) return GeoVector.NullVector;
            double l = Math.Sqrt(l2);
            double l32 = Math.Sqrt(l2 * l2 * l2);
            double r = xAxis.Length;
            return (-(uv.x * (r - l)) / l32) * zAxis - (uv.x) / l2 * zAxis - (uv.x * uv.y) / l32 * yAxis + 1 / l * xAxis - (uv.x * uv.x) / l32 * xAxis;
        }

        public override GeoVector VDirection(GeoPoint2D uv)
        {
            double l2 = uv.x * uv.x + uv.y * uv.y;
            if (l2 == 0.0) return GeoVector.NullVector;
            double l = Math.Sqrt(l2);
            double l32 = Math.Sqrt(l2 * l2 * l2);
            double r = xAxis.Length;
            return (-(uv.y * (r - l)) / l32) * zAxis - (uv.y) / l2 * zAxis + 1 / l * yAxis - (uv.y * uv.y) / l32 * yAxis - (uv.x * uv.y) / l32 * xAxis;
        }
        public override void Derivation2At(GeoPoint2D uv, out GeoPoint p, out GeoVector du, out GeoVector dv, out GeoVector duu, out GeoVector dvv, out GeoVector duv)
        {   // from maxima and common sub expression
            double u = uv.x;
            double v = uv.y;
            double r = xAxis.Length;
            double uv52 = exp52(sqr(v) + sqr(u));
            double uv32 = exp32(sqr(v) + sqr(u));
            GeoVector zl = 1 / (sqr(v) + sqr(u)) * zAxis;
            double uvl = r - Math.Sqrt(sqr(v) + sqr(u));
            GeoVector sc1 = (-(uvl * zAxis)) / uv32;
            p = location + uvl / Math.Sqrt(sqr(v) + sqr(u)) * zAxis  + u / Math.Sqrt(sqr(v) + sqr(u)) * yAxis  + v / Math.Sqrt(sqr(v) + sqr(u)) * xAxis;
            du = (-(u * uvl * zAxis)) / uv32 - u * zAxis / (sqr(v) + sqr(u)) + yAxis / Math.Sqrt(sqr(v) + sqr(u)) - sqr(u) * yAxis / uv32 - u * v * xAxis / uv32;
            dv = (-(v * uvl * zAxis)) / uv32 - v * zAxis / (sqr(v) + sqr(u)) - u * v * yAxis / uv32 + xAxis / Math.Sqrt(sqr(v) + sqr(u)) - sqr(v) * xAxis / uv32;
            duu = sc1 + 3.0 * sqr(u) * uvl * zAxis / uv52 - zl + 3.0 * sqr(u) * zAxis / sqr(sqr(v) + sqr(u)) - 3.0 * u * yAxis / uv32 + 3.0 * cube(u) * yAxis / uv52 - v * xAxis / uv32 + 3.0 * sqr(u) * v * xAxis / uv52;
            dvv = sc1 + 3.0 * sqr(v) * uvl * zAxis / uv52 - zl + 3.0 * sqr(v) * zAxis / sqr(sqr(v) + sqr(u)) - u * yAxis / uv32 + 3.0 * u * sqr(v) * yAxis / uv52 - 3.0 * v * xAxis / uv32 + 3.0 * cube(v) * xAxis / uv52;
            duv = 3.0 * u * v * uvl * zAxis / uv52 + 3.0 * u * v * zAxis / sqr(sqr(v) + sqr(u)) - v * yAxis / uv32 + 3.0 * sqr(u) * v * yAxis / uv52 - u * xAxis / uv32 + 3.0 * u * sqr(v) * xAxis / uv52;
        }
        public override GeoPoint2D PositionOf(GeoPoint p)
        {
            double r = xAxis.Length;
            GeoVector v = Geometry.ReBase(p - (location - zAxis), xAxis, yAxis, zAxis);
            GeoVector2D v2d = new GeoVector2D(v.x, v.y);
            v2d.Length = r / v.z;
            return new GeoPoint2D(v2d.x, v2d.y);
        }
        public override ModOp2D ReverseOrientation()
        {   // flip x and y axis, keep z axis, left handed system
            GeoVector tmp = xAxis;
            xAxis = yAxis;
            yAxis = tmp;
            return new ModOp2D(0, 1, 0, 1, 0, 0);

        }
        public override bool IsUPeriodic => false;
        public override bool IsVPeriodic => false;
        public override bool SameGeometry(BoundingRect thisBounds, ISurface other, BoundingRect otherBounds, double precision, out ModOp2D firstToSecond)
        {
            if (other is CylindricalSurfaceNP cnp)
            {
                if (Geometry.DistPL(cnp.location, location, zAxis) < precision && Geometry.DistPL(cnp.location + cnp.zAxis, location, zAxis) < precision)
                {
                    firstToSecond = ModOp2D.Null;
                    return true;
                }
                firstToSecond = ModOp2D.Null;
                return false;
            }
            return base.SameGeometry(thisBounds, other, otherBounds, precision, out firstToSecond);
        }

        void IJsonSerialize.GetObjectData(IJsonWriteData data)
        {
            data.AddProperty("Location", location);
            data.AddProperty("XAxis", xAxis);
            data.AddProperty("YAxis", yAxis);
            data.AddProperty("ZAxis", zAxis);
        }
        protected CylindricalSurfaceNP() { } // for JSON
        void IJsonSerialize.SetObjectData(IJsonReadData data)
        {
            location = data.GetProperty<GeoPoint>("Location");
            xAxis = data.GetProperty<GeoVector>("XAxis");
            yAxis = data.GetProperty<GeoVector>("YAxis");
            zAxis = data.GetProperty<GeoVector>("ZAxis");
        }
        protected CylindricalSurfaceNP(SerializationInfo info, StreamingContext context)
        {
            location = (GeoPoint)info.GetValue("Location", typeof(GeoPoint));
            xAxis = (GeoVector)info.GetValue("XAxis", typeof(GeoVector));
            yAxis = (GeoVector)info.GetValue("YAxis", typeof(GeoVector));
            zAxis = (GeoVector)info.GetValue("ZAxis", typeof(GeoVector));
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Location", location);
            info.AddValue("XAxis", xAxis);
            info.AddValue("YAxis", yAxis);
            info.AddValue("ZAxis", zAxis);
        }
        public override void GetSafeParameterSteps(double umin, double umax, double vmin, double vmax, out double[] intu, out double[] intv)
        {   // here we want to provide parameter steps so that there are invalid patches, which are completely outside the annulus.
            // The boxed surface ignores patches which return false for "IsInside" for all its vertices. And with this segmentation, 
            // these pathological patches are avoided.
            double r = xAxis.Length;
            List<double> usteps = new List<double>();
            List<double> vsteps = new List<double>();
            // a annulus from -r to r with a hole of radius r/2: the four corner patches and the central patch are totally outside of the annulus
            // The central patch may contain an infinite pole
            if (umin > -r) usteps.Add(umin);
            if (vmin > -r) vsteps.Add(vmin);
            for (int i = 0; i < 8; i++)
            {
                double d = i * 2 * r / 7.0 - r;
                if (d >= umin && d <= umax) usteps.Add(d);
                if (d >= vmin && d <= vmax) vsteps.Add(d);
            }
            if (umax < r) usteps.Add(umax);
            if (vmax < r) vsteps.Add(vmax);
            intu = usteps.ToArray();
            intv = vsteps.ToArray();
        }
        public override ICurve2D GetProjectedCurve(ICurve curve, double precision)
        {
            if (curve is Ellipse elli)
            {   // this must be a ellipse in 2d with the center at 0,0
                GeoVector2D majax = PositionOf(elli.Center + elli.MajorAxis).ToVector();
                GeoVector2D minax = PositionOf(elli.Center + elli.MinorAxis).ToVector();
                if (GeoVector2D.Orientation(majax, minax) < 0) minax = -minax;
                if (elli.IsArc)
                {
                    EllipseArc2D ea1 = EllipseArc2D.Create(GeoPoint2D.Origin, majax, minax, PositionOf(elli.StartPoint), PositionOf(elli.EndPoint), true);
                    EllipseArc2D ea2 = EllipseArc2D.Create(GeoPoint2D.Origin, majax, minax, PositionOf(elli.StartPoint), PositionOf(elli.EndPoint), false);
                    GeoPoint2D mp = PositionOf(elli.PointAt(0.5));
                    double pos1 = ea1.PositionOf(mp);
                    double pos2 = ea2.PositionOf(mp);
                    if (Math.Abs(pos1 - 0.5) < Math.Abs(pos2 - 0.5)) return ea1;
                    else return ea2;
                }
                else
                {   // the ellipse may start at any point, a 2d ellipse doesn't have a start angle, it is always assumed to be 0
                    // so we make an ellipse 2d arc, which has startpoint
                    EllipseArc2D e = EllipseArc2D.Create(GeoPoint2D.Origin, majax, minax, PositionOf(elli.StartPoint), PositionOf(elli.EndPoint), true);
                    e.MakeFullEllipse(); // it might have been an extremely small arc before
                    if (elli.PositionOf(PointAt(e.PointAt(0.1))) > 0.5) e.Reverse();
                    return e;
                }
            } else if (curve is Line line)
            {   // this must be a line parallel to the axis, i.e. in 2d a line through the origin
                return new Line2D(PositionOf(line.StartPoint), PositionOf(line.EndPoint));
            }
            return base.GetProjectedCurve(curve, precision);
        }
        public bool IsInside(GeoPoint2D uv)
        {
            double r = xAxis.Length;
            double l = Math.Sqrt(uv.x * uv.x + uv.y * uv.y);
            return l <= r && l >= r / 2;
        }

        public double[] Clip(ICurve2D curve)
        {
            SimpleShape ss = new SimpleShape(Border.MakeCircle(GeoPoint2D.Origin, xAxis.Length), Border.MakeCircle(GeoPoint2D.Origin, xAxis.Length / 2.0));
            return ss.Clip(curve, true);
        }
        public override void GetNaturalBounds(out double umin, out double umax, out double vmin, out double vmax)
        {
            double r = xAxis.Length;
            umin = vmin = -r;
            umax = vmax = r;
        }
        public override ISurface Clone()
        {
            return new CylindricalSurfaceNP(location, xAxis, yAxis, zAxis);
        }
        public override bool UvChangesWithModification => true;
        public override IPropertyEntry PropertyEntry
        {
            get
            {
                List<IPropertyEntry> se = new List<IPropertyEntry>();
                GeoPointProperty location = new GeoPointProperty("CylindricalSurface.Location", base.Frame, false);
                location.ReadOnly = true;
                location.GetGeoPointEvent += delegate (GeoPointProperty sender) { return this.location; };
                se.Add(location);
                GeoVectorProperty dirx = new GeoVectorProperty("CylindricalSurface.DirectionX", base.Frame, false);
                dirx.ReadOnly = true;
                dirx.IsAngle = false;
                dirx.GetGeoVectorEvent += delegate (GeoVectorProperty sender) { return xAxis; };
                se.Add(dirx);
                GeoVectorProperty diry = new GeoVectorProperty("CylindricalSurface.DirectionY", base.Frame, false);
                diry.ReadOnly = true;
                diry.IsAngle = false;
                diry.GetGeoVectorEvent += delegate (GeoVectorProperty sender) { return yAxis; };
                se.Add(diry);
                if (Precision.IsEqual(xAxis.Length, yAxis.Length))
                {
                    DoubleProperty radius = new DoubleProperty("CylindricalSurface.Radius", base.Frame);
                    radius.ReadOnly = true;
                    radius.DoubleValue = xAxis.Length;
                    radius.GetDoubleEvent += delegate (DoubleProperty sender) { return xAxis.Length; };
                    se.Add(radius);
                }
                return new GroupProperty("CylindricalSurface", se.ToArray());
            }
        }
    }
}
