using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using CADability.Curve2D;

namespace CADability.GeoObject
{
    [Serializable()]
    public class ConicalSurfaceNP : ISurfaceImpl, IJsonSerialize, ISerializable
    {
        GeoPoint location; // the location of the tip of the cone
        GeoVector xAxis, yAxis, zAxis; // the axis build a perpendicular coordinate system, the zAxis is the axis of the cone. Right handed: the axis is inside the cone
        // only the part int the direction of the zAxis is used. the x and y system define the u,v system, the length of the zAxis with respect to the length of x and y axis
        // define the opening angle
        public ConicalSurfaceNP(GeoPoint location, GeoVector xAxis, GeoVector yAxis, GeoVector zAxis, double semiAngle)
        {
            this.location = location;
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.zAxis = zAxis; // xAxis and yAxis are assumed to have the same length (otherwise opening angle is meaningless)
            this.xAxis.Length = 1.0;
            this.yAxis.Length = 1.0;
            this.zAxis.Length = 1.0 / Math.Tan(semiAngle);
        }
        public ConicalSurfaceNP(GeoPoint location, GeoVector xAxis, GeoVector yAxis, GeoVector zAxis)
        {
            this.location = location;
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.zAxis = zAxis;
        }
        public override ISurface Clone()
        {
            return new ConicalSurfaceNP(location, xAxis, yAxis, zAxis);
        }
        public override ICurve FixedU(double u, double vmin, double vmax)
        {   // the result would be a parabola, which is not implemented as a 3d curve
            return new SurfaceCurve(this, new Line2D(new GeoPoint2D(u, vmin), new GeoPoint2D(u, vmax)));
        }
        public override ICurve FixedV(double v, double umin, double umax)
        {
            return new SurfaceCurve(this, new Line2D(new GeoPoint2D(umin, v), new GeoPoint2D(umax, v)));
        }
        public override ISurface GetModified(ModOp m)
        {   // m must be orthogonal
            return new ConicalSurfaceNP(m * location, m * xAxis, m * yAxis, m * zAxis);
        }
        public override GeoPoint PointAt(GeoPoint2D uv)
        {
            return location + (Math.Sqrt(sqr(uv.y) + sqr(uv.x)) * zAxis) + uv.y * yAxis + uv.x * xAxis;
        }
        public override GeoPoint2D PositionOf(GeoPoint p)
        {
            GeoVector rb = Geometry.ReBase(p - location, xAxis, yAxis, zAxis);
            return new GeoPoint2D(rb.x, rb.y);
        }
        public override GeoVector UDirection(GeoPoint2D uv)
        {
            double u = uv.x;
            double v = uv.y;
            return u * zAxis / Math.Sqrt(sqr(v) + sqr(u)) + xAxis;
        }
        public override GeoVector VDirection(GeoPoint2D uv)
        {
            double u = uv.x;
            double v = uv.y;
            return v * zAxis / Math.Sqrt(sqr(v) + sqr(u)) + yAxis;
        }
        public override void DerivationAt(GeoPoint2D uv, out GeoPoint p, out GeoVector du, out GeoVector dv)
        {
            double u = uv.x;
            double v = uv.y;
            double sc3 = Math.Sqrt(sqr(v) + sqr(u));
            p = location + (sc3 * zAxis) + v * yAxis + u * xAxis;
            du = u * zAxis / sc3 + xAxis;
            dv = v * zAxis / sc3 + yAxis;
        }
        public override void Derivation2At(GeoPoint2D uv, out GeoPoint p, out GeoVector du, out GeoVector dv, out GeoVector duu, out GeoVector dvv, out GeoVector duv)
        {
            double u = uv.x;
            double v = uv.y;
            double sc3 = Math.Sqrt(sqr(v) + sqr(u));
            double sc2 = exp32(sqr(v) + sqr(u));
            GeoVector sc1 = zAxis / sc3;
            p = location + (sc3 * zAxis) + v * yAxis + u * xAxis;
            du = u * zAxis / sc3 + xAxis;
            dv = v * zAxis / sc3 + yAxis;
            duu = sc1 - sqr(u) * zAxis / sc2;
            dvv = sc1 - sqr(v) * zAxis / sc2;
            duv = (-(u * v * zAxis)) / sc2;
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
        public override bool UvChangesWithModification => true;
        public override bool SameGeometry(BoundingRect thisBounds, ISurface other, BoundingRect otherBounds, double precision, out ModOp2D firstToSecond)
        {
            if (other is ConicalSurfaceNP cnp)
            {
                if ((cnp.location | location) < precision && Precision.SameDirection(zAxis, cnp.zAxis, false)) // SameDirection is not with respect to precision!
                {
                    firstToSecond = ModOp2D.Null;
                    return true;
                }
                firstToSecond = ModOp2D.Null;
                return false;
            }
            return base.SameGeometry(thisBounds, other, otherBounds, precision, out firstToSecond);
        }
        public double OpeningAngle
        {
            get
            {
                return 2 * Math.Atan2(xAxis.Length, zAxis.Length);
            }
        }
        public override Polynom GetImplicitPolynomial()
        {
            double xz2 = (2.0 * xAxis.z);
            double xy2 = 2.0 * xAxis.y;
            double xx2 = 2.0 * xAxis.x;
            double sc19 = 2.0 * sqr(zAxis.x);
            double sc18 = xx2 * zAxis.x;
            double sc17 = xy2 * zAxis.y;
            double c = (quad(zAxis.z) + xz2 * cube(zAxis.z) + (2.0 * sqr(zAxis.y) + sc17 + sc19 + sc18 + sqr(xAxis.z)) * sqr(zAxis.z) + (xz2 * sqr(zAxis.y) + xy2 * xAxis.z * zAxis.y + xz2 * sqr(zAxis.x) + xx2 * xAxis.z * zAxis.x) * zAxis.z + quad(zAxis.y) + xy2 * cube(zAxis.y) + (sc19 + sc18 + sqr(xAxis.y)) * sqr(zAxis.y) + (xy2 * sqr(zAxis.x) + xx2 * xAxis.y * zAxis.x) * zAxis.y + quad(zAxis.x) + xx2 * cube(zAxis.x) + sqr(xAxis.x) * sqr(zAxis.x)) / (sqr(zAxis.z) + xz2 * zAxis.z + sqr(zAxis.y) + sc17 + sqr(zAxis.x) + sc18 + sqr(xAxis.z) + sqr(xAxis.y) + sqr(xAxis.x));
            if ((xAxis ^ yAxis) * zAxis < 0) c = -c;
            Polynom x = new Polynom(1, "x", 0, "y", 0, "z");
            Polynom y = new Polynom(0, "x", 1, "y", 0, "z");
            Polynom z = new Polynom(0, "x", 0, "y", 1, "z");
            return (((z - location.z) * zAxis.z + (y - location.y) * zAxis.y + (x - location.x) * zAxis.x)^2) - c * (((z - location.z)^2) + ((y - location.y)^2) + ((x - location.x)^2));
        }
        public double DebugImplicit(GeoPoint p)
        {
            double sc22 = (2.0 * xAxis.z);
            double sc21 = 2.0 * xAxis.y;
            double sc20 = 2.0 * xAxis.x;
            double sc19 = 2.0 * sqr(zAxis.x);
            double sc18 = sc20 * zAxis.x;
            double sc17 = sc21 * zAxis.y;
            double c = (quad(zAxis.z) + sc22 * cube(zAxis.z) + (2.0 * sqr(zAxis.y) + sc17 + sc19 + sc18 + sqr(xAxis.z)) * sqr(zAxis.z) + (sc22 * sqr(zAxis.y) + sc21 * xAxis.z * zAxis.y + sc22 * sqr(zAxis.x) + sc20 * xAxis.z * zAxis.x) * zAxis.z + quad(zAxis.y) + sc21 * cube(zAxis.y) + (sc19 + sc18 + sqr(xAxis.y)) * sqr(zAxis.y) + (sc21 * sqr(zAxis.x) + sc20 * xAxis.y * zAxis.x) * zAxis.y + quad(zAxis.x) + sc20 * cube(zAxis.x) + sqr(xAxis.x) * sqr(zAxis.x)) / (sqr(zAxis.z) + sc22 * zAxis.z + sqr(zAxis.y) + sc17 + sqr(zAxis.x) + sc18 + sqr(xAxis.z) + sqr(xAxis.y) + sqr(xAxis.x));
            if ((xAxis ^ yAxis) * zAxis < 0) c = -c;

            // z * (-2 * location_z * zAxis_z ^ 2 + (-2 * location_y * zAxis_y - 2 * location_x * zAxis_x) * zAxis_z + 2 * y * zAxis_y * zAxis_z + 2 * x * zAxis_x * zAxis_z + 2 * c * location_z) + z ^ 2 * (zAxis_z ^ 2 - c) + location_z ^ 2 * zAxis_z ^ 2 + y * (-2 * location_z * zAxis_y * zAxis_z - 2 * location_y * zAxis_y ^ 2 + 2 * x * zAxis_x * zAxis_y - 2 * location_x * zAxis_x * zAxis_y + 2 * c * location_y) + x * (-2 * location_z * zAxis_x * zAxis_z - 2 * location_y * zAxis_x * zAxis_y - 2 * location_x * zAxis_x ^ 2 + 2 * c * location_x) + (2 * location_y * location_z * zAxis_y + 2 * location_x * location_z * zAxis_x) * zAxis_z + y ^ 2 * (zAxis_y ^ 2 - c) + location_y ^ 2 * zAxis_y ^ 2 + 2 * location_x * location_y * zAxis_x * zAxis_y + x ^ 2 * (zAxis_x ^ 2 - c) + location_x ^ 2 * zAxis_x ^ 2 - c * location_z ^ 2 - c * location_y ^ 2 - c * location_x ^ 2
            return sqr(zAxis.z * (p.z - location.z) + zAxis.y * (p.y - location.y) + zAxis.x * (p.x - location.x)) - c * (sqr(p.z - location.z) + sqr(p.y - location.y) + sqr(p.x - location.x));
        }
        #region IJsonSerialize
        protected ConicalSurfaceNP() { }
        void IJsonSerialize.GetObjectData(IJsonWriteData data)
        {
            data.AddProperty("Location", location);
            data.AddProperty("XAxis", xAxis);
            data.AddProperty("YAxis", yAxis);
            data.AddProperty("ZAxis", zAxis);
        }
        void IJsonSerialize.SetObjectData(IJsonReadData data)
        {
            location = data.GetProperty<GeoPoint>("Location");
            xAxis = data.GetProperty<GeoVector>("XAxis");
            yAxis = data.GetProperty<GeoVector>("YAxis");
            zAxis = data.GetProperty<GeoVector>("ZAxis");
        }
        #endregion
        #region ISerializable
        protected ConicalSurfaceNP(SerializationInfo info, StreamingContext context)
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
        #endregion
    }

    /*
     */
}
