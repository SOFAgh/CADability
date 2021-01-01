using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CADability.GeoObject
{
    /// <summary>
    /// A spherical surface with a non periodic u/v system. It also has no poles. It cannot represent a whole sphere, there must be some part
    /// which is outside the usable area. The center, xAxis, yAxis and zAxis define a coordinate system. The plane defined by (center, xAxis and yAxis)
    /// is the equator plane of the sphere, (center+zAxis) is the north pole. (center-zAxis9 is the south pole, which may not be part of the used area.
    /// The u/v system is the u/v system of the equator area. The point of the sphere for a provided u/v point is the projection of this point from the south pole 
    /// to the surface. All circular arcs on the sphere (e.g. planar intersections) are elliptical arcs in the u/v system (or lines, when they pass the north pole)
    /// </summary>
    [Serializable]
    public class SphericalSurfaceNP : ISurfaceImpl, IJsonSerialize, ISerializable
    {
        GeoPoint center;
        GeoVector xAxis, yAxis, zAxis;
        public SphericalSurfaceNP(GeoPoint center, GeoVector xAxis, GeoVector yAxis, GeoVector zAxis)
        {
            this.center = center;
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.zAxis = zAxis;
        }
        /// <summary>
        /// Constructs a non periodic spherical surface with <paramref name="center"/> and <paramref name="radius"/>. The z-axis will be oriented
        /// so that the "south pole" falls into an unused part of the sphere. The edges are <paramref name="orientedCurves"/> bound the sphere
        /// so that when you walk along the curve (<paramref name="outwardOriented"/>==true: on the outside, false: on the inside of the sphere), the
        /// used surfaces is on the left side of the curve. The curves must all be connected and define one or more closed loops on the surface.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="outwardOriented">true: the center is on the inside, false: the center is on the outside of the surface</param>
        /// <param name="orientedCurves"></param>
        public SphericalSurfaceNP(GeoPoint center, double radius, bool outwardOriented, ICurve[] orientedCurves)
        {   // find all intersection points of the curves with a plane through the center
            // the points must be on a circle (because the curves are on the sphere) and alternating enter and leave the used surface.
            // we look for the biggest part outside the surface and use its center as the south pole
            for (int ii = 0; ii < 2; ii++)
            {   // if the first loop doesn't find a solution, run with different points again
                // exactly 3 arcs, which define an octant of the sphere can make a problem
                for (int i = 0; i < orientedCurves.Length; i++)
                {
                    double pos = 0.45;
                    if (ii == 1) pos = 0.55;
                    GeoPoint p = orientedCurves[i].PointAt(pos); // not exactly in the middle to avoid special cases
                    GeoVector dir = orientedCurves[i].DirectionAt(pos);
                    GeoVector xdir = p - center;
                    Plane pln;
                    if (outwardOriented) pln = new Plane(center, xdir, dir ^ xdir);
                    else pln = new Plane(center, xdir, xdir ^ dir);
                    if (ii == 1)
                    {
                        pln.Modify(ModOp.Rotate(center, xdir, SweepAngle.Deg(5))); // slightly rotate to not go exactly through a vertex
                    }
                    double[] pi = orientedCurves[i].GetPlaneIntersection(pln);
                    SortedDictionary<double, bool> positions = new SortedDictionary<double, bool>();
                    bool ok = false;
                    for (int j = 0; j < pi.Length; j++)
                    {
                        GeoPoint2D pi2d = pln.Project(orientedCurves[i].PointAt(pi[j]));
                        double a = Math.Atan2(pi2d.y, pi2d.x);
                        if (Math.Abs(pi[j] - pos) < 1e-6)
                        {
                            ok = true;
                            a = 0.0;
                        }
                        if (positions.ContainsKey(a))
                        {
                            ok = false;
                            break;
                        }
                        else
                        {
                            positions[a] = outwardOriented ^ (pln.ToLocal(orientedCurves[i].DirectionAt(pi[j])).z > 0);
                        }
                    }
                    if (ok)
                    {
                        for (int k = 0; k < orientedCurves.Length; k++)
                        {
                            if (k == i) continue;
                            pi = orientedCurves[k].GetPlaneIntersection(pln);
                            for (int j = 0; j < pi.Length; j++)
                            {
                                GeoPoint2D pi2d = pln.Project(orientedCurves[k].PointAt(pi[j]));
                                double a = Math.Atan2(pi2d.y, pi2d.x);
                                if (a < 0) a += Math.PI * 2.0;
                                if (positions.ContainsKey(a))
                                {
                                    ok = false;
                                    break;
                                }
                                else
                                {
                                    positions[a] = outwardOriented ^ (pln.ToLocal(orientedCurves[k].DirectionAt(pi[j])).z > 0);
                                }
                            }
                            if (!ok) break; // two identical intersection points
                        }
                    }
                    if (ok)
                    {
                        double lastPos = -1;
                        bool lastEnter = false;
                        double sp = 0, d = double.MinValue;
                        foreach (KeyValuePair<double, bool> position in positions)
                        {
                            if (lastPos >= 0)
                            {
                                if (position.Value == lastEnter)
                                {
                                    ok = false; // must be alternating
                                    break;
                                }
                                if (position.Value && position.Key - lastPos > d)
                                {
                                    d = position.Key - lastPos;
                                    sp = lastPos;
                                }
                            }
                            lastPos = position.Key;
                            lastEnter = position.Value;
                        }
                        GeoPoint2D soutPole2d = new GeoPoint2D(radius * Math.Cos(sp + d / 2), radius * Math.Sin(sp + d / 2));
                        if (ok && d > double.MinValue)
                        {
                            this.center = center;
                            zAxis = center - pln.ToGlobal(soutPole2d);
                            zAxis.ArbitraryNormals(out xAxis, out yAxis);
                            if (!outwardOriented) xAxis = -xAxis;
                            xAxis.Length = zAxis.Length;
                            yAxis.Length = zAxis.Length;
                        }
                        else ok = false;
                    }
                    if (ok) return;
                }
            }
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
        public override void Derivation2At(GeoPoint2D uv, out GeoPoint p, out GeoVector du, out GeoVector dv, out GeoVector duu, out GeoVector dvv, out GeoVector duv)
        {   // from maxima and common sub expression
            double u = uv.x;
            double v = uv.y;
            double r = xAxis.Length;
            double sc16 = exp52(sqr(v) + sqr(u));
            double sc15 = exp32(sqr(v) + sqr(u));
            double sc14 = sqr(v) + sqr(u) + sqr(r);
            double sc13 = 2.0 * Math.Atan2(r, Math.Sqrt(sqr(v) + sqr(u)));
            double sc12 = sqr(sc14);
            double sc11 = Math.Sin(sc13);
            double sc10 = Math.Cos(sc13);
            double sc9 = (sqr(v) + sqr(u)) * sc12;
            double sc8 = Math.Sqrt(sqr(v) + sqr(u)) * sc14;
            double sc7 = sc15 * sc14;
            double sc6 = Math.Sqrt(sqr(v) + sqr(u)) * sc12;
            double sc5 = 4.0 * r * sc10;
            double sc4 = 2.0 * r * sc10;
            double sc3 = sc4 * u;
            double sc2 = 4.0 * sqr(r) * sc11;
            GeoVector sc1 = (-(sc4 * zAxis)) / sc8;
            p = center + sc11 * zAxis + v * yAxis / Math.Sqrt(sqr(v) + sqr(u)) + u * xAxis / Math.Sqrt(sqr(v) + sqr(u));
            du = (-(sc3 * zAxis)) / sc8 - u * v * yAxis / sc15 + xAxis / Math.Sqrt(sqr(v) + sqr(u)) - sqr(u) * xAxis / sc15;
            dv = (-(sc4 * v * zAxis)) / sc8 + yAxis / Math.Sqrt(sqr(v) + sqr(u)) - sqr(v) * yAxis / sc15 - u * v * xAxis / sc15;
            duu = sc1 + sc4 * sqr(u) * zAxis / sc7 + sc5 * sqr(u) * zAxis / sc6 - sc2 * sqr(u) * zAxis / sc9 - v * yAxis / sc15 + 3.0 * sqr(u) * v * yAxis / sc16 - 3.0 * u * xAxis / sc15 + 3.0 * cube(u) * xAxis / sc16;
            dvv = sc1 + sc4 * sqr(v) * zAxis / sc7 + sc5 * sqr(v) * zAxis / sc6 - sc2 * sqr(v) * zAxis / sc9 - 3.0 * v * yAxis / sc15 + 3.0 * cube(v) * yAxis / sc16 - u * xAxis / sc15 + 3.0 * u * sqr(v) * xAxis / sc16;
            duv = sc3 * v * zAxis / sc7 + sc5 * u * v * zAxis / sc6 - sc2 * u * v * zAxis / sc9 - u * yAxis / sc15 + 3.0 * u * sqr(v) * yAxis / sc16 - v * xAxis / sc15 + 3.0 * sqr(u) * v * xAxis / sc16;
        }
        public override void GetNaturalBounds(out double umin, out double umax, out double vmin, out double vmax)
        {   // there are no natural bounds, this are the bounds of a half sphere
            umin = -xAxis.Length;
            umax = xAxis.Length;
            vmin = -xAxis.Length;
            vmax = xAxis.Length;
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
        public override ISurface Clone()
        {
            return new SphericalSurfaceNP(center, xAxis, yAxis, zAxis);
        }
        public override bool SameGeometry(BoundingRect thisBounds, ISurface other, BoundingRect otherBounds, double precision, out ModOp2D firstToSecond)
        {
            if (other is SphericalSurfaceNP snp)
            {
                if ((center | snp.center) < precision && Math.Abs(xAxis.Length - snp.xAxis.Length) < precision)
                {
                    firstToSecond = ModOp2D.Null;
                    return true;
                }
                firstToSecond = ModOp2D.Null;
                return false;
            }
            return base.SameGeometry(thisBounds, other, otherBounds, precision, out firstToSecond);
        }
        public override bool UvChangesWithModification => true;
        void IJsonSerialize.GetObjectData(IJsonWriteData data)
        {
            data.AddProperty("Center", center);
            data.AddProperty("XAxis", xAxis);
            data.AddProperty("YAxis", yAxis);
            data.AddProperty("ZAxis", zAxis);
        }
        protected SphericalSurfaceNP() { } // for JSON
        void IJsonSerialize.SetObjectData(IJsonReadData data)
        {
            center = data.GetProperty<GeoPoint>("Center");
            xAxis = data.GetProperty<GeoVector>("XAxis");
            yAxis = data.GetProperty<GeoVector>("YAxis");
            zAxis = data.GetProperty<GeoVector>("ZAxis");
        }
        protected SphericalSurfaceNP(SerializationInfo info, StreamingContext context)
        {
            center = (GeoPoint)info.GetValue("Center", typeof(GeoPoint));
            xAxis = (GeoVector)info.GetValue("XAxis", typeof(GeoVector));
            yAxis = (GeoVector)info.GetValue("YAxis", typeof(GeoVector));
            zAxis = (GeoVector)info.GetValue("ZAxis", typeof(GeoVector));
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Center", center);
            info.AddValue("XAxis", xAxis);
            info.AddValue("YAxis", yAxis);
            info.AddValue("ZAxis", zAxis);
        }
    }
}
