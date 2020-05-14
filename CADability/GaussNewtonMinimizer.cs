using CADability.Curve2D;
using CADability.GeoObject;
using CADability.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace CADability
{
    public static class MyExtensions
    {
        public static double[] Add(this double[] a, double[] b)
        {
            double[] res = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                res[i] = a[i] + b[i];
            }
            return res;
        }
        public static IArray<T> Row<T>(this T[,] a, int ind)
        {
            return new ArrayRow<T>(a, ind);
        }
        public static IArray<T> Column<T>(this T[,] a, int ind)
        {
            return new ArrayColumn<T>(a, ind);
        }
        public static IArray<T> Linear<T>(this T[,] a)
        {
            return new ArrayLinear<T>(a);
        }
        public static IArray<T> ToIArray<T>(this T[] a)
        {
            return new ToIArray<T>(a);
        }
    }
    public class GaussNewtonMinimizer
    {
        public delegate void ErrorFunction(double[] parameters, out double[] errorValues);
        public delegate void JacobiFunction(double[] parameters, out Matrix partialDerivations);

        private ErrorFunction errorFunction;
        private JacobiFunction jacobiFunction;
        private Matrix Jacobi;
        private Matrix JacobiTJacobi;
        private double[] NegJacobiTError;
        private double[] Error;

        public GaussNewtonMinimizer(ErrorFunction eFunction, JacobiFunction jFunction)
        {
            this.errorFunction = eFunction;
            this.jacobiFunction = jFunction;
        }

        private double dot(double[] a, double[] b)
        {
            if (a.Length != b.Length) throw new ApplicationException("GaussNewtonMinimizer.dot: parameters must have same length");
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }
            return sum;
        }
        private void UnaryMinus(double[] d)
        {
            for (int i = 0; i < d.Length; i++)
            {
                d[i] = -d[i];
            }
        }
        private void ComputeLinearSystemInputs(double[] pCurrent)
        {
            jacobiFunction(pCurrent, out Jacobi);
            JacobiTJacobi = Matrix.Transpose(Jacobi) * Jacobi;
            NegJacobiTError = (Error * Jacobi).Row(0);
            UnaryMinus(NegJacobiTError);

            //double diagsum = 0.0;
            //for (int i = 0; i < mNegJTF.Length; i++)
            //{
            //    diagsum += mJTJ[i, i];
            //}
            //double adj = 0.1 * diagsum / mNegJTF.Length;
            //for (int i = 0; i < mNegJTF.Length; i++)
            //{
            //    mJTJ[i, i]+=adj;
            //}
        }

        /// <summary>
        /// Solves the Gauss Newton approximation with the given <paramref name="startParameters"/> and the error function and Jacobi matrix as specified in the constructor.
        /// Returns the result in <paramref name="parameters"/> and returns true, if the error function of the result is less than <paramref name="errorTolerance"/>. 
        /// The iteration doesn't stop when <paramref name="errorTolerance"/> is reached, but goes on until no more improvement of the result ocurres.
        /// </summary>
        /// <param name="startParameters">start parameters</param>
        /// <param name="maxIterations">maximum number of iterations</param>
        /// <param name="updateLengthTolerance">not used</param>
        /// <param name="errorTolerance">maximum error for positiv result</param>
        /// <param name="minError">effective error</param>
        /// <param name="numIterations">number of iterations used</param>
        /// <param name="parameters">resulting parameters</param>
        /// <returns></returns>
        public bool Solve(double[] startParameters, int maxIterations, double updateLengthTolerance, double errorTolerance, out double minError, out int numIterations, out double[] parameters)
        {
            double[] minLocation = startParameters.Clone() as double[];
            minError = double.MaxValue;
            double minErrorDifference = double.MaxValue;
            double minUpdateLength = 0.0;
            numIterations = 0;
            errorFunction(startParameters, out Error);
            minError = dot(Error, Error);
            double[] pCurrent = startParameters.Clone() as double[];
            for (numIterations = 1; numIterations < maxIterations; numIterations++)
            {
                ComputeLinearSystemInputs(pCurrent);
                double[] pNext;
                //CholeskyDecomposition cd = new CholeskyDecomposition(JacobiTJacobi); // there must be something wrong with Cholesky!
                //Matrix solved1 = cd.Solve(new Matrix(NegJacobiTError, true));
#if MATHNET
                MathNet.Numerics.LinearAlgebra.Matrix<double> m = new MathNet.Numerics.LinearAlgebra.Double.DenseMatrix(JacobiTJacobi.RowCount, JacobiTJacobi.ColumnCount);
                for (int i = 0; i < JacobiTJacobi.RowCount; i++)
                {
                    for (int j = 0; j < JacobiTJacobi.ColumnCount; j++)
                    {
                        m[i, j] = JacobiTJacobi[i, j];
                    }
                }
                MathNet.Numerics.LinearAlgebra.Matrix<double> b = new MathNet.Numerics.LinearAlgebra.Double.DenseMatrix(NegJacobiTError.Length, 1);
                for (int i = 0; i < NegJacobiTError.Length; i++)
                {
                    b[i, 0] = NegJacobiTError[i];
                }
                try
                {
                    MathNet.Numerics.LinearAlgebra.Vector<double> s = m.Cholesky().Solve(new MathNet.Numerics.LinearAlgebra.Double.DenseVector(NegJacobiTError));
                    pNext = pCurrent.Add(s.ToArray());
                }
                catch (System.ArgumentException)
                {
                    break;
                }
#else
                Matrix solved = JacobiTJacobi.SaveSolve(new Matrix(NegJacobiTError, true));
                if (solved == null) break; // should not happen
                pNext = pCurrent.Add(solved.Column(0));
#endif
                errorFunction(pNext, out Error);
                double error = dot(Error, Error);
                if (error < minError)
                {
                    double convergence = error / minError; // some value less than 1, as long as we have strong convergence we continue the process
                    //minErrorDifference = minError - error;
                    //minUpdateLength = Math.Sqrt(dot(NegJacobiTError, NegJacobiTError));
                    minLocation = pNext;
                    minError = error;
                    if (error <= errorTolerance && convergence > 0.25) // || minUpdateLength <= updateLengthTolerance)
                    {
                        parameters = pNext;
                        return true;
                    }
                }
                else if (numIterations > 5) // the first few steps may diverge
                {
                    parameters = pCurrent;
                    return error <= errorTolerance; // maybe the last step couldn't improve the result, so we take the last result (which was better)
                }
                else
                {
                    minError = error;
                }
                pCurrent = pNext;

            }
            parameters = pCurrent;
            return minError <= errorTolerance;
        }

        static double sqr(double d) { return d * d; }
        static double exp32(double d) { return Math.Sqrt(d * d * d); }
        /// <summary>
        /// Calculates the closest line approximating the provided <paramref name="points"/>.
        /// </summary>
        /// <param name="points">Points to be approximated by the line</param>
        /// <param name="location">Location of the line</param>
        /// <param name="direction">direction of the line</param>
        /// <returns>maximum distance to the line</returns>
        public static double LineFit(IArray<GeoPoint> points, double precision, out GeoPoint location, out GeoVector direction)
        {
            // We use a GaussNewtonMinimizer with the following parameters: l: location, d: direction of the line
            // parameters: 0:lx, 1:ly, 2: lz, 3: dx, 4: dy, 5: dz

            void efunc(double[] parameters, out double[] values)
            {
                // v = d ^ (l - p);
                // v.Length/d.Length is the distance of point p to the line (l,d)
                // sqrt((dy*(lz-pz)-dz*(ly-py))² + (...)² + (...)²) / sqrt(dx*dx+dy*dy+dz*dz) the distance
                // error: dy*(lz-pz)-dz*(ly-py))² + (...)² + (...)² / (dx*dx+dy*dy+dz*dz) the sqaure of the distance

                values = new double[points.Length];
                double lx = parameters[0];
                double ly = parameters[1];
                double lz = parameters[2];
                double dx = parameters[3];
                double dy = parameters[4];
                double dz = parameters[5];
                double dl = dx * dx + dy * dy + dz * dz;
                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    values[i] = (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px))) / (dl);
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {
                // derivations of the (square of the) distance by lx,ly,lz,dx,dy,dz
                //((-2 * dz * (dx * (lz - pz) - dz * (lx - px))) - 2 * dy * (dx * (ly - py) - dy * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2);
                //(2 * dx * (dx * (ly - py) - dy * (lx - px)) - 2 * dz * (dy * (lz - pz) - dz * (ly - py))) / (dz ^ 2 + dy ^ 2 + dx ^ 2);
                //(2 * dy * (dy * (lz - pz) - dz * (ly - py)) + 2 * dx * (dx * (lz - pz) - dz * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2);
                //(2 * (dx * (lz - pz) - dz * (lx - px)) * (lz - pz) + 2 * (dx * (ly - py) - dy * (lx - px)) * (ly - py)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) - (2 * dx * ((dy * (lz - pz) - dz * (ly - py)) ^ 2 + (dx * (lz - pz) - dz * (lx - px)) ^ 2 + (dx * (ly - py) - dy * (lx - px)) ^ 2)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) ^ 2;
                //(2 * (dy * (lz - pz) - dz * (ly - py)) * (lz - pz) + 2 * (px - lx) * (dx * (ly - py) - dy * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2) - (2 * dy * ((dy * (lz - pz) - dz * (ly - py)) ^ 2 + (dx * (lz - pz) - dz * (lx - px)) ^ 2 + (dx * (ly - py) - dy * (lx - px)) ^ 2)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) ^ 2;
                //(2 * (py - ly) * (dy * (lz - pz) - dz * (ly - py)) + 2 * (px - lx) * (dx * (lz - pz) - dz * (lx - px))) / (dz ^ 2 + dy ^ 2 + dx ^ 2) - (2 * dz * ((dy * (lz - pz) - dz * (ly - py)) ^ 2 + (dx * (lz - pz) - dz * (lx - px)) ^ 2 + (dx * (ly - py) - dy * (lx - px)) ^ 2)) / (dz ^ 2 + dy ^ 2 + dx ^ 2) ^ 2;

                derivs = new Matrix(points.Length, 6); // Jacobi Matrix Ableitungen nach cx, cy, cz und r
                                                       // (pnts.x-cx)²+(pnts.y-cy)²+(pnts.z-cz)²-r² == 0
                double lx = parameters[0];
                double ly = parameters[1];
                double lz = parameters[2];
                double dx = parameters[3];
                double dy = parameters[4];
                double dz = parameters[5];
                double dl = dx * dx + dy * dy + dz * dz;

                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    derivs[i, 0] = ((-2 * dz * (dx * (lz - pz) - dz * (lx - px))) - 2 * dy * (dx * (ly - py) - dy * (lx - px))) / dl;
                    derivs[i, 1] = (2 * dx * (dx * (ly - py) - dy * (lx - px)) - 2 * dz * (dy * (lz - pz) - dz * (ly - py))) / dl;
                    derivs[i, 2] = (2 * dy * (dy * (lz - pz) - dz * (ly - py)) + 2 * dx * (dx * (lz - pz) - dz * (lx - px))) / dl;
                    derivs[i, 3] = (2 * (dx * (lz - pz) - dz * (lx - px)) * (lz - pz) + 2 * (dx * (ly - py) - dy * (lx - px)) * (ly - py)) / (dl) - (2 * dx * (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px)))) / sqr(dl);
                    derivs[i, 4] = (2 * (dy * (lz - pz) - dz * (ly - py)) * (lz - pz) + 2 * (px - lx) * (dx * (ly - py) - dy * (lx - px))) / (dl) - (2 * dy * (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px)))) / sqr(dl);
                    derivs[i, 5] = (2 * (py - ly) * (dy * (lz - pz) - dz * (ly - py)) + 2 * (px - lx) * (dx * (lz - pz) - dz * (lx - px))) / (dl) - (2 * dz * (sqr(dy * (lz - pz) - dz * (ly - py)) + sqr(dx * (lz - pz) - dz * (lx - px)) + sqr(dx * (ly - py) - dy * (lx - px)))) / sqr(dl);
                }
            }
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            GeoVector d = points[1] - points[0];
            double tl = d.TaxicabLength;
            for (int i = 2; i < points.Length; i++)
            {
                GeoVector di = points[i] - points[0];
                double li = di.TaxicabLength;
                if (li > tl)
                {
                    tl = li;
                    d = di;
                }
            }
            bool ok = gnm.Solve(new double[] { points[0].x, points[0].y, points[0].z, d.x, d.y, d.z }, 30, 1e-6, precision * precision, out double minError, out int numiter, out double[] result);
            if (ok)
            {
                location = new GeoPoint(result[0], result[1], result[2]);
                direction = new GeoVector(result[3], result[4], result[5]);
                return Math.Sqrt(minError);
            }
            else
            {
                location = points[0];
                direction = d;
                return double.MaxValue;
            }
        }
        public static double CircleFit(IArray<GeoPoint> points, GeoPoint center, double radius, double precision, out Ellipse elli)
        {
            double maxerror = PlaneFit(points, precision, out Plane pln);

            GeoPoint2D[] points2d = new GeoPoint2D[points.Length];
            for (int i = 0; i < points2d.Length; i++)
            {
                points2d[i] = pln.Project(points[i]);
            }
            GeoPoint2D center2d;
            if (!center.IsValid)
            {
                BoundingRect ext = new BoundingRect(points2d);
                center2d = ext.GetCenter();
                radius = ext.Size / 4.0;
            }
            else
            {
                center2d = pln.Project(center);
            }
            // parameters: 0:cx, 1:cy, 3: radius

            void efunc(double[] parameters, out double[] values)
            {
                values = new double[points2d.Length];
                double cx = parameters[0];
                double cy = parameters[1];
                double r = parameters[2];
                for (int i = 0; i < points2d.Length; i++)
                {
                    double px = points2d[i].x;
                    double py = points2d[i].y;
                    values[i] = sqr(px - cx) + sqr(py - cy) - r * r;
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {
                derivs = new Matrix(points2d.Length, 3); // Jacobi Matrix for (px-cx)²+(py-cy)²-r² == 0
                double cx = parameters[0];
                double cy = parameters[1];
                double r = parameters[2];

                for (int i = 0; i < points.Length; i++)
                {
                    double px = points2d[i].x;
                    double py = points2d[i].y;
                    derivs[i, 0] = -2 * (px - cx);
                    derivs[i, 1] = -2 * (py - cy);
                    derivs[i, 2] = -2 * r;
                }
            }
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            bool ok = gnm.Solve(new double[] { center2d.x, center2d.y, radius }, 30, precision * precision * 1e-2, precision * precision, out double minError, out int numiter, out double[] result);
            if (ok || minError < 1e-9)
            {
                center = pln.ToGlobal(new GeoPoint2D(result[0], result[1]));
                elli = Ellipse.Construct();
                elli.SetCirclePlaneCenterRadius(pln, center, result[2]);
                return Math.Sqrt(minError);
            }
            else
            {
                elli = null;
                return double.MaxValue;
            }
        }
        public static double PlaneFit(IArray<GeoPoint> points, double precision, out Plane plane)
        {
            // We use a GaussNewtonMinimizer with the following parameters: n: normal of the plane (nx,ny,nz)
            // d distance of the plane from the origin, nx*px+ny*py+nz*pz=d
            // this is not good, because nx==ny==nz==d==0 is always a solution, so
            // sqr(c * pz + b * py + a * px - d) + sqr(a * a + b * b + c * c - 1); ( with (a,b,c)=(nx,ny,nz))
            // also forces the normal vector to have a length of 1

            void efunc(double[] parameters, out double[] values)
            {
                // error: a * px + b * py - c * pz - d (this may be positive or negative. It seems to be ok)
                // a * px + b * py - c * pz - d)^2+(a^2+b^2+c^2-1)^2;
                values = new double[points.Length];
                double a = parameters[0];
                double b = parameters[1];
                double c = parameters[2];
                double d = parameters[3];
                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    values[i] = sqr(c * pz + b * py + a * px - d) + sqr(a * a + b * b + c * c - 1);
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {

                derivs = new Matrix(points.Length, 4); // Jacobi Matrix Derivations
                double a = parameters[0];
                double b = parameters[1];
                double c = parameters[2];
                double d = parameters[3];

                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    
                    derivs[i, 0] = 2 * px * (-c * pz + b * py + a * px - d) + 4 * a * (c * c + b * b + a * a - 1);
                    derivs[i, 1] = 2 * py * (-c * pz + b * py + a * px - d) + 4 * b * (c * c + b * b + a * a - 1);
                    derivs[i, 2] = 2 * pz * (-c * pz + b * py + a * px - d) + 4 * c * (c * c + b * b + a * a - 1);
                    derivs[i, 3] = -2 * (-c * pz + b * py + a * px - d);
                }
            }
            double[] sparams = new double[4];
            for (int i = 0; i < points.Length; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    for (int k = j + 1; k < points.Length; k++)
                    {
                        GeoVector dirx = points[j] - points[i];
                        GeoVector diry = points[k] - points[i];
                        GeoVector n = dirx ^ diry;
                        if (!n.IsNullVector())
                        {
                            Plane pln = new Plane(points[i], dirx, diry);
                            n.Norm();
                            double d = pln.Distance(GeoPoint.Origin);
                            sparams[0] = n.x;
                            sparams[1] = n.y;
                            sparams[2] = n.z;
                            sparams[3] = -d;
                            break;
                        }
                    }
                }
            }
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            bool ok = gnm.Solve(sparams, 30, 1e-6, precision * precision, out double minError, out int numiter, out double[] result);
            if (ok)
            {
                GeoVector normal = new GeoVector(result[0], result[1], result[2]);
                GeoPoint loc = new GeoPoint(result[3] * result[0], result[3] * result[1], result[3] * result[2]);
                plane = new Plane(loc, normal);
                return Math.Sqrt(minError);
            }
            else
            {
                plane = Plane.XYPlane;
                return double.MaxValue;
            }
        }

        public static double ConeFit(IArray<GeoPoint> points, GeoPoint apex, GeoVector axis, double theta, double precision, out ConicalSurface cs)
        {
            /*
             * two rotations and a scaling on the z-axis convert the provided cone data into the unit cone.
             * First move the apex to the origin (px-ax), then rotate (by a) around the z-axis to align the axis with the y-axis, next rotate (by b) around the x-axis to 
             * coincide the cone axis with the z-axis, then scale (by f) the z-axis to make the (full) opening angle 90°
            RZ: matrix([cos(a),-sin(a),0],[sin(a),cos(a),0],[0,0,1]);
            RX: matrix([1,0,0],[0,cos(b),-sin(b)],[0,sin(b),cos(b)]);
            FZ: matrix([1,0,0],[0,1,0],[0,0,f]);
            P: matrix([px-ax],[py-ay],[pz-az]);
            E: FZ.RX.RZ.P;
            Q: E[1]^2+E[2]^2-E[3]^2;
            dfdax: diff(Q, ax, 1);
            dfday: diff(Q, ay, 1);
            dfdaz: diff(Q, az, 1);
            dfda: diff(Q,a,1);
            dfdb: diff(Q,b,1);
            yields:
            Q:[(cos(b)*(cos(a)*(py-ay)+sin(a)*(px-ax))-sin(b)*(pz-az))^2-f^2*(cos(b)*(pz-az)+sin(b)*(cos(a)*(py-ay)+sin(a)*(px-ax)))^2+(cos(a)*(px-ax)-sin(a)*(py-ay))^2];
            dfdax:[(-2*sin(a)*cos(b)*(cos(b)*(cos(a)*(py-ay)+sin(a)*(px-ax))-sin(b)*(pz-az)))+2*sin(a)*sin(b)*f^2*(cos(b)*(pz-az)+sin(b)*(cos(a)*(py-ay)+sin(a)*(px-ax)))-2*cos(a)*(cos(a)*(px-ax)-sin(a)*(py-ay))];
            dfday:[(-2*cos(a)*cos(b)*(cos(b)*(cos(a)*(py-ay)+sin(a)*(px-ax))-sin(b)*(pz-az)))+2*cos(a)*sin(b)*f^2*(cos(b)*(pz-az)+sin(b)*(cos(a)*(py-ay)+sin(a)*(px-ax)))+2*sin(a)*(cos(a)*(px-ax)-sin(a)*(py-ay))];
            dfdaz:[2*sin(b)*(cos(b)*(cos(a)*(py-ay)+sin(a)*(px-ax))-sin(b)*(pz-az))+2*cos(b)*f^2*(cos(b)*(pz-az)+sin(b)*(cos(a)*(py-ay)+sin(a)*(px-ax)))];
            dfda:[2*cos(b)*(cos(a)*(px-ax)-sin(a)*(py-ay))*(cos(b)*(cos(a)*(py-ay)+sin(a)*(px-ax))-sin(b)*(pz-az))-2*sin(b)*f^2*(cos(a)*(px-ax)-sin(a)*(py-ay))*(cos(b)*(pz-az)+sin(b)*(cos(a)*(py-ay)+sin(a)*(px-ax)))+2*((-cos(a)*(py-ay))-sin(a)*(px-ax))*(cos(a)*(px-ax)-sin(a)*(py-ay))];
            dfdb:[2*((-cos(b)*(pz-az))-sin(b)*(cos(a)*(py-ay)+sin(a)*(px-ax)))*(cos(b)*(cos(a)*(py-ay)+sin(a)*(px-ax))-sin(b)*(pz-az))-2*f^2*(cos(b)*(pz-az)+sin(b)*(cos(a)*(py-ay)+sin(a)*(px-ax)))*(cos(b)*(cos(a)*(py-ay)+sin(a)*(px-ax))-sin(b)*(pz-az))];
            */
#if DEBUG
            GeoObjectList dbglst = new GeoObjectList();
#endif
            // parameters: { ax, ay, az, a, b, f }
            void efunc(double[] parameters, out double[] values)
            {
#if DEBUG
                ConicalSurface csdbg = new ConicalSurface(ModOp.Translate(parameters[0], parameters[1], parameters[2]) * ModOp.Rotate(GeoVector.ZAxis, -parameters[3]) * ModOp.Rotate(GeoVector.XAxis, -parameters[4]) * ModOp.Scale(parameters[5], parameters[5], 1));
                Face fcdbg = Face.MakeFace(csdbg, new BoundingRect(Math.PI, -0.005, 2 * Math.PI, -0.01));
                dbglst.Add(fcdbg);
#endif
                values = new double[points.Length];
                double ax = parameters[0];
                double ay = parameters[1];
                double az = parameters[2];
                double a = parameters[3];
                double b = parameters[4];
                double f = parameters[5];
                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    values[i] = sqr(Math.Cos(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)) - Math.Sin(b) * (pz - az)) - f * f * sqr(Math.Cos(b) * (pz - az) + Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax))) + sqr(Math.Cos(a) * (px - ax) - Math.Sin(a) * (py - ay));
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {
                derivs = new Matrix(points.Length, 6); // Jacobi Matrix 
                double ax = parameters[0];
                double ay = parameters[1];
                double az = parameters[2];
                double a = parameters[3];
                double b = parameters[4];
                double f = parameters[5];
                double ff = f * f;
                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    derivs[i, 0] = (-2 * Math.Sin(a) * Math.Cos(b) * (Math.Cos(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)) - Math.Sin(b) * (pz - az))) + 2 * Math.Sin(a) * Math.Sin(b) * ff * (Math.Cos(b) * (pz - az) + Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax))) - 2 * Math.Cos(a) * (Math.Cos(a) * (px - ax) - Math.Sin(a) * (py - ay));
                    derivs[i, 1] = (-2 * Math.Cos(a) * Math.Cos(b) * (Math.Cos(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)) - Math.Sin(b) * (pz - az))) + 2 * Math.Cos(a) * Math.Sin(b) * f * (Math.Cos(b) * (pz - az) + Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax))) + 2 * Math.Sin(a) * (Math.Cos(a) * (px - ax) - Math.Sin(a) * (py - ay));
                    derivs[i, 2] = 2 * Math.Sin(b) * (Math.Cos(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)) - Math.Sin(b) * (pz - az)) + 2 * Math.Cos(b) * f * (Math.Cos(b) * (pz - az) + Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)));
                    derivs[i, 3] = 2 * Math.Cos(b) * (Math.Cos(a) * (px - ax) - Math.Sin(a) * (py - ay)) * (Math.Cos(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)) - Math.Sin(b) * (pz - az)) - 2 * Math.Sin(b) * f * (Math.Cos(a) * (px - ax) - Math.Sin(a) * (py - ay)) * (Math.Cos(b) * (pz - az) + Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax))) + 2 * ((-Math.Cos(a) * (py - ay)) - Math.Sin(a) * (px - ax)) * (Math.Cos(a) * (px - ax) - Math.Sin(a) * (py - ay));
                    derivs[i, 4] = 2 * ((-Math.Cos(b) * (pz - az)) - Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax))) * (Math.Cos(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)) - Math.Sin(b) * (pz - az)) - 2 * f * (Math.Cos(b) * (pz - az) + Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax))) * (Math.Cos(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)) - Math.Sin(b) * (pz - az));
                    derivs[i, 5] = -2 * f * sqr(Math.Cos(b) * (pz - az) + Math.Sin(b) * (Math.Cos(a) * (py - ay) + Math.Sin(a) * (px - ax)));
                }
            }
            SweepAngle toY = Angle.A90 - new Angle(axis.x, axis.y);
            GeoVector axisY = ModOp.Rotate(GeoVector.ZAxis, toY) * axis;
            SweepAngle toZ = Angle.A90 - new Angle(axisY.y, axisY.z);
            GeoVector axisZ = ModOp.Rotate(GeoVector.XAxis, toZ) * axisY;

            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            bool ok = gnm.Solve(new double[] { apex.x, apex.y, apex.z, toY, toZ, theta }, 30, precision * precision * 0.01, precision * precision, out double minError, out int numiter, out double[] result);
            // parameters: 0: apex.x, 1:apex.y, 2: apex.z, 3: a (rotation around z-axis), 4: b (rotation around z-axis), 5: f (stretching factor in z)
            if (ok)
            {
                cs = new ConicalSurface(ModOp.Translate(result[0], result[1], result[2]) * ModOp.Rotate(GeoVector.ZAxis, -result[3]) * ModOp.Rotate(GeoVector.XAxis, -result[4]) * ModOp.Scale(result[5], result[5], 1));
                return Math.Sqrt(minError);
            }
            else
            {
                cs = null;
                return double.MaxValue;
            }
        }
        public static double ConeFitOld(GeoPoint[] points, GeoPoint apex, GeoVector axis, double theta, out ConicalSurface cs)
        {
            // according to wikipedia (a cone with apex=(0,0,0), d: axis and theta: opening angle
            //F(u) = (u * d)^2 - (d*d)*(u*u)*cos(theta)
            // with apex=c:
            // F(p) = ((p-c)*d)^2 - (d*d)*((p-c)*(p-c))*(cos(theta))^2
            //ax: px - cx;
            //ay: py - cy;
            //az: pz - cz;
            //f: (ax * dx + ay * dy + az * dz) ^ 2 - (dx * dx + dy * dy + dz * dz) * (ax * ax + ay * ay + az * az) * (cos(t)) ^ 2;
            //dfdcx: diff(f, cx, 1);
            //dfdcy: diff(f, cy, 1);
            //dfdcz: diff(f, cz, 1);
            //dfdnx: diff(f, dx, 1);
            //dfdny: diff(f, dy, 1);
            //dfdnz: diff(f, dz, 1);
            //dfdt: diff(f, t, 1);
            //stringout("C:/Temp/coneapprox.txt", values);
            // yields:
            //f: (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) ^ 2 - (dz ^ 2 + dy ^ 2 + dx ^ 2) * ((pz - cz) ^ 2 + (py - cy) ^ 2 + (px - cx) ^ 2) * cos(t) ^ 2;
            //dfdcx: 2 * (dz ^ 2 + dy ^ 2 + dx ^ 2) * (px - cx) * cos(t) ^ 2 - 2 * dx * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx));
            //dfdcy: 2 * (dz ^ 2 + dy ^ 2 + dx ^ 2) * (py - cy) * cos(t) ^ 2 - 2 * dy * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx));
            //dfdcz: 2 * (dz ^ 2 + dy ^ 2 + dx ^ 2) * (pz - cz) * cos(t) ^ 2 - 2 * dz * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx));
            //dfdnx: 2 * (px - cx) * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) - 2 * dx * ((pz - cz) ^ 2 + (py - cy) ^ 2 + (px - cx) ^ 2) * cos(t) ^ 2;
            //dfdny: 2 * (py - cy) * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) - 2 * dy * ((pz - cz) ^ 2 + (py - cy) ^ 2 + (px - cx) ^ 2) * cos(t) ^ 2;
            //dfdnz: 2 * (pz - cz) * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) - 2 * dz * ((pz - cz) ^ 2 + (py - cy) ^ 2 + (px - cx) ^ 2) * cos(t) ^ 2;
            //dfdt: 2 * (dz ^ 2 + dy ^ 2 + dx ^ 2) * ((pz - cz) ^ 2 + (py - cy) ^ 2 + (px - cx) ^ 2) * cos(t) * sin(t);
            //ax: px - cx;
            //ay: py - cy;
            //az: pz - cz;

            /*
            RZ: matrix([cos(a),-sin(a),0],[sin(a),cos(a),0],[0,0,1]);
            RX: matrix([1,0,0],[0,cos(b),-sin(b)],[0,sin(b),cos(b)]);
            FZ: matrix([1,0,0],[0,1,0],[0,0,f]);
            P: matrix([px-ax],[py-ay],[pz-az]);
            E: FZ.RX.RZ.P;
            Q: E[1]^2+E[2]^2-E[3]^2;
            dfdax: diff(Q, ax, 1);
            dfday: diff(Q, ay, 1);
            dfdaz: diff(Q, az, 1);
            dfda: diff(Q,a,1);
            dfdb: diff(Q,b,1);
            */

            void efunc(double[] parameters, out double[] values)
            {
                values = new double[points.Length];
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double dx = parameters[3];
                double dy = parameters[4];
                double dz = parameters[5];
                double t = parameters[6];
                for (int i = 0; i < points.Length; i++)
                {
                    GeoVector a = points[i] - new GeoPoint(cx, cy, cz);
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    values[i] = sqr(dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) - (dz * dz + dy * dy + dx * dx) * (sqr(pz - cz) + sqr(py - cy) + sqr(px - cx)) * sqr(Math.Cos(t));
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {
                derivs = new Matrix(points.Length, 7); // Jacobi Matrix 
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double dx = parameters[3];
                double dy = parameters[4];
                double dz = parameters[5];
                double t = parameters[6];
                double dd = (dz * dz + dy * dy + dx * dx);
                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    double pc = sqr(pz - cz) + sqr(py - cy) + sqr(px - cx);
                    derivs[i, 0] = 2 * dd * (px - cx) * sqr(Math.Cos(t)) - 2 * dx * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx));
                    derivs[i, 1] = 2 * dd * (py - cy) * sqr(Math.Cos(t)) - 2 * dy * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx));
                    derivs[i, 2] = 2 * dd * (pz - cz) * sqr(Math.Cos(t)) - 2 * dz * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx));
                    derivs[i, 3] = 2 * (px - cx) * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) - 2 * dx * (sqr(pz - cz) + sqr(py - cy) + sqr(px - cx)) * sqr(Math.Cos(t));
                    derivs[i, 4] = 2 * (py - cy) * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) - 2 * dy * (sqr(pz - cz) + sqr(py - cy) + sqr(px - cx)) * sqr(Math.Cos(t));
                    derivs[i, 5] = 2 * (pz - cz) * (dz * (pz - cz) + dy * (py - cy) + dx * (px - cx)) - 2 * dz * (sqr(pz - cz) + sqr(py - cy) + sqr(px - cx)) * sqr(Math.Cos(t));
                    derivs[i, 6] = 2 * dd * (sqr(pz - cz) + sqr(py - cy) + sqr(px - cx)) * Math.Cos(t) * Math.Sin(t);
                }
            }
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            bool ok = gnm.Solve(new double[] { apex.x, apex.y, apex.z, axis.x, axis.y, axis.z, theta }, 30, 1e-6, 1e-9, out double minError, out int numiter, out double[] result);
            // parameters: 0: apex.x, 1:apex.y, 2: apex.z, 3: axis.x, 4: axis.y, 5: axis.z, 6: theta
            if (ok)
            {
                apex = new GeoPoint(result[0], result[1], result[2]);
                axis = new GeoVector(result[3], result[4], result[5]);
                Plane pln = new Plane(apex, axis); // arbitrary x and y direction
                theta = result[6];
                cs = new ConicalSurface(apex, pln.DirectionX, pln.DirectionY, pln.Normal, theta);
                return minError;
            }
            else
            {
                cs = null;
                return double.MaxValue;
            }
        }
        public static double TorusFit(IArray<GeoPoint> points, GeoPoint center, GeoVector axis, double minrad, double precision, out ToroidalSurface ts)
        {
            // see https://www.geometrictools.com/Documentation/DistanceToCircle3.pdf
            // using this Maxima input:
            //R2: nx* nx +ny * ny + nz * nz; /* n is both the normal (axis direction) of the torus and the major radius (lenth of n) */
            //dx: px - cx;
            //dy: py - cy;
            //dz: pz - cz;
            //ndx: ny* dz-nz * dy;
            //ndy: nx* dz-nz * dx;
            //ndz: nx* dy-ny * dx;
            //a: (nx * dx + ny * dy + nz * dz) ^ 2 / R2; /* sub expression */
            //f: r* r -(a + (sqrt(dx * dx + dy * dy + dz * dz - a) - sqrt(R2)) ^ 2); /* r*r - "distance point to main torus circle", r is the minor radius, positive inside, negative outside of the torus */
            //dfdcx: diff(f, cx, 1); /* derivations needed for Jacobi Matrix */
            //dfdcy: diff(f, cy, 1);
            //dfdcz: diff(f, cz, 1);
            //dfdnx: diff(f, nx, 1);
            //dfdny: diff(f, ny, 1);
            //dfdnz: diff(f, nz, 1);
            //dfdr: diff(f, r, 1);
            //stringout("C:/Temp/torusapprox.txt", values);
            void efunc(double[] parameters, out double[] values)
            {
                values = new double[points.Length];
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double nx = parameters[3];
                double ny = parameters[4];
                double nz = parameters[5];
                double r = parameters[6];
                double R2 = nx * nx + ny * ny + nz * nz;
                double R = Math.Sqrt(R2);
                for (int i = 0; i < points.Length; i++)
                {
                    GeoVector d = points[i] - new GeoPoint(cx, cy, cz);
                    values[i] = r * r - (sqr(nx * d.x + ny * d.y + nz * d.z) / R2 + sqr(Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z - sqr(nx * d.x + ny * d.y + nz * d.z) / R2) - R));
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {
                derivs = new Matrix(points.Length, 7); // Jacobi Matrix 
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double nx = parameters[3];
                double ny = parameters[4];
                double nz = parameters[5];
                double r = parameters[6];
                double nn = nz * nz + ny * ny + nx * nx;
                double rn = Math.Sqrt(nn);

                for (int i = 0; i < points.Length; i++)
                {
                    double px = points[i].x;
                    double py = points[i].y;
                    double pz = points[i].z;
                    double dx = px - cx;
                    double dy = py - cy;
                    double dz = pz - cz;
                    // manually extracted common subexpressions
                    double nd = nz * dz + ny * dy + nx * dx;
                    double dd = dz * dz + dy * dy + dx * dx;
                    double b = Math.Sqrt((-sqr(nd)) / nn + dd);
                    double a = b - Math.Sqrt(nn);
                    double ndnn = nd / nn;
                    double sndnn = sqr(nd) / sqr(nn);
                    derivs[i, 0] = 2.0 * nx * ndnn - (2.0 * nx * ndnn - 2.0 * dx) * a / b;
                    derivs[i, 1] = 2.0 * ny * ndnn - (2.0 * ny * ndnn - 2.0 * dy) * a / b;
                    derivs[i, 2] = 2.0 * nz * ndnn - (2.0 * nz * ndnn - 2.0 * dz) * a / b;
                    derivs[i, 3] = (-2.0) * ((nx * sndnn - dx * ndnn) / b - nx / rn) * a + 2.0 * nx * sndnn - 2.0 * dx * ndnn;
                    derivs[i, 4] = (-2.0) * ((ny * sndnn - dy * ndnn) / b - ny / rn) * a + 2.0 * ny * sndnn - 2.0 * dy * ndnn;
                    derivs[i, 5] = (-2.0) * ((nz * sndnn - dz * ndnn) / b - nz / rn) * a + 2.0 * nz * sndnn - 2.0 * dz * ndnn;
                    derivs[i, 6] = 2 * r;
                }
            }
#if MATHNETx
                MathNet.Numerics.LinearAlgebra.Vector<double> oeFunc(MathNet.Numerics.LinearAlgebra.Vector<double> parameters, MathNet.Numerics.LinearAlgebra.Vector<double> x)
                {
                    efunc(parameters.ToArray(), out double[] errorValues);
                    return MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(errorValues);
                }
                MathNet.Numerics.LinearAlgebra.Matrix<double> ojfunc(MathNet.Numerics.LinearAlgebra.Vector<double> parameters, MathNet.Numerics.LinearAlgebra.Vector<double> x)
                {
                    jfunc(parameters.ToArray(), out Matrix partialDerivations);
                    var derivs = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(partialDerivations.RowCount, partialDerivations.ColumnCount);
                    for (int i = 0; i < partialDerivations.RowCount; i++)
                    {
                        for (int j = 0; j < partialDerivations.ColumnCount; j++)
                        {
                            derivs[i, j] = partialDerivations[i, j];
                        }
                    }
                    return derivs;
                }
                MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer minimizer = new MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer(0.001, 1e-20, 1e-20, 1e-20, 20);

                var obj = MathNet.Numerics.Optimization.ObjectiveFunction.NonlinearModel(oeFunc, ojfunc, MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(points.Length), MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(points.Length));
                var lmresult = minimizer.FindMinimum(obj, MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(new double[] { 0, 0, 0, 0, 0, 1, 1 }));

#endif
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            bool ok = gnm.Solve(new double[] { center.x, center.y, center.z, axis.x, axis.y, axis.z, minrad }, 30, 1e-6, precision * precision, out double minError, out int numiter, out double[] result);
            // parameters: 0:cx, 1:cy, 2: cz, 3: nx, 4: ny, 5: nz, 6: r (majorRadius is sqrt(n*n), r: minorRadius)
            if (ok)
            {
                GeoPoint loc = new GeoPoint(result[0], result[1], result[2]);
                GeoVector normal = new GeoVector(result[3], result[4], result[5]);
                double majrad = normal.Length;
                Plane pln = new Plane(loc, normal);
                ts = new ToroidalSurface(pln.Location, pln.DirectionX, pln.DirectionY, pln.Normal, majrad, Math.Abs(result[6]));
                return Math.Sqrt(minError);
            }
            else
            {
                ts = null;
                return double.MaxValue;
            }
        }
        public static double SphereFit(IArray<GeoPoint> pnts, GeoPoint center, double radius, double precision, out SphericalSurface ss)
        {
            // parameters: 0:cx, 1:cy, 2: cz, 3: r
            void efunc(double[] parameters, out double[] values)
            {
                // (pnts.x-cx)²+(pnts.y-cy)²+(pnts.z-cz)²==r²
                values = new double[pnts.Length];
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double r = parameters[3];
                for (int i = 0; i < pnts.Length; i++)
                {
                    values[i] = sqr(pnts[i].x - cx) + sqr(pnts[i].y - cy) + sqr(pnts[i].z - cz) - sqr(r);
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {
                derivs = new Matrix(pnts.Length, 4); // Jacobi Matrix Ableitungen nach cx, cy, cz und r
                // (pnts.x-cx)²+(pnts.y-cy)²+(pnts.z-cz)²-r² == 0
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double r = parameters[3];

                for (int i = 0; i < pnts.Length; i++)
                {
                    derivs[i, 0] = -2 * (pnts[i].x - cx);
                    derivs[i, 1] = -2 * (pnts[i].y - cy);
                    derivs[i, 2] = -2 * (pnts[i].z - cz);
                    derivs[i, 3] = -2 * r;
                }
            }
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            bool ok = gnm.Solve(new double[] { center.x, center.y, center.z, radius }, 30, 1e-6, precision*precision, out double minError, out int numiter, out double[] result);
            if (ok)
            {
                center = new GeoPoint(result[0], result[1], result[2]);
                radius = Math.Abs(result[3]);
                ss = new SphericalSurface(center, radius * GeoVector.XAxis, radius * GeoVector.YAxis, radius * GeoVector.ZAxis);
                return Math.Sqrt(minError);
            }
            else
            {
                ss = null;
                return double.MaxValue;
            }

        }
        public static double CylinderFit(IArray<GeoPoint> pnts, GeoPoint center, GeoVector axis, double radius, double precision, out CylindricalSurface cs)
        {
            /* maxima: disance point to line
                load("vect"); 
                norm(x) := sqrt(x . x);
                loc: [lx,ly,lz];
                dir: [dx,dy,dz];
                p: [px,py,pz];
                v1: express(dir~(loc-p));
                dist: norm(v1)/norm(dir);
                difflx: diff(dist-r,lx,1);
                diffly: diff(dist-r,ly,1);
                difflz: diff(dist-r,lz,1);
                diffdx: diff(dist-r,dx,1);
                diffdy: diff(dist-r,dy,1);
                diffdz: diff(dist-r,dz,1);
                diffr: diff(dist-r,r,1);
                yields:
                dist:sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2)/sqrt(dz^2+dy^2+dx^2);
                difflx:(2*dz*(dz*(lx-px)-dx*(lz-pz))-2*dy*(dx*(ly-py)-dy*(lx-px)))/(2*sqrt(dz^2+dy^2+dx^2)*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2));
                diffly:(2*dx*(dx*(ly-py)-dy*(lx-px))-2*dz*(dy*(lz-pz)-dz*(ly-py)))/(2*sqrt(dz^2+dy^2+dx^2)*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2));
                difflz:(2*dy*(dy*(lz-pz)-dz*(ly-py))-2*dx*(dz*(lx-px)-dx*(lz-pz)))/(2*sqrt(dz^2+dy^2+dx^2)*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2));
                diffdx:(2*(dz*(lx-px)-dx*(lz-pz))*(pz-lz)+2*(dx*(ly-py)-dy*(lx-px))*(ly-py))/(2*sqrt(dz^2+dy^2+dx^2)*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2))-(dx*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2))/(dz^2+dy^2+dx^2)^(3/2);
                diffdy:(2*(dy*(lz-pz)-dz*(ly-py))*(lz-pz)+2*(px-lx)*(dx*(ly-py)-dy*(lx-px)))/(2*sqrt(dz^2+dy^2+dx^2)*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2))-(dy*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2))/(dz^2+dy^2+dx^2)^(3/2);
                diffdz:(2*(py-ly)*(dy*(lz-pz)-dz*(ly-py))+2*(lx-px)*(dz*(lx-px)-dx*(lz-pz)))/(2*sqrt(dz^2+dy^2+dx^2)*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2))-(dz*sqrt((dy*(lz-pz)-dz*(ly-py))^2+(dz*(lx-px)-dx*(lz-pz))^2+(dx*(ly-py)-dy*(lx-px))^2))/(dz^2+dy^2+dx^2)^(3/2);
                diffr:-1;
                simplified:
                dist=sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px)))))/sqrt((sqr(dz)+sqr(dy)+sqr(dx)));;
                difflx=(2.0*dz*(dz*(lx-px)-dx*(lz-pz))-2.0*dy*(dx*(ly-py)-dy*(lx-px)))/((2.0*sqrt((sqr(dz)+sqr(dy)+sqr(dx))))*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))));;
                diffly=(2.0*dx*(dx*(ly-py)-dy*(lx-px))-2.0*dz*(dy*(lz-pz)-dz*(ly-py)))/((2.0*sqrt((sqr(dz)+sqr(dy)+sqr(dx))))*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))));;
                difflz=(2.0*dy*(dy*(lz-pz)-dz*(ly-py))-2.0*dx*(dz*(lx-px)-dx*(lz-pz)))/((2.0*sqrt((sqr(dz)+sqr(dy)+sqr(dx))))*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))));;
                diffdx=((2.0*(dz*(lx-px)-dx*(lz-pz)))*(pz-lz)+(2.0*(dx*(ly-py)-dy*(lx-px)))*(ly-py))/((2.0*sqrt((sqr(dz)+sqr(dy)+sqr(dx))))*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))))-(dx*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))))/exp((sqr(dz)+sqr(dy)+sqr(dx)), (3.0/2.0));;
                diffdy=((2.0*(dy*(lz-pz)-dz*(ly-py)))*(lz-pz)+(2.0*(px-lx))*(dx*(ly-py)-dy*(lx-px)))/((2.0*sqrt((sqr(dz)+sqr(dy)+sqr(dx))))*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))))-(dy*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))))/exp((sqr(dz)+sqr(dy)+sqr(dx)), (3.0/2.0));;
                diffdz=((2.0*(py-ly))*(dy*(lz-pz)-dz*(ly-py))+(2.0*(lx-px))*(dz*(lx-px)-dx*(lz-pz)))/((2.0*sqrt((sqr(dz)+sqr(dy)+sqr(dx))))*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))))-(dz*sqrt((sqr((dy*(lz-pz)-dz*(ly-py)))+sqr((dz*(lx-px)-dx*(lz-pz)))+sqr((dx*(ly-py)-dy*(lx-px))))))/exp((sqr(dz)+sqr(dy)+sqr(dx)), (3.0/2.0));
             */
            // parameters: 0: lx, 1: ly, 2: lz, 3: dx, 4: dy, 5: dz, 6: r (l: location, d: direction, r: radius)
            void efunc(double[] parameters, out double[] values)
            {
                values = new double[pnts.Length];
                double lx = parameters[0];
                double ly = parameters[1];
                double lz = parameters[2];
                double dx = parameters[3];
                double dy = parameters[4];
                double dz = parameters[5];
                double r = parameters[6];
                for (int i = 0; i < pnts.Length; i++)
                {
                    double px = pnts[i].x;
                    double py = pnts[i].y;
                    double pz = pnts[i].z;
                    values[i] = Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px))))) / Math.Sqrt((sqr(dz) + sqr(dy) + sqr(dx))) - r;
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {
                derivs = new Matrix(pnts.Length, 7); // Jacobi Matrix 
                double lx = parameters[0];
                double ly = parameters[1];
                double lz = parameters[2];
                double dx = parameters[3];
                double dy = parameters[4];
                double dz = parameters[5];
                double r = parameters[6];

                for (int i = 0; i < pnts.Length; i++)
                {
                    double px = pnts[i].x;
                    double py = pnts[i].y;
                    double pz = pnts[i].z;

                    derivs[i, 0] = (2.0 * dz * (dz * (lx - px) - dx * (lz - pz)) - 2.0 * dy * (dx * (ly - py) - dy * (lx - px))) / ((2.0 * Math.Sqrt((sqr(dz) + sqr(dy) + sqr(dx)))) * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px))))));
                    derivs[i, 1] = (2.0 * dx * (dx * (ly - py) - dy * (lx - px)) - 2.0 * dz * (dy * (lz - pz) - dz * (ly - py))) / ((2.0 * Math.Sqrt((sqr(dz) + sqr(dy) + sqr(dx)))) * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px))))));
                    derivs[i, 2] = (2.0 * dy * (dy * (lz - pz) - dz * (ly - py)) - 2.0 * dx * (dz * (lx - px) - dx * (lz - pz))) / ((2.0 * Math.Sqrt((sqr(dz) + sqr(dy) + sqr(dx)))) * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px))))));
                    derivs[i, 3] = ((2.0 * (dz * (lx - px) - dx * (lz - pz))) * (pz - lz) + (2.0 * (dx * (ly - py) - dy * (lx - px))) * (ly - py)) / ((2.0 * Math.Sqrt((sqr(dz) + sqr(dy) + sqr(dx)))) * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px)))))) - (dx * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px)))))) / exp32((sqr(dz) + sqr(dy) + sqr(dx)));
                    derivs[i, 4] = ((2.0 * (dy * (lz - pz) - dz * (ly - py))) * (lz - pz) + (2.0 * (px - lx)) * (dx * (ly - py) - dy * (lx - px))) / ((2.0 * Math.Sqrt((sqr(dz) + sqr(dy) + sqr(dx)))) * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px)))))) - (dy * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px)))))) / exp32((sqr(dz) + sqr(dy) + sqr(dx)));
                    derivs[i, 5] = ((2.0 * (py - ly)) * (dy * (lz - pz) - dz * (ly - py)) + (2.0 * (lx - px)) * (dz * (lx - px) - dx * (lz - pz))) / ((2.0 * Math.Sqrt((sqr(dz) + sqr(dy) + sqr(dx)))) * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px)))))) - (dz * Math.Sqrt((sqr((dy * (lz - pz) - dz * (ly - py))) + sqr((dz * (lx - px) - dx * (lz - pz))) + sqr((dx * (ly - py) - dy * (lx - px)))))) / exp32((sqr(dz) + sqr(dy) + sqr(dx)));
                    derivs[i, 6] = -1.0;
                }
            }
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            // the algorithm doesn't converge when the axis is almost exactely aligned to the x-axis (or y-axis, z-axis). So we modify the data
            // that the starting axis is (1,1,1) and and reverse the result to the original position. Maybe we need to do the same with other Fit methods?
            GeoVector diag = new GeoVector(1, 1, 1).Normalized;
            ModOp toDiag = ModOp.Rotate(center, axis, diag);
            for (int i = 0; i < pnts.Length; i++)
            {
                pnts[i] = toDiag * pnts[i];
            }
            axis = toDiag * axis;
            bool ok = gnm.Solve(new double[] { center.x, center.y, center.z, axis.x, axis.y, axis.z, radius }, 30, 1e-6, precision * precision, out double minError, out int numiter, out double[] result);
            if (ok)
            {
                center = new GeoPoint(result[0], result[1], result[2]);
                axis = new GeoVector(result[3], result[4], result[5]).Normalized;
                ModOp fromDiag = toDiag.GetInverse();
                axis = fromDiag * axis;
                center = fromDiag * center;
                axis.ArbitraryNormals(out GeoVector dirx, out GeoVector diry);
                radius = Math.Abs(result[6]);
                cs = new CylindricalSurface(center, radius * dirx, radius * diry, axis);
                return Math.Sqrt(minError);
            }
            else
            {
                cs = null;
                return double.MaxValue;
            }

        }
#if DEBUG
        public static void DebugCone()
        {
            Plane pln = new Plane(GeoPoint.Origin, new GeoVector(50, -50, 50));
            ConicalSurface cs = new ConicalSurface(new GeoPoint(100, 80, 60), pln.DirectionX, pln.DirectionY, pln.Normal, 0.5);
            Face fc = Face.MakeFace(cs, new BoundingRect(0, 1, Math.PI, 10));
            GeoPoint p0 = cs.PointAt(new GeoPoint2D(0, 1));
            GeoPoint p1 = cs.PointAt(new GeoPoint2D(0, 0));
            GeoPoint p2 = Geometry.DropPL(p0, p1, pln.Normal);
            double d = p2 | p1;
            GeoPoint[] samples = new GeoPoint[25]; // 25 evenly spread points
            double umin = 0, umax = Math.PI, vmin = 1, vmax = 10;
            double ustep = (umax - umin) / 4;
            double vstep = (vmax - vmin) / 4;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    samples[5 * i + j] = cs.PointAt(new GeoPoint2D(umin + i * ustep, vmin + j * vstep));
                }
            }
            GeoPoint cnt = new GeoPoint(samples);
            double maxerror = ConeFit(samples.ToIArray(), cnt, new GeoVector(1, -1, 1), 0.45, 1e-6, out ConicalSurface ocs);
            Face ofc = Face.MakeFace(ocs, new BoundingRect(0, 1, Math.PI, 10));

        }
        public static void DebugApproxNurbs()
        {
            /* zum Interpolieren eines NURBS (mit nur einem Knotenabschnitt)
             * Die Parameter für die Fehlerfunktion sind die Pole (mit Gewichten)
             * Der 1. und letzte Pol haben Gewicht 1
             * Die Fehlerfunktion erzeugt den NURBS und berechnet die Abstände zu den samples
             * Es wird keine Ableitungsfunktion zu verfügung gestellt, keine Jacobi Matrix
             */
            BSpline2D bsp = BSpline2D.MakeCircle(new GeoPoint2D(10, 20), 5);
            bsp.PointAt(0.125);
            double[] uknots = new double[3];
            double u = 0.125;
            int degree = 2;
            int span = 2;
            double[,] p = new double[3, 3];
            double x = 0 + (0 + ((uknots[span + 1] - u) * ((0 + ((uknots[span + 1] - u) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))))) / (uknots[span + 1] - u + (u - uknots[span + (-1)]))))) * p[span - degree + 0, 0] + (((u - uknots[span + (-1)]) * ((0 + ((uknots[span + 1] - u) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))))) / (uknots[span + 1] - u + (u - uknots[span + (-1)])))) + ((uknots[span + 2] - u) * ((u - uknots[span + 0]) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))) / (uknots[span + 2] - u + (u - uknots[span + 0]))))) * p[span - degree + 1, 0] + (u - uknots[span + 0]) * ((u - uknots[span + 0]) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))) / (uknots[span + 2] - u + (u - uknots[span + 0]))) * p[span - degree + 2, 0];
            double y = 0 + (0 + ((uknots[span + 1] - u) * ((0 + ((uknots[span + 1] - u) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))))) / (uknots[span + 1] - u + (u - uknots[span + (-1)]))))) * p[span - degree + 0, 1] + (((u - uknots[span + (-1)]) * ((0 + ((uknots[span + 1] - u) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))))) / (uknots[span + 1] - u + (u - uknots[span + (-1)])))) + ((uknots[span + 2] - u) * ((u - uknots[span + 0]) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))) / (uknots[span + 2] - u + (u - uknots[span + 0]))))) * p[span - degree + 1, 1] + (u - uknots[span + 0]) * ((u - uknots[span + 0]) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))) / (uknots[span + 2] - u + (u - uknots[span + 0]))) * p[span - degree + 2, 1];
            double w = 0 + (0 + ((uknots[span + 1] - u) * ((0 + ((uknots[span + 1] - u) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))))) / (uknots[span + 1] - u + (u - uknots[span + (-1)]))))) * p[span - degree + 0, 2] + (((u - uknots[span + (-1)]) * ((0 + ((uknots[span + 1] - u) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))))) / (uknots[span + 1] - u + (u - uknots[span + (-1)])))) + ((uknots[span + 2] - u) * ((u - uknots[span + 0]) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))) / (uknots[span + 2] - u + (u - uknots[span + 0]))))) * p[span - degree + 1, 2] + (u - uknots[span + 0]) * ((u - uknots[span + 0]) * (1 / (uknots[span + 1] - u + (u - uknots[span + 0]))) / (uknots[span + 2] - u + (u - uknots[span + 0]))) * p[span - degree + 2, 2];
        }
        public static void DebugTestTorus()
        {
            // see https://www.geometrictools.com/Documentation/DistanceToCircle3.pdf
            GeoVector dirx = new GeoVector(1, 1, 2);
            GeoVector diry = GeoVector.ZAxis ^ dirx;
            GeoVector dirz = dirx ^ diry;
            GeoVector n = 15 * dirz.Normalized;
            ToroidalSurface ts = new ToroidalSurface(new GeoPoint(10, 20, 30), dirx.Normalized, diry.Normalized, dirz.Normalized, 15, 5);
            GeoPoint[] pnts = new GeoPoint[20];
            Random rnd = new Random(123);
            for (int i = 0; i < pnts.Length; i++)
            {
                GeoPoint2D uv = new GeoPoint2D(rnd.NextDouble() * 2 * Math.PI, rnd.NextDouble() * 2 * Math.PI);
                pnts[i] = ts.PointAt(uv);
            }
            // parameters: 0:cx, 1:cy, 2: cz, 3: nx, 4: ny, 5: nz, 6: r (majorRadius is sqrt(n*n), r: minorRadius)
            void efunc(double[] parameters, out double[] values)
            {
                values = new double[pnts.Length];
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double nx = parameters[3];
                double ny = parameters[4];
                double nz = parameters[5];
                double r = parameters[6];
                double R2 = nx * nx + ny * ny + nz * nz;
                double R = Math.Sqrt(R2);
                for (int i = 0; i < pnts.Length; i++)
                {
                    GeoVector d = pnts[i] - new GeoPoint(cx, cy, cz);
                    values[i] = r * r - (sqr(nx * d.x + ny * d.y + nz * d.z) / R2 + sqr(Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z - sqr(nx * d.x + ny * d.y + nz * d.z) / R2) - R));
                }
            }
            void jfunc(double[] parameters, out Matrix derivs)
            {


                derivs = new Matrix(pnts.Length, 7); // Jacobi Matrix 
                double cx = parameters[0];
                double cy = parameters[1];
                double cz = parameters[2];
                double nx = parameters[3];
                double ny = parameters[4];
                double nz = parameters[5];
                double r = parameters[6];
                double nn = nz * nz + ny * ny + nx * nx;
                double rn = Math.Sqrt(nn);

                for (int i = 0; i < pnts.Length; i++)
                {
                    double px = pnts[i].x;
                    double py = pnts[i].y;
                    double pz = pnts[i].z;
                    double dx = px - cx;
                    double dy = py - cy;
                    double dz = pz - cz;
                    // manually extracted common subexpressions
                    double nd = nz * dz + ny * dy + nx * dx;
                    double dd = dz * dz + dy * dy + dx * dx;
                    double b = Math.Sqrt((-sqr(nd)) / nn + dd);
                    double a = b - Math.Sqrt(nn);
                    double ndnn = nd / nn;
                    double sndnn = sqr(nd) / sqr(nn);
                    derivs[i, 0] = 2.0 * nx * ndnn - (2.0 * nx * ndnn - 2.0 * dx) * a / b;
                    derivs[i, 1] = 2.0 * ny * ndnn - (2.0 * ny * ndnn - 2.0 * dy) * a / b;
                    derivs[i, 2] = 2.0 * nz * ndnn - (2.0 * nz * ndnn - 2.0 * dz) * a / b;
                    derivs[i, 3] = (-2.0) * ((nx * sndnn - dx * ndnn) / b - nx / rn) * a + 2.0 * nx * sndnn - 2.0 * dx * ndnn;
                    derivs[i, 4] = (-2.0) * ((ny * sndnn - dy * ndnn) / b - ny / rn) * a + 2.0 * ny * sndnn - 2.0 * dy * ndnn;
                    derivs[i, 5] = (-2.0) * ((nz * sndnn - dz * ndnn) / b - nz / rn) * a + 2.0 * nz * sndnn - 2.0 * dz * ndnn;
                    derivs[i, 6] = 2 * r;
                }
            }
#if MATHNET
            MathNet.Numerics.LinearAlgebra.Vector<double> oeFunc(MathNet.Numerics.LinearAlgebra.Vector<double> parameters, MathNet.Numerics.LinearAlgebra.Vector<double> x)
            {
                efunc(parameters.ToArray(), out double[] errorValues);
                return MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(errorValues);
            }
            MathNet.Numerics.LinearAlgebra.Matrix<double> ojfunc(MathNet.Numerics.LinearAlgebra.Vector<double> parameters, MathNet.Numerics.LinearAlgebra.Vector<double> x)
            {
                jfunc(parameters.ToArray(), out Matrix partialDerivations);
                var derivs = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(partialDerivations.RowCount, partialDerivations.ColumnCount);
                for (int i = 0; i < partialDerivations.RowCount; i++)
                {
                    for (int j = 0; j < partialDerivations.ColumnCount; j++)
                    {
                        derivs[i, j] = partialDerivations[i, j];
                    }
                }
                return derivs;
            }
            MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer minimizer = new MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer(0.001, 1e-20, 1e-20, 1e-20, 20);

            var obj = MathNet.Numerics.Optimization.ObjectiveFunction.NonlinearModel(oeFunc, ojfunc, MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(pnts.Length), MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(pnts.Length));
            var lmresult = minimizer.FindMinimum(obj, MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(new double[] { 0, 0, 0, 0, 0, 1, 1 }));

#endif
            GaussNewtonMinimizer gnm = new GaussNewtonMinimizer(efunc, jfunc);
            bool ok = gnm.Solve(new double[] { 11, 22, 33, -9, -9, 9, 6 }, 30, 1e-6, 1e-9, out double minError, out int numiter, out double[] result);
        }
        public static void ApproxDualSurfaceCurve(ICurve dsc)
        {
#if MATHNET
            GeoPoint[] pnts = new GeoPoint[24];
            for (int i = 0; i < pnts.Length; i++)
            {
                pnts[i] = dsc.PointAt((double)(i + 1) / (double)(pnts.Length + 1));
            }
            BSpline bsp = BSpline.Construct();
            GeoPoint[] poles = new GeoPoint[5];
            double[] weights = new double[5];
            poles[0] = dsc.PointAt(0.0);
            poles[4] = dsc.PointAt(1.0);
            weights[0] = weights[4] = 1.0;
            MathNet.Numerics.LinearAlgebra.Vector<double> oeFunc(MathNet.Numerics.LinearAlgebra.Vector<double> parameters, MathNet.Numerics.LinearAlgebra.Vector<double> x)
            {   // parameters pole[1] and pole[2] (including weights) of a NURBS curve
                poles[1] = new GeoPoint(parameters[0], parameters[1], parameters[2]);
                poles[2] = new GeoPoint(parameters[4], parameters[5], parameters[6]);
                poles[3] = new GeoPoint(parameters[8], parameters[9], parameters[10]);
                weights[1] = parameters[3];
                weights[2] = parameters[7];
                weights[3] = parameters[11];
                bsp.SetData(4, poles, weights, new double[] { 0, 1 }, new int[] { 5, 5 }, false);
                double[] errorValues = new double[pnts.Length];
                for (int i = 0; i < pnts.Length; i++)
                {
                    // errorValues[i] = pnts[i] | (bsp as ICurve).PointAt((double)(i + 1) / (double)(pnts.Length + 1));
                    errorValues[i] = (bsp as ICurve).DistanceTo(pnts[i]);
                }
                double sum = 0.0;
                for (int i = 0; i < errorValues.Length; i++)
                {
                    System.Diagnostics.Trace.Write(errorValues[i].ToString() + " ");
                    sum += errorValues[i] * errorValues[i];
                }
                System.Diagnostics.Trace.WriteLine(sum.ToString());

                return MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(errorValues);
            }
            MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer minimizer = new MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer(0.1, 1e-20, 1e-20, 1e-20, 1000);

            var obj = MathNet.Numerics.Optimization.ObjectiveFunction.NonlinearModel(oeFunc, MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(pnts.Length), MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(pnts.Length));
            Geometry.DistLL(poles[0], dsc.StartDirection, poles[3], dsc.EndDirection, out double par1, out double par2);
            bsp.ThroughPoints(new GeoPoint[] { dsc.PointAt(0.0), dsc.PointAt(0.25), dsc.PointAt(0.5), dsc.PointAt(0.75), dsc.PointAt(1.0) }, 4, false);
            GeoPoint pl1 = bsp.Poles[1];
            GeoPoint pl2 = bsp.Poles[2];
            GeoPoint pl3 = bsp.Poles[3];

            var lmresult = minimizer.FindMinimum(obj, MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(new double[] { pl1.x, pl1.y, pl1.z, 1.0, pl2.x, pl2.y, pl2.z, 1.0, pl3.x, pl3.y, pl3.z, 1.0 }));

            poles[1] = new GeoPoint(lmresult.MinimizingPoint[0], lmresult.MinimizingPoint[1], lmresult.MinimizingPoint[2]);
            poles[2] = new GeoPoint(lmresult.MinimizingPoint[4], lmresult.MinimizingPoint[5], lmresult.MinimizingPoint[6]);
            poles[3] = new GeoPoint(lmresult.MinimizingPoint[8], lmresult.MinimizingPoint[9], lmresult.MinimizingPoint[10]);
            weights[1] = lmresult.MinimizingPoint[3];
            weights[2] = lmresult.MinimizingPoint[7];
            weights[3] = lmresult.MinimizingPoint[11];
            bsp.SetData(4, poles, weights, new double[] { 0, 1 }, new int[] { 5, 5 }, false);

#endif
        }
        private static double SignedDistance(ICurve2D crv, GeoPoint2D p)
        {   // signed distance
            double pos = crv.PositionOf(p);
            GeoPoint2D footPoint = crv.PointAt(pos);
            GeoVector2D dir = crv.DirectionAt(pos).Normalized;
            return Geometry.DistPL(p, footPoint, dir);
        }
        public static void ApproxCurve2D(ICurve2D crv)
        {
#if MATHNET
            //if (crv == null)
            //{
            //    crv = BSpline2D.MakeCircle(new GeoPoint2D(0, 0), 1);
            //    Polynom[] nrbspl = (crv as BSpline2D).Nurbs.CurvePointPolynom(0.125);
            //    crv = crv.Trim(0.0, 0.25);
            //}
            // Funktioniert zwar, konvergiert aber oft nicht oder nur schlecht
            int deg = 4;
            int numPnts = deg * 3; // number of parameters
            GeoPoint2D[] pnts = new GeoPoint2D[numPnts];
            for (int i = 0; i < pnts.Length; i++)
            {
                pnts[i] = crv.PointAt((double)(i + 1) / (double)(pnts.Length + 1));
            }
            GeoPoint2D[] poles = new GeoPoint2D[deg + 1];
            double[] weights = new double[deg + 1];
            poles[0] = crv.PointAt(0.0);
            poles[poles.Length - 1] = crv.PointAt(1.0);
            weights[0] = weights[weights.Length - 1] = 1.0;
            MathNet.Numerics.LinearAlgebra.Vector<double> oeFunc(MathNet.Numerics.LinearAlgebra.Vector<double> parameters, MathNet.Numerics.LinearAlgebra.Vector<double> x)
            {   // parameters pole[1] and pole[2] (including weights) of a NURBS curve
                for (int i = 0; i < deg - 1; i++)
                {
                    poles[i + 1] = new GeoPoint2D(parameters[3 * i], parameters[3 * i + 1]);
                    weights[i + 1] = parameters[3 * i + 2];
                }
                BSpline2D bsppar = new BSpline2D(poles, weights, new double[] { 0, 1 }, new int[] { deg + 1, deg + 1 }, deg, false, 0.0, 1.0);
                double[] errorValues = new double[pnts.Length];
                for (int i = 0; i < pnts.Length; i++)
                {
                    // errorValues[i] = pnts[i] | (bsp as ICurve).PointAt((double)(i + 1) / (double)(pnts.Length + 1));
                    errorValues[i] = SignedDistance(bsppar, pnts[i]);
                }
                double sum = 0.0;
                for (int i = 0; i < errorValues.Length; i++)
                {
                    System.Diagnostics.Trace.Write(errorValues[i].ToString() + " ");
                    sum += errorValues[i] * errorValues[i];
                }
                System.Diagnostics.Trace.WriteLine(sum.ToString());

                return MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(errorValues);
            }
            MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer minimizer = new MathNet.Numerics.Optimization.LevenbergMarquardtMinimizer(0.1, 1e-20, 1e-20, 1e-20, 1000);

            var obj = MathNet.Numerics.Optimization.ObjectiveFunction.NonlinearModel(oeFunc, MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(pnts.Length), MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(pnts.Length));
            GeoPoint2D[] throughpoints = new GeoPoint2D[deg + 1];
            for (int i = 0; i < throughpoints.Length; i++)
            {
                throughpoints[i] = crv.PointAt((double)i / (double)(throughpoints.Length - 1));
            }
            BSpline2D bsp = new BSpline2D(throughpoints, deg, false);
            MathNet.Numerics.LinearAlgebra.Vector<double> initialGuess = MathNet.Numerics.LinearAlgebra.CreateVector.Dense<double>(3 * (deg - 1));
            for (int i = 0; i < deg - 1; i++)
            {
                initialGuess[3 * i] = bsp.Poles[i + 1].x;
                initialGuess[3 * i + 1] = bsp.Poles[i + 1].y;
                initialGuess[3 * i + 2] = 1.0; //weight
            }
            var lmresult = minimizer.FindMinimum(obj, initialGuess);

            for (int i = 0; i < deg - 1; i++)
            {
                poles[i + 1] = new GeoPoint2D(lmresult.MinimizingPoint[3 * i], lmresult.MinimizingPoint[3 * i + 1]);
                weights[i + 1] = lmresult.MinimizingPoint[3 * i + 2];
            }
            bsp = new BSpline2D(poles, weights, new double[] { 0, 1 }, new int[] { deg + 1, deg + 1 }, deg, false, 0.0, 1.0);

#endif
        }
        public static void TestCylCylIntersection()
        {
            // using this Maxima input:
            // k1: 2.34314575050762
            // k2: 1.37258300203048
            // k3: 0.970562748477142
            // k4: 3.31370849898476
            // /*unit circle, first quadrant (0: [0,1]:*/
            // k1: sqrt(2)-2;
            // k2: sqrt(2)-1;
            // w: 1 + k1*u -k1*u^2;
            // cy1x: x = (1+k1*u-k2*u^2)/w;
            // cy1y: y = (sqrt(2)*u-k2*u^2) / w;
            // /* this defines the unit cylinder */
            // xo: 
            // fco(xo,yo):= xo^2 + yo^2 - r^2; /* cylinder around z-axis, radius r */
            // fco(x+dx,c*y-s*z); /* moved dx to right and rotated around x-axis */
            // solve([cy1x,cy1y,fco(x+dx,c*y-s*z)],[x,y,z]); /* x,y,z depending on u */
            //stringout("C:/Temp/cylcyl.txt", values);

            // it turns out that the general form of a quadrant of a cylinder/cylinder intersection should be something like
            // x = (sqrt(a*u4 + b*u³ + c*u² +d) + e*u² + f*u +g)/(h*u²+i*u+j);
            // same for y and z which means 3*10 unknowns

            // let's try with r=2, dx=2.5

            double sqrt2 = Math.Sqrt(2.0);
            GeoPoint[] pnts = new GeoPoint[100];
            double dx = 2.5;
            double r = 2;
            double s = Math.Sin(Math.PI / 4.0);
            double c = Math.Cos(Math.PI / 4.0);
            for (int i = 0; i < 100; i++)
            {
                double u = i * 0.01;
                double x = ((sqrt2 - 1) * u * u + (2 - sqrt2) * u - 1) / ((sqrt2 - 2) * u * u + (2 - sqrt2) * u - 1);
                double y = ((sqrt2 - 1) * u * u - sqrt2 * u) / ((sqrt2 - 2) * u * u + (2 - sqrt2) * u - 1);
                double z = (Math.Sqrt(((9232.0 * sqr(r) + Math.Sqrt(2.0) * ((-6528.0) * sqr(r) + 6528.0 * sqr(dx) + 9232.0 * dx + 3264.0) - 9232.0 * sqr(dx) - 13056.0 * dx - 4616.0) * Math.Pow(u, 16.0) + (Math.Sqrt(2.0) * (52224.0 * sqr(r) - 52224.0 * sqr(dx) - 77680.0 * dx - 28816.0) - 73856.0 * sqr(r) + 73856.0 * sqr(dx) + 109856.0 * dx + 40752.0) * Math.Pow(u, 15.0) + (280128.0 * sqr(r) + Math.Sqrt(2.0) * ((-198080.0) * sqr(r) + 198080.0 * sqr(dx) + 308016.0 * dx + 119320.0) - 280128.0 * sqr(dx) - 435600.0 * dx - 168744.0) * Math.Pow(u, 14.0) + (Math.Sqrt(2.0) * (472640.0 * sqr(r) - 472640.0 * sqr(dx) - 764400.0 * dx - 307880.0) - 668416.0 * sqr(r) + 668416.0 * sqr(dx) + 1081024.0 * dx + 435408.0) * Math.Pow(u, 13.0) + (1122688.0 * sqr(r) + Math.Sqrt(2.0) * ((-793856.0) * sqr(r) + 793856.0 * sqr(dx) + 1329384.0 * dx + 554488.0) - 1122688.0 * sqr(dx) - 1880032.0 * dx - 784164.0) * Math.Pow(u, 12.0) + (Math.Sqrt(2.0) * (995008.0 * sqr(r) - 995008.0 * sqr(dx) - 1718472.0 * dx - 739544.0) - 1407168.0 * sqr(r) + 1407168.0 * sqr(dx) + 2430288.0 * dx + 1045872.0) * Math.Pow(u, 11.0) + (1361248.0 * sqr(r) + Math.Sqrt(2.0) * ((-962528.0) * sqr(r) + 962528.0 * sqr(dx) + 1708448.0 * dx + 756000.0) - 1361248.0 * sqr(dx) - 2416120.0 * dx - 1069144.0) * Math.Pow(u, 10.0) + (Math.Sqrt(2.0) * (732960.0 * sqr(r) - 732960.0 * sqr(dx) - 1332736.0 * dx - 604480.0) - 1036608.0 * sqr(r) + 1036608.0 * sqr(dx) + 1884800.0 * dx + 854864.0) * Math.Pow(u, 9.0) + (627976.0 * sqr(r) + Math.Sqrt(2.0) * ((-444000.0) * sqr(r) + 444000.0 * sqr(dx) + 824580.0 * dx + 382200.0) - 627976.0 * sqr(dx) - 1166184.0 * dx - 540518.0) * Math.Pow(u, 8.0) + (Math.Sqrt(2.0) * (214656.0 * sqr(r) - 214656.0 * sqr(dx) - 406044.0 * dx - 191788.0) - 303648.0 * sqr(r) + 303648.0 * sqr(dx) + 574312.0 * dx + 271244.0) * Math.Pow(u, 7.0) + (116816.0 * sqr(r) + Math.Sqrt(2.0) * ((-82544.0) * sqr(r) + 82544.0 * sqr(dx) + 158620.0 * dx + 76142.0) - 116816.0 * sqr(dx) - 224420.0 * dx - 107706.0) * Math.Pow(u, 6.0) + (Math.Sqrt(2.0) * (24976.0 * sqr(r) - 24976.0 * sqr(dx) - 48636.0 * dx - 23666.0) - 35392.0 * sqr(r) + 35392.0 * sqr(dx) + 68880.0 * dx + 33500.0) * Math.Pow(u, 5.0) + (8288.0 * sqr(r) + Math.Sqrt(2.0) * ((-5824.0) * sqr(r) + 5824.0 * sqr(dx) + 11466.0 * dx + 5642.0) - 8288.0 * sqr(dx) - 16296.0 * dx - 8009.0) * Math.Pow(u, 4.0) + (Math.Sqrt(2.0) * (1008.0 * sqr(r) - 1008.0 * sqr(dx) - 2002.0 * dx - 994.0) - 1456.0 * sqr(r) + 1456.0 * sqr(dx) + 2884.0 * dx + 1428.0) * Math.Pow(u, 3.0) + (184.0 * sqr(r) + Math.Sqrt(2.0) * ((-120.0) * sqr(r) + 120.0 * sqr(dx) + 240.0 * dx + 120.0) - 184.0 * sqr(dx) - 366.0 * dx - 182.0) * sqr(u) + (Math.Sqrt(2.0) * (8.0 * sqr(r) - 8.0 * sqr(dx) - 16.0 * dx - 8.0) - 16.0 * sqr(r) + 16.0 * sqr(dx) + 32.0 * dx + 16.0) * u + sqr(r) - sqr(dx) - 2.0 * dx - 1.0)) + (17.0 * Math.Pow(2.0, (3.0 / 2.0)) * c - 48.0 * c) * Math.Pow(u, 8.0) + (172.0 * c - 61.0 * Math.Pow(2.0, (3.0 / 2.0)) * c) * Math.Pow(u, 7.0) + (3.0 * Math.Pow(2.0, (13.0 / 2.0)) * c - 270.0 * c) * Math.Pow(u, 6.0) + (240.0 * c - 43.0 * Math.Pow(2.0, (5.0 / 2.0)) * c) * Math.Pow(u, 5.0) + (95.0 * Math.Sqrt(2.0) * c - 130.0 * c) * Math.Pow(u, 4.0) + (42.0 * c - 33.0 * Math.Sqrt(2.0) * c) * Math.Pow(u, 3.0) + (7.0 * Math.Sqrt(2.0) * c - 7.0 * c) * sqr(u) - Math.Sqrt(2.0) * c * u) / ((3.0 * Math.Pow(2.0, (9.0 / 2.0)) * s - 68.0 * s) * Math.Pow(u, 8.0) + (272.0 * s - 3.0 * Math.Pow(2.0, (13.0 / 2.0)) * s) * Math.Pow(u, 7.0) + (43.0 * Math.Pow(2.0, (7.0 / 2.0)) * s - 488.0 * s) * Math.Pow(u, 6.0) + (512.0 * s - 45.0 * Math.Pow(2.0, (7.0 / 2.0)) * s) * Math.Pow(u, 5.0) + (15.0 * Math.Pow(2.0, (9.0 / 2.0)) * s - 344.0 * s) * Math.Pow(u, 4.0) + (152.0 * s - 13.0 * Math.Pow(2.0, (7.0 / 2.0)) * s) * Math.Pow(u, 3.0) + (7.0 * Math.Pow(2.0, (5.0 / 2.0)) * s - 44.0 * s) * sqr(u) + (8.0 * s - Math.Pow(2.0, (5.0 / 2.0)) * s) * u - s);
                if (double.IsNaN(z)) z = 0.0;
                pnts[i] = new GeoPoint(x, y, z);
            }

            //double k1 = 2.34314575050762;
            //double k2 = 1.37258300203048;
            //double k3 = 0.970562748477142;
            //double k4 = 3.31370849898476;

            //double u0 = (-(Math.Sqrt(((sqr(k2) + 4.0 * k1 * k2) * sqr(r) + (4.0 * k1 * k3 + (2.0 * dx + 2.0) * sqr(k2) + ((8.0 * dx + 4.0) * k1) * k2) * r + ((4.0 * dx + 4.0) * k1) * k3 + (sqr(dx) + 2.0 * dx + 1.0) * sqr(k2) + ((4.0 * sqr(dx) + 4.0 * dx) * k1) * k2)) + k2 * r + (dx + 1.0) * k2)) / (2.0 * k2 * r + 2.0 * k3 + 2.0 * dx * k2);
            //double u1 = (Math.Sqrt(((sqr(k2) + 4.0 * k1 * k2) * sqr(r) + (4.0 * k1 * k3 + (2.0 * dx + 2.0) * sqr(k2) + ((8.0 * dx + 4.0) * k1) * k2) * r + ((4.0 * dx + 4.0) * k1) * k3 + (sqr(dx) + 2.0 * dx + 1.0) * sqr(k2) + ((4.0 * sqr(dx) + 4.0 * dx) * k1) * k2)) - k2 * r + (-dx - 1.0) * k2) / (2.0 * k2 * r + 2.0 * k3 + 2.0 * dx * k2);
            //double u2 = (Math.Sqrt(((sqr(k2) + 4.0 * k1 * k2) * sqr(r) + (((-4.0) * k1) * k3 + ((-2.0) * dx - 2.0) * sqr(k2) + (((-8.0) * dx - 4.0) * k1) * k2) * r + ((4.0 * dx + 4.0) * k1) * k3 + (sqr(dx) + 2.0 * dx + 1.0) * sqr(k2) + ((4.0 * sqr(dx) + 4.0 * dx) * k1) * k2)) - k2 * r + (dx + 1.0) * k2) / (2.0 * k2 * r - 2.0 * k3 - 2.0 * dx * k2);
            //double u3 = (-(Math.Sqrt(((sqr(k2) + 4.0 * k1 * k2) * sqr(r) + (((-4.0) * k1) * k3 + ((-2.0) * dx - 2.0) * sqr(k2) + (((-8.0) * dx - 4.0) * k1) * k2) * r + ((4.0 * dx + 4.0) * k1) * k3 + (sqr(dx) + 2.0 * dx + 1.0) * sqr(k2) + ((4.0 * sqr(dx) + 4.0 * dx) * k1) * k2)) + k2 * r + (-dx - 1.0) * k2)) / (2.0 * k2 * r - 2.0 * k3 - 2.0 * dx * k2);

            //u1 += 1e-6;
            //u3 -= 1e-6;
            //u1 = 0.0;
            //u3 = 0.25;
            //double du = (u3 - u1) / pnts.Length;
            //for (int i = 0; i < pnts.Length; i++)
            //{
            //    double u = u1 + i * du;
            //    double x = (k3 * sqr(u) + k2 * u - k1) / (k2 * sqr(u) + k2 * u - k1);
            //    double y = (k3 * sqr(u) - k4 * u) / (k2 * sqr(u) + k2 * u - k1);
            //    double z = (Math.Sqrt(((sqr(k2) * (sqr(r) - sqr(dx)) - sqr(k3) - 2.0 * dx * k2 * k3) * Math.Pow(u, 4.0) + (sqr(k2) * (2.0 * sqr(r) - 2.0 * sqr(dx) - 2.0 * dx) + (((-2.0) * dx - 2.0) * k2) * k3) * Math.Pow(u, 3.0) + (sqr(k2) * (sqr(r) - sqr(dx) - 2.0 * dx - 1.0) + k1 * k2 * ((-2.0) * sqr(r) + 2.0 * sqr(dx) + 2.0 * dx) + ((2.0 * dx + 2.0) * k1) * k3) * sqr(u) + (k1 * k2 * ((-2.0) * sqr(r) + 2.0 * sqr(dx) + 4.0 * dx + 2.0)) * u + sqr(k1) * (sqr(r) - sqr(dx) - 2.0 * dx - 1.0))) + c * k3 * sqr(u) - c * k4 * u) / (k2 * s * sqr(u) + k2 * s * u - k1 * s);
            //    pnts[i] = new GeoPoint(x, y, z);
            //}
        }
#endif
    }
}
