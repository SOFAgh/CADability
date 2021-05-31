using System;
using System.Collections;
using System.Collections.Generic;
using CADability.GeoObject;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CADability.Curve2D;
using MathNet.Numerics.Optimization.TrustRegion;

namespace CADability
{
    static public partial class BoxedSurfaceExtension
    {
        static private double quad(double x) { return x * x * x * x; }
        static private double cube(double x) { return x * x * x; }
        static double sqr(double d) { return d * d; }
        static double exp32(double d) { return Math.Sqrt(d * d * d); }
        /// <summary>
        /// Find the (u,v)-position for the provided point on a surface. Using the NewtonMinimizer from MathNet. Faster than LevenbergMarquardtMinimizer. Surfaces need second derivatives.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="p3d"></param>
        /// <param name="res"></param>
        /// <param name="mindist"></param>
        /// <returns></returns>
        public static bool PositionOfMN(ISurface surface, GeoPoint p3d, ref GeoPoint2D res, out double mindist)
        {
            NewtonMinimizer nm = new NewtonMinimizer(1e-12, 30);
            IObjectiveFunction iof = ObjectiveFunction.GradientHessian(
                new Func<Vector<double>, Tuple<double, Vector<double>, Matrix<double>>>(delegate (Vector<double> vd)
                {
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]);
                    surface.Derivation2At(uv, out GeoPoint loc, out GeoVector du, out GeoVector dv, out GeoVector duu, out GeoVector dvv, out GeoVector duv);
                    double val = (p3d.x - loc.x) * (p3d.x - loc.x) + (p3d.y - loc.y) * (p3d.y - loc.y) + (p3d.z - loc.z) * (p3d.z - loc.z);
                    double u = -2 * du.x * (p3d.x - loc.x) - 2 * du.y * (p3d.y - loc.y) - 2 * du.z * (p3d.z - loc.z);
                    double v = -2 * dv.x * (p3d.x - loc.x) - 2 * dv.y * (p3d.y - loc.y) - 2 * dv.z * (p3d.z - loc.z);
                    Vector<double> gradient = new DenseVector(new double[] { u, v });
                    Matrix<double> hessian = new DenseMatrix(2, 2);
                    hessian[0, 0] = -2 * duu.z * (p3d.z - loc.z) - 2 * duu.y * (p3d.y - loc.y) - 2 * duu.x * (p3d.x - loc.x) + 2 * du.z * du.z + 2 * du.y * du.y + 2 * du.x * du.x;
                    hessian[1, 1] = -2 * dvv.z * (p3d.z - loc.z) - 2 * dvv.y * (p3d.y - loc.y) - 2 * dvv.x * (p3d.x - loc.x) + 2 * dv.z * dv.z + 2 * dv.y * dv.y + 2 * dv.x * dv.x;
                    hessian[0, 1] = hessian[1, 0] = -2 * duv.z * (p3d.z - loc.z) - 2 * duv.y * (p3d.y - loc.y) - 2 * duv.x * (p3d.x - loc.x) + 2 * du.z * dv.z + 2 * du.y * dv.y + 2 * du.x * dv.x;
                    return new Tuple<double, Vector<double>, Matrix<double>>(val, gradient, hessian);
                }));
            try
            {
                MinimizationResult mres = nm.FindMinimum(iof, new DenseVector(new double[] { res.x, res.y }));
                res = new GeoPoint2D(mres.MinimizingPoint[0], mres.MinimizingPoint[1]);
                mindist = mres.FunctionInfoAtMinimum.Value;
                return true;
            }
            catch
            {
                res = GeoPoint2D.Origin;
                mindist = double.MaxValue;
                return false;
            }
        }
        /// <summary>
        /// Find the (u,v)-position for the provided point on a surface. Using the LevenbergMarquardtMinimizer from MathNet. Slower than the NewtonMinimizer but working with first tests.
        /// It is said that it is more robust.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="p3d"></param>
        /// <param name="res"></param>
        /// <param name="mindist"></param>
        /// <returns></returns>
        public static bool PositionOfLM(ISurface surface, GeoPoint p3d, ref GeoPoint2D res, out double mindist)
        {
            Vector<double> observedX = new DenseVector(3); // there is no need to set values, index 0 is for x, 1 for y and 2 for z
            Vector<double> observedY = new DenseVector(new double[] { p3d.x, p3d.y, p3d.z }); // this is the data we want to achieve
            LevenbergMarquardtMinimizer lm = new LevenbergMarquardtMinimizer();
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    GeoPoint p = surface.PointAt(new GeoPoint2D(vd[0], vd[1]));
                    return new DenseVector(new double[] { p.x, p.y, p.z });
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {   // these are the derivations for PointAt(uv)-p3d in x, y and z
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]);
                    surface.DerivationAt(uv, out GeoPoint loc, out GeoVector du, out GeoVector dv);
                    var prime = new DenseMatrix(3, 2);
                    prime[0, 0] = du.x;
                    prime[0, 1] = dv.x;
                    prime[1, 0] = du.y;
                    prime[1, 1] = dv.y;
                    prime[2, 0] = du.z;
                    prime[2, 1] = dv.z;
                    return prime;
                }), observedX, observedY);
            try
            {
                NonlinearMinimizationResult mres = lm.FindMinimum(iom, new DenseVector(new double[] { res.x, res.y }));
                res = new GeoPoint2D(mres.MinimizingPoint[0], mres.MinimizingPoint[1]);
                mindist = (p3d.x - mres.MinimizedValues[0]) * (p3d.x - mres.MinimizedValues[0]) + (p3d.y - mres.MinimizedValues[1]) * (p3d.y - mres.MinimizedValues[1]) + (p3d.z - mres.MinimizedValues[2]) * (p3d.z - mres.MinimizedValues[2]);
                return true;
            }
            catch
            {
                res = GeoPoint2D.Origin;
                mindist = double.MaxValue;
                return false;
            }
        }
        public static void Test()
        {
        }
        public static bool CurveIntersection2D(ICurve2D curve1, double spar1, double epar1, ICurve2D curve2, double spar2, double epar2, ref double par1, ref double par2, ref GeoPoint2D ip)
        {
            NewtonMinimizer nm = new NewtonMinimizer(1e-30, 30);
            IObjectiveFunction iof = ObjectiveFunction.GradientHessian(
                new Func<Vector<double>, Tuple<double, Vector<double>, Matrix<double>>>(delegate (Vector<double> vd)
                {
                    double u = vd[0]; // parameter on first curve
                    double v = vd[1]; // parameter on second curve
                    curve1.TryPointDeriv2At(u, out GeoPoint2D f, out GeoVector2D fu, out GeoVector2D fuu);
                    curve2.TryPointDeriv2At(v, out GeoPoint2D g, out GeoVector2D gv, out GeoVector2D gvv);
                    double val = f & g; // the squared distance between the two points, which newton has to minimize
                    double du = 2 * fu.x * (f.x - g.x) + 2 * fu.y * (f.y - g.y);
                    double dv = 2 * gv.x * (f.x - g.x) + 2 * gv.y * (f.y - g.y);
                    Vector<double> gradient = new DenseVector(new double[] { du, dv });
                    Matrix<double> hessian = new DenseMatrix(2, 2);
                    hessian[0, 0] = 2 * fuu.x * (f.x - g.x) + 2 * fuu.y * (f.y - g.y) + 2 * fu.x * fu.x + 2 * fu.y * fu.y;
                    hessian[1, 1] = 2 * gvv.x * (f.x - g.x) + 2 * gvv.y * (f.y - g.y) + 2 * gv.x * gv.x + 2 * gv.y * gv.y;
                    hessian[0, 1] = hessian[1, 0] = -2 * fu.x * gv.x - 2 * fu.y * gv.y;
                    return new Tuple<double, Vector<double>, Matrix<double>>(val, gradient, hessian);
                }));
            try
            {
                MinimizationResult mres = nm.FindMinimum(iof, new DenseVector(new double[] { (spar1 + epar1) / 2, (spar2 + epar2) / 2 }));
                par1 = mres.MinimizingPoint[0];
                par2 = mres.MinimizingPoint[1];
                ip = new GeoPoint2D(curve1.PointAt(par1), curve2.PointAt(par2));
                // mres.FunctionInfoAtMinimum.Value;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public static bool CurveSurfaceIntersection(ISurface surface, ICurve curve, ref GeoPoint2D uvOnSurface, ref double uOnCurve, out GeoPoint ip)
        {
            NewtonMinimizer nm = new NewtonMinimizer(1e-12, 30, true);
            GeoPoint lastIp = GeoPoint.Origin;
            IObjectiveFunction iof = ObjectiveFunction.GradientHessian(
                new Func<Vector<double>, Tuple<double, Vector<double>, Matrix<double>>>(delegate (Vector<double> vd)
                {
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]); // parameter on surface
                    surface.Derivation2At(uv, out GeoPoint s, out GeoVector su, out GeoVector sv, out GeoVector suu, out GeoVector svv, out GeoVector suv);
                    double t = vd[2]; // parameter on curve
                    curve.TryPointDeriv2At(t, out GeoPoint c, out GeoVector ct, out GeoVector ctt);
                    double val = c & s; // the squared distance between the two points, which newton has to minimize
                    lastIp = new GeoPoint(c, s); // remember last intersection point
                    Vector<double> gradient = new DenseVector(new double[] {
                        2 * (s.x - c.x) * su.x + 2 * (s.y - c.y) * su.y + 2 * (s.z - c.z) * su.z,
                        2 * (s.x - c.x) * sv.x + 2 * (s.y - c.y) * sv.y + 2 * (s.z - c.z) * sv.z,
                        -2 * (s.x - c.x) * ct.x - 2 * (s.y - c.y) * ct.y - 2 * (s.z - c.z) * ct.z
                    });
                    Matrix<double> hessian = new DenseMatrix(3, 3);
                    hessian[0, 0] = 2 * (s.x - c.x) * suu.x + 2 * su.x * su.x + 2 * (s.y - c.y) * suu.y + 2 * su.y * su.y + 2 * (s.z - c.z) * suu.z + 2 * su.z * su.z;
                    hessian[1, 1] = 2 * (s.x - c.x) * svv.x + 2 * sv.x * sv.x + 2 * (s.y - c.y) * svv.y + 2 * sv.y * sv.y + 2 * (s.z - c.z) * svv.z + 2 * sv.z * sv.z;
                    hessian[2, 2] = 2 * ct.x * ct.x - 2 * ctt.x * (s.x - c.x) + 2 * ct.y * ct.y - 2 * ctt.y * (s.y - c.y) + 2 * ct.z * ct.z - 2 * ctt.z * (s.z - c.z);
                    hessian[0, 1] = hessian[1, 0] = 2 * su.x * sv.x + 2 * (s.x - c.x) * suv.x + 2 * su.y * sv.y + 2 * (s.y - c.y) * suv.y + 2 * su.z * sv.z + 2 * (s.z - c.z) * suv.z;
                    hessian[0, 2] = hessian[2, 0] = -2 * ct.x * su.x - 2 * ct.y * su.y - 2 * ct.z * su.z;
                    hessian[1, 2] = hessian[2, 1] = -2 * ct.x * sv.x - 2 * ct.y * sv.y - 2 * ct.z * sv.z;
                    return new Tuple<double, Vector<double>, Matrix<double>>(val, gradient, hessian);
                }));
            try
            {
                MinimizationResult mres = nm.FindMinimum(iof, new DenseVector(new double[] { uvOnSurface.x, uvOnSurface.y, uOnCurve }));
                uvOnSurface = new GeoPoint2D(mres.MinimizingPoint[0], mres.MinimizingPoint[1]);
                uOnCurve = mres.MinimizingPoint[2];
                ip = lastIp;
                return true;
            }
            catch (Exception e)
            {
                ip = GeoPoint.Origin;
                return false;
            }
        }

        public static bool CurveSurfaceIntersection(ISurface surface, ICurve curve, BoundingRect surfaceBounds, double uOnCurveMin, double uOnCurveMax, ref GeoPoint2D uvOnSurface, ref double uOnCurve, out GeoPoint ip)
        {
            Vector<double> observedX = new DenseVector(3); // there is no need to set values, index 0 is for x, 1 for y and 2 for z
            Vector<double> observedY = new DenseVector(new double[] { 0, 0, 0 }); // this is the data we want to achieve
            GeoPoint lastip = GeoPoint.Origin;
            TrustRegionNewtonCGMinimizer trm = new TrustRegionNewtonCGMinimizer();
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]); // parameter on surface
                    double t = vd[2]; // parameter on curve
                    GeoPoint c = curve.PointAt(t);
                    GeoPoint p = surface.PointAt(new GeoPoint2D(vd[0], vd[1]));
                    lastip = new GeoPoint(c, p);
                    return new DenseVector(new double[] { p.x - c.x, p.y - c.y, p.z - c.z });
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {   // these are the derivations for PointAt(uv)-p3d in x, y and z
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]);
                    surface.DerivationAt(uv, out GeoPoint loc, out GeoVector du, out GeoVector dv);
                    double t = vd[2]; // parameter on curve
                    GeoVector dt = curve.DirectionAt(t);
                    var prime = new DenseMatrix(3, 3);
                    prime[0, 0] = du.x;
                    prime[0, 1] = dv.x;
                    prime[0, 2] = -dt.x;
                    prime[1, 0] = du.y;
                    prime[1, 1] = dv.y;
                    prime[1, 2] = -dt.y;
                    prime[2, 0] = du.z;
                    prime[2, 1] = dv.z;
                    prime[2, 2] = -dt.z;
                    return prime;
                }), observedX, observedY);
            try
            {
                NonlinearMinimizationResult mres = trm.FindMinimum(iom, new double[] { uvOnSurface.x, uvOnSurface.y, uOnCurve }, new double[] { surfaceBounds.Left, surfaceBounds.Bottom, uOnCurveMin }, new double[] { surfaceBounds.Right, surfaceBounds.Top, uOnCurveMax });
                if (mres.ReasonForExit == ExitCondition.Converged)
                {
                    uvOnSurface = new GeoPoint2D(mres.MinimizingPoint[0], mres.MinimizingPoint[1]);
                    uOnCurve = mres.MinimizingPoint[2];
                    ip = lastip;
                    return true;
                }
                else
                {
                    ip = lastip;
                    return false;
                }
            }
            catch
            {
                ip = lastip;
                return false;
            }
        }
        public static bool CurveSurfaceIntersectionDL(ISurface surface, ICurve curve, BoundingRect surfaceBounds, double uOnCurveMin, double uOnCurveMax, ref GeoPoint2D uvOnSurface, ref double uOnCurve, out GeoPoint ip)
        {
            Vector<double> observedX = new DenseVector(3); // there is no need to set values, index 0 is for x, 1 for y and 2 for z
            Vector<double> observedY = new DenseVector(new double[] { 0, 0, 0 }); // this is the data we want to achieve
            GeoPoint lastip = GeoPoint.Origin;
            TrustRegionDogLegMinimizer trm = new TrustRegionDogLegMinimizer();
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]); // parameter on surface
                    double t = vd[2]; // parameter on curve
                    GeoPoint c = curve.PointAt(t);
                    GeoPoint p = surface.PointAt(new GeoPoint2D(vd[0], vd[1]));
                    lastip = new GeoPoint(c, p);
                    return new DenseVector(new double[] { p.x - c.x, p.y - c.y, p.z - c.z });
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {   // these are the derivations for PointAt(uv)-p3d in x, y and z
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]);
                    surface.DerivationAt(uv, out GeoPoint loc, out GeoVector du, out GeoVector dv);
                    double t = vd[2]; // parameter on curve
                    GeoVector dt = curve.DirectionAt(t);
                    var prime = new DenseMatrix(3, 3);
                    prime[0, 0] = du.x;
                    prime[0, 1] = dv.x;
                    prime[0, 2] = -dt.x;
                    prime[1, 0] = du.y;
                    prime[1, 1] = dv.y;
                    prime[1, 2] = -dt.y;
                    prime[2, 0] = du.z;
                    prime[2, 1] = dv.z;
                    prime[2, 2] = -dt.z;
                    return prime;
                }), observedX, observedY);
            try
            {
                NonlinearMinimizationResult mres = trm.FindMinimum(iom, new double[] { uvOnSurface.x, uvOnSurface.y, uOnCurve }, new double[] { surfaceBounds.Left, surfaceBounds.Bottom, uOnCurveMin }, new double[] { surfaceBounds.Right, surfaceBounds.Top, uOnCurveMax });
                if (mres.ReasonForExit == ExitCondition.Converged)
                {
                    uvOnSurface = new GeoPoint2D(mres.MinimizingPoint[0], mres.MinimizingPoint[1]);
                    uOnCurve = mres.MinimizingPoint[2];
                    ip = lastip;
                    return true;
                }
                else
                {
                    ip = lastip;
                    return false;
                }
            }
            catch
            {
                ip = lastip;
                return false;
            }
        }

        public static bool CurveSurfaceIntersectionLM(ISurface surface, ICurve curve, ref GeoPoint2D uvOnSurface, ref double uOnCurve, out GeoPoint ip)
        {
            Vector<double> observedX = new DenseVector(3); // there is no need to set values, index 0 is for x, 1 for y and 2 for z
            Vector<double> observedY = new DenseVector(new double[] { 0, 0, 0 }); // this is the data we want to achieve
            GeoPoint lastip = GeoPoint.Origin;
            LevenbergMarquardtMinimizer lm = new LevenbergMarquardtMinimizer(maximumIterations: 10);
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]); // parameter on surface
                    double t = vd[2]; // parameter on curve
                    GeoPoint c = curve.PointAt(t);
                    GeoPoint p = surface.PointAt(new GeoPoint2D(vd[0], vd[1]));
                    lastip = new GeoPoint(c, p);
                    return new DenseVector(new double[] { p.x - c.x, p.y - c.y, p.z - c.z });
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {   // these are the derivations for PointAt(uv)-p3d in x, y and z
                    GeoPoint2D uv = new GeoPoint2D(vd[0], vd[1]);
                    surface.DerivationAt(uv, out GeoPoint loc, out GeoVector du, out GeoVector dv);
                    double t = vd[2]; // parameter on curve
                    GeoVector dt = curve.DirectionAt(t);
                    var prime = new DenseMatrix(3, 3);
                    prime[0, 0] = du.x;
                    prime[0, 1] = dv.x;
                    prime[0, 2] = -dt.x;
                    prime[1, 0] = du.y;
                    prime[1, 1] = dv.y;
                    prime[1, 2] = -dt.y;
                    prime[2, 0] = du.z;
                    prime[2, 1] = dv.z;
                    prime[2, 2] = -dt.z;
                    return prime;
                }), observedX, observedY);
            try
            {
                NonlinearMinimizationResult mres = lm.FindMinimum(iom, new DenseVector(new double[] { uvOnSurface.x, uvOnSurface.y, uOnCurve }));
                uvOnSurface = new GeoPoint2D(mres.MinimizingPoint[0], mres.MinimizingPoint[1]);
                uOnCurve = mres.MinimizingPoint[2];
                ip = lastip;
                return true;
            }
            catch
            {
                ip = lastip;
                return false;
            }
        }

        public static bool SurfacesIntersectionLM(ISurface surface1, ISurface surface2, ISurface surface3, ref GeoPoint2D uv1, ref GeoPoint2D uv2, ref GeoPoint2D uv3, ref GeoPoint ip)
        {
            //Polynom implicitSurface1 = null, implicitSurface2 = null, implicitSurface3 = null;
            //if (surface1 is ISurfaceImpl si1) implicitSurface1 = si1.GetImplicitPolynomial();
            //if (surface2 is ISurfaceImpl si2) implicitSurface2 = si2.GetImplicitPolynomial();
            //if (surface3 is ISurfaceImpl si3) implicitSurface3 = si3.GetImplicitPolynomial();
            //List<ISurface> notimpl = new List<ISurface>();
            //notimpl.Add(surface1); notimpl.Add(surface2); notimpl.Add(surface3);
            //List<Polynom> polynoms = new List<Polynom>();
            //List<GeoPoint2D> luv = new List<GeoPoint2D>(new GeoPoint2D[] { uv1, uv2, uv3 });
            //if (implicitSurface3 != null)
            //{
            //    notimpl.RemoveAt(2);
            //    luv.RemoveAt(2);
            //    polynoms.Add(implicitSurface3);
            //}
            //if (implicitSurface2 != null)
            //{
            //    notimpl.RemoveAt(1);
            //    luv.RemoveAt(1);
            //    polynoms.Add(implicitSurface2);
            //}
            //if (implicitSurface1 != null)
            //{
            //    notimpl.RemoveAt(0);
            //    luv.RemoveAt(0);
            //    polynoms.Add(implicitSurface1);
            //}
            //if (polynoms.Count == 3)
            //{
            //    if (SurfacesIntersectionLM(polynoms[0], polynoms[1], polynoms[2], ref ip))
            //    {
            //        uv1 = surface1.PositionOf(ip);
            //        uv2 = surface2.PositionOf(ip);
            //        uv3 = surface3.PositionOf(ip);
            //        return true;
            //    }
            //}
            Vector<double> observedX = new DenseVector(9); // there is no need to set values
            Vector<double> observedY = new DenseVector(new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }); // this is the data we want to achieve
            GeoPoint lastip = GeoPoint.Origin;
            LevenbergMarquardtMinimizer lm = new LevenbergMarquardtMinimizer(maximumIterations: 10);
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    GeoPoint p1 = surface1.PointAt(new GeoPoint2D(vd[0], vd[1]));
                    GeoPoint p2 = surface2.PointAt(new GeoPoint2D(vd[2], vd[3]));
                    GeoPoint p3 = surface3.PointAt(new GeoPoint2D(vd[4], vd[5]));
                    return new DenseVector(new double[] { p1.x - p2.x, p1.y - p2.y, p1.z - p2.z, p2.x - p3.x, p2.y - p3.y, p2.z - p3.z, p3.x - p1.x, p3.y - p1.y, p3.z - p1.z });
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {   // these are the derivations for p1-p2, p2-3 and p3-p1 in x, y and z
                    surface1.DerivationAt(new GeoPoint2D(vd[0], vd[1]), out GeoPoint loc1, out GeoVector du1, out GeoVector dv1);
                    surface2.DerivationAt(new GeoPoint2D(vd[2], vd[3]), out GeoPoint loc2, out GeoVector du2, out GeoVector dv2);
                    surface3.DerivationAt(new GeoPoint2D(vd[4], vd[5]), out GeoPoint loc3, out GeoVector du3, out GeoVector dv3);
                    var prime = new DenseMatrix(9, 6);
                    prime[0, 0] = du1.x;
                    prime[0, 1] = dv1.x;
                    prime[0, 2] = -du2.x;
                    prime[0, 3] = -dv2.x;
                    prime[0, 4] = 0;
                    prime[0, 5] = 0;
                    prime[1, 0] = du1.y;
                    prime[1, 1] = dv1.y;
                    prime[1, 2] = -du2.y;
                    prime[1, 3] = -dv2.y;
                    prime[1, 4] = 0;
                    prime[1, 5] = 0;
                    prime[2, 0] = du1.z;
                    prime[2, 1] = dv1.z;
                    prime[2, 2] = -du2.z;
                    prime[2, 3] = -dv2.z;
                    prime[2, 4] = 0;
                    prime[2, 5] = 0;
                    prime[3, 0] = 0;
                    prime[3, 1] = 0;
                    prime[3, 2] = du2.x;
                    prime[3, 3] = dv2.x;
                    prime[3, 4] = -du3.x;
                    prime[3, 5] = -dv3.x;
                    prime[4, 0] = 0;
                    prime[4, 1] = 0;
                    prime[4, 2] = du2.y;
                    prime[4, 3] = dv2.y;
                    prime[4, 4] = -du3.y;
                    prime[4, 5] = -dv3.y;
                    prime[5, 0] = 0;
                    prime[5, 1] = 0;
                    prime[5, 2] = du2.z;
                    prime[5, 3] = dv2.z;
                    prime[5, 4] = -du3.z;
                    prime[5, 5] = -dv3.z;
                    prime[6, 0] = -du1.x;
                    prime[6, 1] = -dv1.x;
                    prime[6, 2] = 0;
                    prime[6, 3] = 0;
                    prime[6, 4] = du3.x;
                    prime[6, 5] = dv3.x;
                    prime[7, 0] = -du1.y;
                    prime[7, 1] = -dv1.y;
                    prime[7, 2] = 0;
                    prime[7, 3] = 0;
                    prime[7, 4] = du3.y;
                    prime[7, 5] = dv3.y;
                    prime[8, 0] = -du1.z;
                    prime[8, 1] = -dv1.z;
                    prime[8, 2] = 0;
                    prime[8, 3] = 0;
                    prime[8, 4] = du3.z;
                    prime[8, 5] = dv3.z;
                    return prime;
                }), observedX, observedY);
            try
            {
                NonlinearMinimizationResult mres = lm.FindMinimum(iom, new DenseVector(new double[] { uv1.x, uv1.y, uv2.x, uv2.y, uv3.x, uv3.y }));
                uv1 = new GeoPoint2D(mres.MinimizingPoint[0], mres.MinimizingPoint[1]);
                uv2 = new GeoPoint2D(mres.MinimizingPoint[2], mres.MinimizingPoint[3]);
                uv3 = new GeoPoint2D(mres.MinimizingPoint[4], mres.MinimizingPoint[5]);
                ip = new GeoPoint(surface1.PointAt(uv1), surface2.PointAt(uv2), surface3.PointAt(uv3));
                return true;
            }
            catch
            {
                ip = GeoPoint.Origin;
                return false;
            }
        }
        public static bool SurfacesIntersectionLM(Polynom surface1, Polynom surface2, Polynom surface3, ref GeoPoint ip)
        {
            Vector<double> observedX = new DenseVector(3); // there is no need to set values
            Vector<double> observedY = new DenseVector(new double[] { 0, 0, 0 }); // this is the data we want to achieve
            Polynom[,] derivatives = new Polynom[3, 3];
            derivatives[0, 0] = surface1.Derivate(1, 0, 0);
            derivatives[0, 1] = surface1.Derivate(0, 1, 0);
            derivatives[0, 2] = surface1.Derivate(0, 0, 1);
            derivatives[1, 0] = surface2.Derivate(1, 0, 0);
            derivatives[1, 1] = surface2.Derivate(0, 1, 0);
            derivatives[1, 2] = surface2.Derivate(0, 0, 1);
            derivatives[2, 0] = surface3.Derivate(1, 0, 0);
            derivatives[2, 1] = surface3.Derivate(0, 1, 0);
            derivatives[2, 2] = surface3.Derivate(0, 0, 1);
            LevenbergMarquardtMinimizer lm = new LevenbergMarquardtMinimizer(maximumIterations: 10);
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    double d1 = surface1.Eval(vd[0], vd[1], vd[2]);
                    double d2 = surface2.Eval(vd[0], vd[1], vd[2]);
                    double d3 = surface3.Eval(vd[0], vd[1], vd[2]);
                    return new DenseVector(new double[] { d1, d2, d3 });
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {
                    var prime = new DenseMatrix(3, 3);
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            prime[i, j] = derivatives[i, j].Eval(vd[0], vd[1], vd[2]);
                        }
                    }
                    return prime;
                }), observedX, observedY);
            try
            {
                NonlinearMinimizationResult mres = lm.FindMinimum(iom, new DenseVector(new double[] { ip.x, ip.y, ip.z }));
                ip = new GeoPoint(mres.MinimizingPoint[0], mres.MinimizingPoint[1], mres.MinimizingPoint[2]);
                return true;
            }
            catch
            {
                ip = GeoPoint.Origin;
                return false;
            }
        }
        /// <summary>
        /// Fit a plane through a number of points. This is working, but has a bad performance. Better use Plane.FromPoints
        /// </summary>
        /// <param name="points"></param>
        /// <param name="location"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static bool PlaneFit(IEnumerable<GeoPoint> points, out GeoPoint location, out GeoVector normal)
        {
            GeoPoint[] pnts = null;
            if (points is GeoPoint[]) pnts = (GeoPoint[])(points as GeoPoint[]).Clone();
            else
            {
                List<GeoPoint> lpnts = null;
                if (points is List<GeoPoint>) lpnts = points as List<GeoPoint>;
                else
                {
                    lpnts = new List<GeoPoint>();
                    foreach (GeoPoint point in points)
                    {
                        lpnts.Add(point);
                    }
                }
                pnts = lpnts.ToArray(); // GeoPoint is a struct, this hopefully clones the GeoPoints
            }
            Vector<double> observedX = new DenseVector(pnts.Length); // there is no need to set values
            Vector<double> observedY = new DenseVector(pnts.Length); // this is the data we want to achieve, namely 0.0
            LevenbergMarquardtMinimizer lm = new LevenbergMarquardtMinimizer(gradientTolerance: 1e-8, maximumIterations: 20);
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    DenseVector res = new DenseVector(pnts.Length);
                    for (int i = 0; i < pnts.Length; i++)
                    {
                        res[i] = (vd[0] * pnts[i].x + vd[1] * pnts[i].y + vd[2] * pnts[i].z) / Math.Sqrt(vd[0] * vd[0] + vd[1] * vd[1] + vd[2] * vd[2]) - Math.Sqrt(vd[0] * vd[0] + vd[1] * vd[1] + vd[2] * vd[2]);
                    }
#if DEBUG
                    double err = 0.0;
                    for (int i = 0; i < pnts.Length; i++) err += res[i] * res[i];
#endif
                    return res;
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {
                    var prime = new DenseMatrix(pnts.Length, 3);
                    double l = vd[0] * vd[0] + vd[1] * vd[1] + vd[2] * vd[2];
                    for (int i = 0; i < pnts.Length; i++)
                    {
                        prime[i, 0] = -vd[0] * (vd[0] * pnts[i].x + vd[1] * pnts[i].y + vd[2] * pnts[i].z) / Math.Sqrt(l * l * l) + (pnts[i].x - vd[0]) / Math.Sqrt(l);
                        prime[i, 1] = -vd[1] * (vd[0] * pnts[i].x + vd[1] * pnts[i].y + vd[2] * pnts[i].z) / Math.Sqrt(l * l * l) + (pnts[i].y - vd[1]) / Math.Sqrt(l);
                        prime[i, 2] = -vd[2] * (vd[0] * pnts[i].x + vd[1] * pnts[i].y + vd[2] * pnts[i].z) / Math.Sqrt(l * l * l) + (pnts[i].z - vd[2]) / Math.Sqrt(l);
                    }
                    return prime;
                }), observedX, observedY);

            try
            {
                DenseVector sparams = new DenseVector(3);
                GeoPoint org = GeoPoint.Origin;
                GeoVector norm = GeoVector.NullVector;
                bool found = false;
                for (int i = 0; i < pnts.Length; i++)
                {
                    for (int j = i + 1; j < pnts.Length; j++)
                    {
                        for (int k = j + 1; k < pnts.Length; k++)
                        {
                            GeoVector dirx = pnts[j] - pnts[i];
                            GeoVector diry = pnts[k] - pnts[i];
                            GeoVector n = dirx ^ diry;
                            if (!n.IsNullVector())
                            {
                                Plane pln = new Plane(pnts[i], dirx, diry);
                                double d = pln.Distance(GeoPoint.Origin);
                                norm = -d * n.Normalized;
                                sparams[0] = norm.x;
                                sparams[1] = norm.y;
                                sparams[2] = norm.z;
                                org = pnts[i];
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                    if (found) break;
                }
                GeoVector offset = new GeoVector(norm.x - org.x, norm.y - org.y, norm.z - org.z);
                for (int i = 0; i < pnts.Length; i++) pnts[i] += offset;
                NonlinearMinimizationResult mres = lm.FindMinimum(iom, new DenseVector(new double[] { norm.x, norm.y, norm.z }));
                if (mres.ReasonForExit == ExitCondition.Converged || mres.ReasonForExit == ExitCondition.RelativeGradient)
                {
                    normal = new GeoVector(mres.MinimizingPoint[0], mres.MinimizingPoint[1], mres.MinimizingPoint[2]);
                    location = GeoPoint.Origin + normal - offset;
                    Plane dbg = new Plane(location, normal);
                    double error = 0.0;
                    foreach (GeoPoint point in points) error += dbg.Distance(point);

                    return true;
                }
            }
            catch (Exception ex)
            {
            }
            normal = GeoVector.NullVector;
            location = GeoPoint.Origin;
            return false;
        }
        public static double LineFit(IEnumerable<GeoPoint> points, double precision, out GeoPoint location, out GeoVector direction)
        {
            GeoPoint[] pnts = null;
            if (points is GeoPoint[]) pnts = (GeoPoint[])(points as GeoPoint[]).Clone();
            else
            {
                List<GeoPoint> lpnts = null;
                if (points is List<GeoPoint>) lpnts = points as List<GeoPoint>;
                else
                {
                    lpnts = new List<GeoPoint>();
                    foreach (GeoPoint point in points)
                    {
                        lpnts.Add(point);
                    }
                }
                pnts = lpnts.ToArray(); // GeoPoint is a struct, this hopefully clones the GeoPoints
            }
            Vector<double> observedX = new DenseVector(pnts.Length); // there is no need to set values
            Vector<double> observedY = new DenseVector(pnts.Length); // this is the data we want to achieve, namely 0.0
            LevenbergMarquardtMinimizer lm = new LevenbergMarquardtMinimizer(gradientTolerance: 1e-15, maximumIterations: 20);
            IObjectiveModel iom = ObjectiveFunction.NonlinearModel(
                new Func<Vector<double>, Vector<double>, Vector<double>>(delegate (Vector<double> vd, Vector<double> ox) // function
                {
                    //// v = d ^ (l - p);
                    //// v.Length/d.Length is the distance of point p to the line (l,d)
                    //// sqrt((dy*(lz-pz)-dz*(ly-py))² + (...)² + (...)²) / sqrt(dx*dx+dy*dy+dz*dz) the distance
                    //// error: dy*(lz-pz)-dz*(ly-py))² + (...)² + (...)² / (dx*dx+dy*dy+dz*dz) the sqaure of the distance

                    DenseVector res = new DenseVector(pnts.Length);
                    double lx = vd[0];
                    double ly = vd[1];
                    double lz = vd[2];
                    double dx = vd[3];
                    double dy = vd[4];
                    double dz = vd[5];
                    double dl = dx * dx + dy * dy + dz * dz;
                    for (int i = 0; i < pnts.Length; i++)
                    {
                        double px = pnts[i].x;
                        double py = pnts[i].y;
                        double pz = pnts[i].z;
                        res[i] = (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px))) / (dl);
                    }
#if DEBUG
                    double err = 0.0;
                    for (int i = 0; i < pnts.Length; i++) err += res[i] * res[i];
#endif
                    return res;
                }),
                new Func<Vector<double>, Vector<double>, Matrix<double>>(delegate (Vector<double> vd, Vector<double> ox) // derivatives
                {
                    //// derivations of the (square of the) distance by lx,ly,lz,dx,dy,dz
                    ////((-2 * dz * (dx * (lz - pz) - dz * (lx - px))) - 2 * dy * (dx * (ly - py) - dy * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2);
                    ////(2 * dx * (dx * (ly - py) - dy * (lx - px)) - 2 * dz * (dy * (lz - pz) - dz * (ly - py))) / (dz ^ 2 + dy ^ 2 + dx ^ 2);
                    ////(2 * dy * (dy * (lz - pz) - dz * (ly - py)) + 2 * dx * (dx * (lz - pz) - dz * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2);
                    ////(2 * (dx * (lz - pz) - dz * (lx - px)) * (lz - pz) + 2 * (dx * (ly - py) - dy * (lx - px)) * (ly - py)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) - (2 * dx * ((dy * (lz - pz) - dz * (ly - py)) ^ 2 + (dx * (lz - pz) - dz * (lx - px)) ^ 2 + (dx * (ly - py) - dy * (lx - px)) ^ 2)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) ^ 2;
                    ////(2 * (dy * (lz - pz) - dz * (ly - py)) * (lz - pz) + 2 * (px - lx) * (dx * (ly - py) - dy * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2) - (2 * dy * ((dy * (lz - pz) - dz * (ly - py)) ^ 2 + (dx * (lz - pz) - dz * (lx - px)) ^ 2 + (dx * (ly - py) - dy * (lx - px)) ^ 2)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) ^ 2;
                    ////(2 * (py - ly) * (dy * (lz - pz) - dz * (ly - py)) + 2 * (px - lx) * (dx * (lz - pz) - dz * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2) - (2 * dz * ((dy * (lz - pz) - dz * (ly - py)) ^ 2 + (dx * (lz - pz) - dz * (lx - px)) ^ 2 + (dx * (ly - py) - dy * (lx - px)) ^ 2)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) ^ 2;

                    var prime = new DenseMatrix(pnts.Length, 6);
                    double lx = vd[0];
                    double ly = vd[1];
                    double lz = vd[2];
                    double dx = vd[3];
                    double dy = vd[4];
                    double dz = vd[5];
                    double dl = dx * dx + dy * dy + dz * dz;

                    for (int i = 0; i < pnts.Length; i++)
                    {
                        double px = pnts[i].x;
                        double py = pnts[i].y;
                        double pz = pnts[i].z;
                        prime[i, 0] = ((-2 * dz * (dx * (lz - pz) - dz * (lx - px))) - 2 * dy * (dx * (ly - py) - dy * (lx - px))) / dl;
                        prime[i, 1] = (2 * dx * (dx * (ly - py) - dy * (lx - px)) - 2 * dz * (dy * (lz - pz) - dz * (ly - py))) / dl;
                        prime[i, 2] = (2 * dy * (dy * (lz - pz) - dz * (ly - py)) + 2 * dx * (dx * (lz - pz) - dz * (lx - px))) / dl;
                        prime[i, 3] = (2 * (dx * (lz - pz) - dz * (lx - px)) * (lz - pz) + 2 * (dx * (ly - py) - dy * (lx - px)) * (ly - py)) / (dl) - (2 * dx * (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px)))) / sqr(dl);
                        prime[i, 4] = (2 * (dy * (lz - pz) - dz * (ly - py)) * (lz - pz) + 2 * (px - lx) * (dx * (ly - py) - dy * (lx - px))) / (dl) - (2 * dy * (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px)))) / sqr(dl);
                        prime[i, 5] = (2 * (py - ly) * (dy * (lz - pz) - dz * (ly - py)) + 2 * (px - lx) * (dx * (lz - pz) - dz * (lx - px))) / (dl) - (2 * dz * (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px)))) / sqr(dl);
                    }
                    return prime;
                }), observedX, observedY);
            GeoVector d = pnts[1] - pnts[0];
            double tl = d.TaxicabLength;
            for (int i = 2; i < pnts.Length; i++)
            {
                GeoVector di = pnts[i] - pnts[0];
                double li = di.TaxicabLength;
                if (li > tl)
                {
                    tl = li;
                    d = di;
                }
            }
            NonlinearMinimizationResult mres = lm.FindMinimum(iom, new DenseVector(new double[] { pnts[0].x, pnts[0].y, pnts[0].z,d.x,d.y,d.z }));
            if (mres.ReasonForExit == ExitCondition.Converged || mres.ReasonForExit == ExitCondition.RelativeGradient)
            {
                location = new GeoPoint(mres.MinimizingPoint[0], mres.MinimizingPoint[1], mres.MinimizingPoint[2]);
                direction = new GeoVector(mres.MinimizingPoint[3], mres.MinimizingPoint[4], mres.MinimizingPoint[5]);
                double error = 0.0;
                foreach (GeoPoint point in points) error += Geometry.DistPL(point, location, direction);

                return error;
            }
            location = GeoPoint.Origin;
            direction = GeoVector.NullVector;
            return double.MaxValue;

        }
    }
}