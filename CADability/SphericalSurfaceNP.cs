using System;
using System.Collections.Generic;
using System.Text;

namespace CADability.GeoObject
{
    public class SphericalSurfaceNP : ISurfaceImpl
    {
        GeoPoint center;
        GeoVector xAxis, yAxis, zAxis;
        ModOp toUnit;
        public SphericalSurfaceNP(GeoPoint center, GeoVector xAxis, GeoVector yAxis, GeoVector zAxis)
        {
            this.center = center;
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.zAxis = zAxis;
        }
        public override ICurve FixedU(double u, double vmin, double vmax)
        {
            Ellipse circle = Ellipse.Construct();
            GeoPoint sp = PointAt(new GeoPoint2D(u, vmin));
            GeoPoint ep = PointAt(new GeoPoint2D(u, vmax));
            GeoPoint pc = center - zAxis; // projection center
            Plane pln = new Plane(pc, sp, ep);
            circle.SetArc3Points(sp, PointAt(new GeoPoint2D(u, (vmax + vmin) / 2.0)), ep, pln);
            return circle;
        }

        public override ICurve FixedV(double v, double umin, double umax)
        {
            Ellipse circle = Ellipse.Construct();
            GeoPoint sp = PointAt(new GeoPoint2D(umin, v));
            GeoPoint ep = PointAt(new GeoPoint2D(umax, v));
            GeoPoint pc = center - zAxis; // projection center
            Plane pln = new Plane(pc, sp, ep);
            circle.SetArc3Points(sp, PointAt(new GeoPoint2D((umax + umin) / 2.0, v)), ep, pln);
            return circle;
        }

        public override ISurface GetModified(ModOp m)
        {
            return new SphericalSurfaceNP(m * center, m * xAxis, m * yAxis, m * zAxis);
        }

        public override GeoPoint PointAt(GeoPoint2D uv)
        {
            double l = Math.Sqrt(uv.x * uv.x + uv.y * uv.y);
            double a = Math.Atan(l);
            if (l == 0.0) l = 1.0; // uv.x and uv.y are 0.0: the terms with "/l" disappear anyhow, no matter of the value of l
            double s = Math.Sin(2 * a);
            double c = Math.Cos(2 * a);
            return new GeoPoint(c * zAxis.x + (uv.y * s * yAxis.x) / l + (uv.x * s * xAxis.x) / l + center.x,
                c * zAxis.y + (uv.y * s * yAxis.y) / l + (uv.x * s * xAxis.y) / l + center.y,
                c * zAxis.z + (uv.y * s * yAxis.z) / l + (uv.x * s * xAxis.z) / l + center.z);
        }

        public override GeoPoint2D PositionOf(GeoPoint p)
        {
            GeoVector pv = Geometry.ReBase(p - center, xAxis, yAxis, zAxis); // vector to the point in unit system
            pv.Length = 1; // map onto the sphere (if not already there)
            Axis beam = new Axis(new GeoPoint(0, 0, -1), new GeoVector(pv.x, pv.y, pv.z + 1)); // beam from south pole to point
            return Plane.XYPlane.Intersect(beam); // intersection of XY plane
        }
        double pow32(double x)
        {
            return Math.Sqrt(x * x * x);
        }
        public override GeoVector UDirection(GeoPoint2D uv)
        {
            if (uv.x == 0) return new GeoVector(0, 0, 1);
            if (uv.y == 0) return new GeoVector(0, 0, 1);
            double uv2 = uv.x * uv.x + uv.y * uv.y;
            double l = Math.Sqrt(uv2);
            double a = Math.Atan(l);
            double s = Math.Sin(2 * a);
            double c = Math.Cos(2 * a);
            double uv232 = pow32(uv2);
            return new GeoVector(
                (-(2 * uv.x * s * zAxis.x) / (l * (uv2 + 1))) - (uv.x * uv.y * s * yAxis.x) / uv232 + (2 * uv.x * uv.y * c * yAxis.x) / (uv2 * (uv2 + 1)) + (s * xAxis.x) / l - (uv.x * uv.x * s * xAxis.x) / uv232 + (2 * uv.x * uv.x * c * xAxis.x) / (uv2 * (uv2 + 1)),
                (-(2 * uv.x * s * zAxis.y) / (l * (uv2 + 1))) - (uv.x * uv.y * s * yAxis.y) / uv232 + (2 * uv.x * uv.y * c * yAxis.y) / (uv2 * (uv2 + 1)) + (s * xAxis.y) / l - (uv.x * uv.x * s * xAxis.y) / uv232 + (2 * uv.x * uv.x * c * xAxis.y) / (uv2 * (uv2 + 1)),
                (-(2 * uv.x * s * zAxis.z) / (l * (uv2 + 1))) - (uv.x * uv.y * s * yAxis.z) / uv232 + (2 * uv.x * uv.y * c * yAxis.z) / (uv2 * (uv2 + 1)) + (s * xAxis.z) / l - (uv.x * uv.x * s * xAxis.z) / uv232 + (2 * uv.x * uv.x * c * xAxis.z) / (uv2 * (uv2 + 1)));
        }

        public override GeoVector VDirection(GeoPoint2D uv)
        {
            if (uv.y == 0) return new GeoVector(0, 0, 1);
            if (uv.x == 0) return new GeoVector(0, 0, 1);
            double uv2 = uv.x * uv.x + uv.y * uv.y;
            double l = Math.Sqrt(uv2);
            double a = Math.Atan(l);
            double s = Math.Sin(2 * a);
            double c = Math.Cos(2 * a);
            double uv232 = pow32(uv2);
            return new GeoVector(
                (-(2 * uv.y * s * zAxis.x) / (l * (uv2 + 1))) + (s * yAxis.x) / l - (uv.y * uv.y * s * yAxis.x) / uv232 + (2 * uv.y * uv.y * c * yAxis.x) / ((uv2) * (uv2 + 1)) - (uv.x * uv.y * s * xAxis.x) / uv232 + (2 * uv.x * uv.y * c * xAxis.x) / ((uv2) * (uv2 + 1)),
                (-(2 * uv.y * s * zAxis.y) / (l * (uv2 + 1))) + (s * yAxis.y) / l - (uv.y * uv.y * s * yAxis.y) / uv232 + (2 * uv.y * uv.y * c * yAxis.y) / ((uv2) * (uv2 + 1)) - (uv.x * uv.y * s * xAxis.y) / uv232 + (2 * uv.x * uv.y * c * xAxis.y) / ((uv2) * (uv2 + 1)),
                (-(2 * uv.y * s * zAxis.z) / (l * (uv2 + 1))) + (s * yAxis.z) / l - (uv.y * uv.y * s * yAxis.z) / uv232 + (2 * uv.y * uv.y * c * yAxis.z) / ((uv2) * (uv2 + 1)) - (uv.x * uv.y * s * xAxis.z) / uv232 + (2 * uv.x * uv.y * c * xAxis.z) / ((uv2) * (uv2 + 1)));
        }
        public override void GetNaturalBounds(out double umin, out double umax, out double vmin, out double vmax)
        {
            umin = -1;
            umax = 1;
            vmin = -1;
            vmax = 1;
        }
    }
}
