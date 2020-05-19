﻿using CADability.Curve2D;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CADability.GeoObject
{

    public class SurfaceOfRevolutionException : ApplicationException
    {
        public SurfaceOfRevolutionException(string msg) : base(msg) { }
    }
    [Serializable()]
    public class SurfaceOfRevolution : ISurfaceImpl, ISurfaceOfRevolution, ISerializable, IExportStep
    {
        private double curveStartParameter, curveEndParameter;
        private double curveParameterOffset;
        private ModOp toSurface; // diese ModOp modifiziert die 2d Kurve, die um die y-Achse rotiert wird, in die 3d Kurve
        private ModOp fromSurface; // invers zu toSurface
        private ICurve2D basisCurve2D; // die Basiskurve im 2D

        // ACHTUNG: Achse und Kurve müssen in einer Ebene liegen, damit dieses Objekt mit seinem 
        // OCas Partner kompatibel ist!
        internal SurfaceOfRevolution(ICurve2D basisCurve2D, ModOp toSurface, double curveStartParameter, double curveEndParameter, double curveParameterOffset) : base()
        {
            this.basisCurve2D = basisCurve2D;
            this.toSurface = toSurface;
            this.fromSurface = toSurface.GetInverse();
            this.curveStartParameter = curveStartParameter;
            this.curveEndParameter = curveEndParameter;
            this.curveParameterOffset = curveParameterOffset;
        }
        public SurfaceOfRevolution(ICurve basisCurve, GeoPoint axisLocation, GeoVector axisDirection, double curveStartParameter, double curveEndParameter, GeoPoint? axisRefPoint = null) : base()
        {
            this.curveEndParameter = curveEndParameter;
            this.curveStartParameter = curveStartParameter;
            // curveStartParameter und curveEndParameter haben folgenden Sinn: 
            // der Aufrufer werwartet ein bestimmtes u/v System. 
            // in u ist es die Kreisbewegung, in v ist es die Kurve (andersrum als bei SurfaceOfLinearExtrusion)
            // curveStartParameter und curveEndParameter definieren die Lage von v, die Lage von u ist durch die Ebene
            // bestimmt
            // finden der Hauptebene, in der alles stattfindet. Die ist gegeben durch die Achse und die Kurve
            Plane pln;
            if (axisRefPoint.HasValue)
            {
                GeoPoint cnt = Geometry.DropPL(axisRefPoint.Value, axisLocation, axisDirection);
                pln = new Plane(axisLocation, axisRefPoint.Value - cnt, axisDirection);
            }
            else
            {
                double ds = Geometry.DistPL(basisCurve.StartPoint, axisLocation, axisDirection);
                double de = Geometry.DistPL(basisCurve.EndPoint, axisLocation, axisDirection);
                if (ds > de && ds > Precision.eps)
                {
                    GeoPoint cnt = Geometry.DropPL(basisCurve.StartPoint, axisLocation, axisDirection);
                    pln = new Plane(axisLocation, basisCurve.StartPoint - cnt, axisDirection);
                }
                else if (de >= ds && de > Precision.eps)
                {
                    GeoPoint cnt = Geometry.DropPL(basisCurve.EndPoint, axisLocation, axisDirection);
                    pln = new Plane(axisLocation, basisCurve.EndPoint - cnt, axisDirection);
                }
                else if (basisCurve.GetPlanarState() == PlanarState.Planar)
                {
                    Plane ppln = basisCurve.GetPlane();
                    GeoVector dirx = ppln.Normal ^ axisDirection; // richtig rum?
                    pln = new Plane(axisLocation, dirx, axisDirection);
                }
                else if (basisCurve.GetPlanarState() == PlanarState.UnderDetermined)
                {
                    pln = new Plane(axisLocation, basisCurve.StartDirection, axisDirection);
                }
                else
                {   // es könnte sein, dass die ganze Fläche singulär ist
                    // dann kann man nichts machen, sollte aber nicht vorkommen
                    throw new SurfaceOfRevolutionException("Surface is invalid");
                }
            }
            // pln hat jetzt den Ursprung bei axisLocation, die y-Achse ist axisDirection und die x-Achse durch die
            // Kurve gegeben
            // die Orientierung scheint mir noch fraglich, wierum wird gedreht, wierum läuft u
            basisCurve2D = basisCurve.GetProjectedCurve(pln);
            // ACHTUNG "Parameterproblem": durch die Projektion der Kurve verschiebt sich bei Kreisen der Parameterraum
            // da 2D Kreise bzw. Bögen keine Achse haben und sich immer auf die X-Achse beziehen
            if (basisCurve2D is Arc2D)
            {
                curveParameterOffset = (basisCurve2D as Arc2D).StartParameter - this.curveStartParameter;
            }
            else if (basisCurve2D is BSpline2D)
            {   // beim BSpline fängt der Parameter im 3D bei knots[0] an, welches gleich this.curveStartParameter ist
                // curveParameterOffset = this.curveStartParameter;
                // wenn man einen geschlossenen Spline um eine Achse rotieren lässt
                // dann entsteht mit obiger Zeile ein Problem, mit dieser jedoch nicht:
                curveParameterOffset = (basisCurve2D as BSpline2D).Knots[0];
            }
            else
            {
                curveParameterOffset = 0.0;
            }
            toSurface = ModOp.Fit(new GeoPoint[] { GeoPoint.Origin, GeoPoint.Origin + GeoVector.XAxis, GeoPoint.Origin + GeoVector.YAxis },
                                  new GeoPoint[] { axisLocation, axisLocation + pln.DirectionX, axisLocation + pln.DirectionY }, false);
            fromSurface = toSurface.GetInverse();
        }
        /// <summary>
        /// Returns the location of the axis of revolution
        /// </summary>
        public GeoPoint Location
        {
            get
            {
                return toSurface * GeoPoint.Origin;
            }
        }
        /// <summary>
        /// Returns the direction of the axis of revolution
        /// </summary>
        public GeoVector Axis
        {
            get
            {
                return toSurface * GeoVector.YAxis;
            }
        }
        /// <summary>
        /// Returns the 0 position of the revolution
        /// </summary>
        public GeoVector XAxis
        {
            get
            {
                return toSurface * GeoVector.XAxis;
            }
        }
        /// <summary>
        /// Returns the curve that is rotated to form this surface
        /// </summary>
        public ICurve BasisCurve
        {
            get
            {
                IGeoObject go = basisCurve2D.MakeGeoObject(Plane.XYPlane);
                go.Modify(toSurface);
                return go as ICurve;
            }
        }
        #region ISurfaceImpl overrides
        private double GetPos(double v)
        {   // wandelt v in einen Wert zwischen 0 und 1 um
            return (v - curveStartParameter) / (curveEndParameter - curveStartParameter);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.PointAt (GeoPoint2D)"/>
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override GeoPoint PointAt(GeoPoint2D uv)
        {
            double pos = GetPos(uv.y);
            GeoPoint2D p2d = basisCurve2D.PointAt(pos);
            ModOp rot = ModOp.Rotate(1, (SweepAngle)uv.x);
            return toSurface * rot * p2d;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.PositionOf (GeoPoint)"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public override GeoPoint2D PositionOf(GeoPoint p)
        {
            GeoPoint unit = fromSurface * p;
            double u = Math.Atan2(-unit.z, unit.x);
            if (u < 0.0) u += 2 * Math.PI;
            double v = basisCurve2D.PositionOf(new GeoPoint2D(Math.Sqrt(unit.z * unit.z + unit.x * unit.x), unit.y));
            double x = basisCurve2D.PointAt(v).x;
            v = v * (curveEndParameter - curveStartParameter) + curveStartParameter;
            // it does not take into account that the curve might cross the axis, in which case it is ambiguous between u and u+pi
            //GeoPoint2D dbg = base.PositionOf(p); // Prüfung bestanden
            //GeoPoint dbg1 = PointAt(dbg);
            //if ((new GeoPoint2D(u, v) | dbg) > 1e-3)
            //{
            //    double d = (new GeoPoint2D(u + 2 * Math.PI, v) | dbg);
            //}
            return new GeoPoint2D(u, v);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.UDirection (GeoPoint2D)"/>
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override GeoVector UDirection(GeoPoint2D uv)
        {   // geht ohne Rückgriff auf die Kurve
            GeoVector dir = new GeoVector(-Math.Sin(uv.x), 0.0, -Math.Cos(uv.x));
            double pos = GetPos(uv.y);
            GeoPoint2D p2d = basisCurve2D.PointAt(pos);
            return p2d.x * (toSurface * dir); // so müsste auch die Länge OK sein, oder?
            // "p2d.x *" am 26.10.16 eingefügt: die Länge der Richtung ist proportional zum Radius, sonst geht "NewtonLineintersection" nicht
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.VDirection (GeoPoint2D)"/>
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override GeoVector VDirection(GeoPoint2D uv)
        {
            double pos = GetPos(uv.y);
            GeoVector2D dir = basisCurve2D.DirectionAt(pos);
            //dir = (1.0 / (curveEndParameter - curveStartParameter)) * dir; // dir ist die Änderung um 1 also volle Kurvenlänge
            // 15.8.17: die Skalierung von dir ist wohl falsch: NewtonLineintersection läuft mit obiger Zeile nicht richtig, so aber perfekt
            ModOp rot = ModOp.Rotate(1, (SweepAngle)uv.x);
            return toSurface * rot * dir;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.GetNormal (GeoPoint2D)"/>
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override GeoVector GetNormal(GeoPoint2D uv)
        {
            try
            {
                return UDirection(uv).Normalized ^ VDirection(uv).Normalized;
            }
            catch (GeoVectorException)
            {
                // vermutlixh ein Pol bei uv.y
                double pos = GetPos(uv.y);
                GeoPoint2D p2d = basisCurve2D.PointAt(pos);
                return VDirection(new GeoPoint2D(0.0, uv.y)).Normalized ^ VDirection(new GeoPoint2D(Math.PI / 2.0, uv.y)).Normalized;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.GetZMinMax (Projection, double, double, double, double, ref double, ref double)"/>
        /// </summary>
        /// <param name="p"></param>
        /// <param name="umin"></param>
        /// <param name="umax"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        /// <param name="zMin"></param>
        /// <param name="zMax"></param>
        public override void GetZMinMax(Projection p, double umin, double umax, double vmin, double vmax, ref double zMin, ref double zMax)
        {
            // hier wird brutal gerastert, das muss anders gehen!
            // nämlich so: 1. betrachte die 4 Eckpunkte
            // 2. finde u Werte für für das z-Maximum bzw z-Minimum der Rotation
            // 3. bestimme die Meridiankurve in diesen beiden Positionen und projiziere gemäß "p"
            // 4. finde die z-Extrema dieser projizierten Kurve
            for (int i = 0; i <= 10; ++i)
            {
                for (int j = 0; j <= 10; ++j)
                {
                    GeoPoint bp = p.UnscaledProjection * PointAt(new GeoPoint2D(umin + i * (umax - umin) / 10, vmin + j * (vmax - vmin) / 10));
                    zMin = Math.Min(zMin, bp.z);
                    zMax = Math.Max(zMax, bp.z);
                }
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.Make3dCurve (ICurve2D)"/>
        /// </summary>
        /// <param name="curve2d"></param>
        /// <returns></returns>
        public override ICurve Make3dCurve(ICurve2D curve2d)
        {
            if (curve2d is Curve2DAspect)
            {
                ICurve res = (curve2d as Curve2DAspect).Get3DCurve(this);
                if (res != null) return res;
            }
            if (curve2d is Line2D)
            {
                Line2D l2d = curve2d as Line2D;
                GeoVector2D dir = l2d.EndPoint - l2d.StartPoint;
                if (Math.Abs(dir.x) < Precision.eps)
                {   // die Basiskurve selbst, da nur v läuft
                    ModOp rot = ModOp.Rotate(1, (SweepAngle)l2d.StartPoint.x);
                    IGeoObject go = basisCurve2D.MakeGeoObject(Plane.XYPlane);
                    go.Modify(toSurface * rot);
                    ICurve res = go as ICurve;
                    double pos1 = GetPos(l2d.StartPoint.y);
                    double pos2 = GetPos(l2d.EndPoint.y);
                    if (pos2 < pos1)
                    {   // läuft von "oben nach unten"
                        res.Reverse();
                        res.Trim(1.0 - pos1, 1.0 - pos2);
                        return res;
                    }
                    else
                    {
                        res.Trim(pos1, pos2);
                        return res;
                    }
                }
                else if (Math.Abs(dir.y) < Precision.eps)
                {
                    // ein Kreisbogen
                    // die Orientierung ist ausprobiert, so dass sie stimmt, ist aber nicht unbedingt logisch so...
                    double pos = GetPos(l2d.StartPoint.y);
                    GeoPoint2D p2d = basisCurve2D.PointAt(pos); // der Punkt auf der Kurve
                    Ellipse e = Ellipse.Construct();
                    double r = Math.Abs(p2d.x);
                    Plane pln = new Plane(Plane.XZPlane, -p2d.y);
                    e.SetPlaneRadius(pln, r, r);
                    e.StartParameter = -l2d.StartPoint.x;
                    if (p2d.x < 0.0) e.StartParameter += Math.PI;
                    SweepAngle sw = l2d.StartPoint.x - l2d.EndPoint.x; // Vorzeichen müsste korrekt sein
                    e.SweepParameter = sw;
                    e.Modify(toSurface);
                    return e;
                }
            }
            if (curve2d is Polyline2D)
            {
                Polyline2D p2d = curve2d as Polyline2D;
                GeoPoint[] throughtpoints = new GeoPoint[p2d.VertexCount];
                for (int i = 0; i < throughtpoints.Length; ++i)
                {
                    throughtpoints[i] = PointAt(p2d.GetVertex(i));
                }
                BSpline res = BSpline.Construct();
                res.ThroughPoints(throughtpoints, 3, false);
                return res;
            }
            return base.Make3dCurve(curve2d);
        }
        public override void GetNaturalBounds(out double umin, out double umax, out double vmin, out double vmax)
        {
            umin = 0.0;
            umax = Math.PI * 2.0;
            vmin = curveStartParameter;
            vmax = curveEndParameter;
        }
        public override ICurve2D GetProjectedCurve(ICurve curve, double precision)
        {
            return new ProjectedCurve(curve, this, true, usedArea); // works also with empty usedArea
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.GetPlaneIntersection (PlaneSurface, double, double, double, double, double)"/>
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="umin"></param>
        /// <param name="umax"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public override IDualSurfaceCurve[] GetPlaneIntersection(PlaneSurface pl, double umin, double umax, double vmin, double vmax, double precision)
        {
            if (Precision.SameDirection(fromSurface * pl.Normal, GeoVector.YAxis, false))
            {   // Schnitte senkrecht zur Achse liefern Kreise
                GeoPoint2D loc = fromSurface.Project(pl.Location); // ein Punkt der 2D Linie
                BoundingRect br = basisCurve2D.GetExtent();
                double xmax = Math.Max(Math.Abs(br.Left), Math.Abs(br.Right)) * 2;
                GeoPoint2DWithParameter[] ips = basisCurve2D.Intersect(new GeoPoint2D(-xmax, loc.y), new GeoPoint2D(xmax, loc.y));
                List<IDualSurfaceCurve> res = new List<IDualSurfaceCurve>();
                for (int i = 0; i < ips.Length; ++i)
                {
                    double v = ips[i].par1 * (curveEndParameter - curveStartParameter) + curveStartParameter;
                    Line2D l1 = new Line2D(new GeoPoint2D(umin, v), new GeoPoint2D(umax, v));
                    ICurve c3d = Make3dCurve(l1);
                    ICurve2D c2d = pl.GetProjectedCurve(c3d, precision);
                    res.Add(new DualSurfaceCurve(c3d, this, l1, pl, c2d));
                }
                return res.ToArray();
            }
            else
            {
                PlaneSurface plu = pl.GetModified(fromSurface) as PlaneSurface; // im Unit system
                if (Math.Abs(plu.Plane.Distance(GeoPoint.Origin)) < precision && Precision.IsPerpendicular(plu.Plane.Normal, GeoVector.YAxis, false))
                {   // Ebene durch die Achse
                    Plane rp = new Plane(GeoPoint.Origin, GeoVector.YAxis ^ plu.Plane.Normal, GeoVector.YAxis);
                    IGeoObject go = basisCurve2D.MakeGeoObject(rp);
                    go.Modify(toSurface);
                    ICurve c3d = go as ICurve;
                    List<IDualSurfaceCurve> res = new List<IDualSurfaceCurve>();
                    double u = Math.Atan2(rp.DirectionX.z, rp.DirectionX.x);
                    SurfaceHelper.AdjustUPeriodic(this, umin, umax, ref u);
                    if (u >= umin && u <= umax)
                    {
                        Line2D l1 = new Line2D(new GeoPoint2D(u, vmin), new GeoPoint2D(u, vmax));
                        ICurve2D c2d = pl.GetProjectedCurve(c3d, precision);
                        res.Add(new DualSurfaceCurve(c3d, this, l1, pl, c2d));
                    }
                    double u1 = u + Math.PI;
                    SurfaceHelper.AdjustUPeriodic(this, umin, umax, ref u1);
                    if (u1 >= umin && u1 <= umax)
                    {
                        Line2D l1 = new Line2D(new GeoPoint2D(u1, vmin), new GeoPoint2D(u1, vmax));
                        c3d = c3d.CloneModified(ModOp.Rotate(this.Axis, SweepAngle.Opposite));
                        ICurve2D c2d = pl.GetProjectedCurve(c3d, precision);
                        res.Add(new DualSurfaceCurve(c3d, this, l1, pl, c2d));
                    }
                    return res.ToArray();
                }
            }
            return base.GetPlaneIntersection(pl, umin, umax, vmin, vmax, precision);
        }
        public override IDualSurfaceCurve[] GetDualSurfaceCurves(BoundingRect thisBounds, ISurface other, BoundingRect otherBounds, List<GeoPoint> seeds)
        {
            if (other is PlaneSurface)
            {
                return GetPlaneIntersection(other as PlaneSurface, thisBounds.Left, thisBounds.Right, thisBounds.Bottom, thisBounds.Top, Precision.eps);
            }
            return base.GetDualSurfaceCurves(thisBounds, other, otherBounds, seeds);
        }
        //public GeoPoint[] GetPlaneIntersection(PlaneSurface pl)
        //{
        //    GeoVector b = pl.Normal;
        //    if (Math.Abs((new SweepAngle(Axis, b)) % (2 * Math.PI)) < Precision.eps)
        //    {

        //    }
        //    GeoVector a = GeoVector.YAxis;
        //    GeoPoint loc = pl.Location;
        //    loc = fromSurface * loc;
        //    b = fromSurface * b;
        //    SweepAngle ang = new SweepAngle(GeoVector.XAxis, b);
        //    ModOp rot = ModOp.Rotate(1, ang);
        //    ModOp rotInv = ModOp.Rotate(1, -ang);
        //    b = rot * b;
        //    GeoVector x = a ^ (a ^ b);
        //    GeoPoint2D[] hp = GetLineIntersection(loc, x);
        //    GeoPoint[] erg = new GeoPoint[hp.Length];
        //    for (int i = 0; i < hp.Length; ++i)
        //    {
        //        erg[i] = toSurface * rotInv * PointAt(hp[i]);
        //    }
        //    return erg;
        //}
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.Clone ()"/>
        /// </summary>
        /// <returns></returns>
        public override ISurface Clone()
        {
            return new SurfaceOfRevolution(basisCurve2D.Clone(), toSurface, curveStartParameter, curveEndParameter, curveParameterOffset);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.Modify (ModOp)"/>
        /// </summary>
        /// <param name="m"></param>
        public override void Modify(ModOp m)
        {
            boxedSurfaceEx = null;
            toSurface = m * toSurface;
            fromSurface = toSurface.GetInverse();
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.GetModified (ModOp)"/>
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public override ISurface GetModified(ModOp m)
        {
            ISurface res = Clone();
            res.Modify(m);
            return res;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.CopyData (ISurface)"/>
        /// </summary>
        /// <param name="CopyFrom"></param>
        public override void CopyData(ISurface CopyFrom)
        {
            SurfaceOfRevolution cc = CopyFrom as SurfaceOfRevolution;
            if (cc != null)
            {
                this.basisCurve2D = cc.basisCurve2D.Clone();
                this.toSurface = cc.toSurface;
                this.fromSurface = cc.fromSurface;
                this.curveStartParameter = cc.curveStartParameter;
                this.curveEndParameter = cc.curveEndParameter;
            }
        }
        public override bool IsUPeriodic
        {
            get
            {
                return true;
            }
        }
        public override bool IsVPeriodic
        {
            get
            {
                return basisCurve2D.IsClosed && Math.Abs(curveEndParameter - curveStartParameter) > 1 - 1e-6;
            }
        }
        public override double UPeriod
        {
            get
            {
                return Math.PI * 2.0;
            }
        }
        public override double VPeriod
        {
            get
            {
                if (IsVPeriodic) return Math.Abs(curveEndParameter - curveStartParameter);
                else return 0.0;
            }
        }
        public class HyperbolaHelp : GeneralCurve2D
        {
            public double a, b, yoffset, tmin, tmax, ymin, ymax;
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.Clone ()"/>
            /// </summary>
            /// <returns></returns>
            public override ICurve2D Clone()
            {
                throw new Exception("The method or operation is not implemented.");
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.Copy (ICurve2D)"/>
            /// </summary>
            /// <param name="toCopyFrom"></param>
            public override void Copy(ICurve2D toCopyFrom)
            {
                throw new Exception("The method or operation is not implemented.");
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.Split (double)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override ICurve2D[] Split(double Position)
            {
                throw new Exception("The method or operation is not implemented.");
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.PointAt (double)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override GeoPoint2D PointAt(double Position)
            {
                return new GeoPoint2D(a * Math.Cosh(tmin + Position * (tmax - tmin)), b * Math.Sinh(tmin + Position * (tmax - tmin)) + yoffset);
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.PositionOf (GeoPoint2D)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override double PositionOf(GeoPoint2D Position)
            {
                double d = Position.y / b;
                double t = Math.Log(d + Math.Sqrt(d * d + 1));
                return (t - tmin) / (tmax - tmin);
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.DirectionAt (double)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override GeoVector2D DirectionAt(double Position)
            {
                return new GeoVector2D(a * Math.Sinh(tmin + Position * (tmax - tmin)), b * Math.Cosh(tmin + Position * (tmax - tmin)));
            }
            public HyperbolaHelp(GeoPoint sp, GeoVector dir, double ymin, double ymax)
            {   // sp & dir must be in the unit system, ymin & ymax the extent of the curve in the unit system
                double par1, par2;
                a = Geometry.DistLLWrongPar2(sp, dir, GeoPoint.Origin, GeoVector.YAxis, out par1, out par2);
                yoffset = par2;
                GeoPoint test = sp;
                if ((Geometry.DistPL(sp, GeoPoint.Origin, GeoVector.YAxis) - a) < Precision.eps)
                    test += dir;
                double xquad = test.x * test.x + test.z * test.z;
                double y = test.y - yoffset;
                b = y * a / (xquad - a * a);
                double tminhelp = (ymin - yoffset) / b;
                tmin = Math.Log(tminhelp + Math.Sqrt(tminhelp * tminhelp + 1));
                double tmaxhelp = (ymax - yoffset) / b;
                tmax = Math.Log(tmaxhelp + Math.Sqrt(tmaxhelp * tmaxhelp + 1));
                this.ymin = ymin;
                this.ymax = ymax;
            }
        }
        public class Hyperbola : GeneralCurve2D
        {
            public double yoffset, b, tmax, tmin;
            public HyperbolaHelp h;
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.Clone ()"/>
            /// </summary>
            /// <returns></returns>
            public override ICurve2D Clone()
            {
                throw new Exception("The method or operation is not implemented.");
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.Copy (ICurve2D)"/>
            /// </summary>
            /// <param name="toCopyFrom"></param>
            public override void Copy(ICurve2D toCopyFrom)
            {
                throw new Exception("The method or operation is not implemented.");
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.Split (double)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override ICurve2D[] Split(double Position)
            {
                throw new Exception("The method or operation is not implemented.");
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.PointAt (double)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override GeoPoint2D PointAt(double Position)
            {
                GeoPoint2D help = h.PointAt(Position);
                return new GeoPoint2D(help.x, help.y + yoffset);
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.PositionOf (GeoPoint2D)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override double PositionOf(GeoPoint2D Position)
            {
                double d = Position.y / b;
                double t = Math.Log(d + Math.Sqrt(d * d + 1));
                return (t - tmin) / (tmax - tmin);
            }
            /// <summary>
            /// Overrides <see cref="CADability.Curve2D.GeneralCurve2D.DirectionAt (double)"/>
            /// </summary>
            /// <param name="Position"></param>
            /// <returns></returns>
            public override GeoVector2D DirectionAt(double Position)
            {
                double help = yoffset / b;
                return h.DirectionAt(Position + Math.Log(help + Math.Sqrt(help * help + 1)));
            }
            public Hyperbola(GeoPoint sp, GeoVector dir, double ymin, double ymax)
            {   // sp & dir must be in the unit system, ymin & ymax the extent of the curve in the unit system
                h = new HyperbolaHelp(sp, dir, ymin, ymax);
                yoffset = h.yoffset;
                b = h.b;
                tmax = h.tmax;
                tmin = h.tmin;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.GetLineIntersection (GeoPoint, GeoVector)"/>
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override GeoPoint2D[] GetLineIntersection(GeoPoint startPoint, GeoVector direction)
        {
            if (Precision.SameDirection(Axis, direction, false))
            {   // Linie parallel zur Achse
                GeoPoint p = fromSurface * startPoint;
                double u = Math.Atan2(-p.z, p.x); // analog PositionOf
                if (u < 0.0) u += 2 * Math.PI;
                double d = Math.Sqrt(p.z * p.z + p.x * p.x); // Abstand der Linie von der Achse
                BoundingRect ext = basisCurve2D.GetExtent();
                GeoPoint2DWithParameter[] isp = basisCurve2D.Intersect(new GeoPoint2D(d, ext.Bottom), new GeoPoint2D(d, ext.Top));
                GeoPoint2D[] res = new GeoPoint2D[isp.Length];
                for (int i = 0; i < isp.Length; ++i)
                {
                    res[i] = new GeoPoint2D(u, curveStartParameter + isp[i].par1 * (curveEndParameter - curveStartParameter));
#if DEBUG
                    GeoPoint dbg = PointAt(res[i]);
                    DebuggerContainer dc = new DebuggerContainer();
                    Line l = Line.Construct();
                    l.SetTwoPoints(startPoint, startPoint + 10 * direction);
                    dc.Add(l);
                    Point pnt = Point.Construct();
                    pnt.Symbol = PointSymbol.Circle;
                    pnt.Location = dbg;
                    dc.Add(pnt);
#endif
                }
                return res;
            }
            if (basisCurve2D is Circle2D)
            {
                // für Kreise und Kreisbögen können wir selbst rechnen:
                // wir betrachten einen Torus um die Z Achse in der X/Y Ebene. Deshalb müssen wir y und z vertauschen
                // und in y versetzen
                Circle2D c2d = basisCurve2D as Circle2D;
                startPoint = fromSurface * startPoint;
                direction = fromSurface * direction;
                startPoint.y -= c2d.Center.y;
                double tmp = startPoint.y;
                startPoint.y = startPoint.z;
                startPoint.z = tmp;
                tmp = direction.y;
                direction.y = direction.z;
                direction.z = tmp;
                GeoPoint[] isp = Geometry.IntersectTorusLine(c2d.Radius, Math.Abs(c2d.Center.x), startPoint, direction);
                GeoPoint2D[] res = new GeoPoint2D[isp.Length];
                for (int i = 0; i < isp.Length; ++i)
                {
                    // wieder zurückübertragen
                    tmp = isp[i].y;
                    isp[i].y = isp[i].z;
                    isp[i].z = tmp;
                    isp[i].y += c2d.Center.y;
                    res[i] = PositionOf(toSurface * isp[i]);
                }
                return res;
            }
            //return base.GetLineIntersection(startPoint, direction);
            else
            {
                GeoPoint2D[] res = BoxedSurfaceEx.GetLineIntersection(startPoint, direction);
#if DEBUG
                DebuggerContainer dc = new DebuggerContainer();
                GeoPoint[] dbg = new GeoPoint[res.Length];
                for (int i = 0; i < res.Length; ++i)
                {
                    dbg[i] = PointAt(res[i]);
                    GeoVector ud = UDirection(res[i]);
                    GeoVector vd = VDirection(res[i]);
                    Line lu = Line.Construct();
                    lu.SetTwoPoints(dbg[i], dbg[i] + 20 * ud.Normalized);
                    Line lv = Line.Construct();
                    lv.SetTwoPoints(dbg[i], dbg[i] + 20 * vd.Normalized);
                    dc.Add(lu);
                    dc.Add(lv);
                }

#endif
                return res;
                // return base.GetLineIntersection(startPoint, direction); // bleibt auch hängen
                // Richards Code bleibt manchmal hängen
                GeoPoint sp = fromSurface * startPoint;
                GeoVector dir = fromSurface * direction;

                double par1, par2;
                if (Geometry.DistLLWrongPar2(sp, dir, GeoPoint.Origin, new GeoVector(0, 1, 0), out par1, out par2) < Precision.eps)
                {
                    GeoPoint2DWithParameter[] old1 = basisCurve2D.Intersect(new GeoPoint2D(Math.Sqrt(sp.x * sp.x + sp.z * sp.z), sp.y),
                        new GeoPoint2D(Math.Sqrt((sp.x + dir.x) * (sp.x + dir.x) + (sp.z + dir.z) * (sp.z + dir.z)), sp.y + dir.y));
                    GeoPoint2D[] New1 = new GeoPoint2D[old1.Length];
                    for (int i = 0; i < old1.Length; ++i)
                        New1[i] = old1[i].p;
                    GeoPoint2DWithParameter[] old2 = basisCurve2D.Intersect(new GeoPoint2D(Math.Sqrt(sp.x * sp.x + sp.z * sp.z), sp.y),
                        new GeoPoint2D(Math.Sqrt((sp.x + dir.x) * (sp.x + dir.x) + (sp.z + dir.z) * (sp.z + dir.z)), sp.y + dir.y));
                    GeoPoint2D[] New = new GeoPoint2D[old1.Length + old2.Length];
                    for (int i = 0; i < old1.Length; ++i)
                        New[i] = old1[i].p;
                    for (int i = 0; i < old2.Length; ++i)
                        New[i + old1.Length] = old2[i].p;
                    return New;
                }

                if (Math.Abs(dir.y) < Precision.eps)
                {
                    GeoPoint2DWithParameter[] old = basisCurve2D.Intersect(
                        new GeoPoint2D(0, sp.y), new GeoPoint2D(1, sp.y));
                    GeoPoint2D[] New = new GeoPoint2D[2 * old.Length];
                    for (int i = 0; i < old.Length; ++i)
                        New[i] = old[i].p;
                    for (int i = 0; i < old.Length; ++i)
                        New[i + old.Length] = new GeoPoint2D(-1 * old[i].p.x, old[i].p.y);
                    return New;
                }

                if (Math.Abs(dir.x) < Precision.eps && Math.Abs(dir.z) < Precision.eps)
                {
                    GeoPoint2DWithParameter[] old1 = basisCurve2D.Intersect(
                        new GeoPoint2D(Math.Sqrt(sp.x * sp.x + sp.z * sp.z), 0),
                        new GeoPoint2D(Math.Sqrt(sp.x * sp.x + sp.z * sp.z), 1));
                    GeoPoint2DWithParameter[] old2 = basisCurve2D.Intersect(
                        new GeoPoint2D(-Math.Sqrt(sp.x * sp.x + sp.z * sp.z), 0),
                        new GeoPoint2D(-Math.Sqrt(sp.x * sp.x + sp.z * sp.z), 1));
                    GeoPoint2D[] New = new GeoPoint2D[old1.Length + old2.Length];
                    for (int i = 0; i < old1.Length; ++i)
                        New[i] = old1[i].p;
                    for (int i = 0; i < old2.Length; ++i)
                        New[i + old1.Length] = old2[i].p;
                    return New;
                }

                BoundingCube bc = BasisCurve.GetExtent();
                ICurve2D c2 = new Hyperbola(sp, dir, bc.Ymin, bc.Ymax);

                GeoPoint2DWithParameter[] hps = c2.Intersect(basisCurve2D);
                GeoPoint2D[] hp = new GeoPoint2D[hps.Length];
                for (int i = 0; i < hps.Length; ++i)
                {
                    hp[i] = hps[i].p;
                }
                return hp;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.Intersect (ICurve, BoundingRect, out GeoPoint[], out GeoPoint2D[], out double[])"/>
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="uvExtent"></param>
        /// <param name="ips"></param>
        /// <param name="uvOnFaces"></param>
        /// <param name="uOnCurve3Ds"></param>
        public override void Intersect(ICurve curve, BoundingRect uvExtent, out GeoPoint[] ips, out GeoPoint2D[] uvOnFaces, out double[] uOnCurve3Ds)
        {
            if (curve.GetPlanarState() == PlanarState.Planar)
            {
                Plane pln = curve.GetPlane();
                if (Precision.SameDirection(pln.Normal, this.Axis, false))
                {
                    ICurve2D crv2d = curve.GetProjectedCurve(pln);
                    List<GeoPoint> lips = new List<GeoPoint>();
                    List<GeoPoint2D> luvOnFaces = new List<GeoPoint2D>();
                    List<double> luOnCurve3Ds = new List<double>();
                    double[] pars = BasisCurve.GetPlaneIntersection(pln);
                    for (int i = 0; i < pars.Length; i++)
                    {
                        GeoPoint2D pnt = pln.Project(BasisCurve.PointAt(pars[i]));
                        GeoPoint2D cnt = pln.Project(Location);
                        Circle2D circ2d = new Circle2D(cnt, cnt | pnt);
                        GeoPoint2DWithParameter[] ips2d = circ2d.Intersect(crv2d);
                        for (int j = 0; j < ips2d.Length; j++)
                        {
                            GeoPoint ip = pln.ToGlobal(ips2d[j].p);
                            double cu = curve.PositionOf(ip);
                            if (cu > -Precision.eps && cu < 1 + Precision.eps)
                            {
                                GeoPoint2D uv = PositionOf(ip);
                                SurfaceHelper.AdjustPeriodic(this, uvExtent, ref uv);
                                if (uvExtent.ContainsEps(uv, Precision.eps))
                                {
                                    lips.Add(ip);
                                    luvOnFaces.Add(uv);
                                    luOnCurve3Ds.Add(cu);
                                }
                            }
                        }
                    }
                    ips = lips.ToArray();
                    uvOnFaces = luvOnFaces.ToArray();
                    uOnCurve3Ds = luOnCurve3Ds.ToArray();
                    return;
                }
                else if (Precision.IsPerpendicular(pln.Normal, Axis, false) && Precision.IsPointOnPlane(Location, pln))
                {   // Ebene der Kurve geht durch Achse
                    ICurve crvnorm = curve.CloneModified(fromSurface); // die Kurve im System der Rotationsfläche: dort ist die Y-Achse die Achse und die basiscurve2d wird rotiert
                    Plane plcnorm = crvnorm.GetPlane();

                    Plane rp = new Plane(GeoPoint.Origin, GeoVector.YAxis ^ plcnorm.Normal, GeoVector.YAxis);
                    ICurve2D prc = crvnorm.GetProjectedCurve(rp); // das ist in die normierte Ebene projiziert, curve liegt auch in dieser
                    List<GeoPoint> lips = new List<GeoPoint>();
                    List<GeoPoint2D> luvOnFaces = new List<GeoPoint2D>();
                    List<double> luOnCurve3Ds = new List<double>();
                    GeoPoint2DWithParameter[] ips2d = basisCurve2D.Intersect(prc);
                    for (int j = 0; j < ips2d.Length; j++)
                    {
                        GeoPoint ip = toSurface * rp.ToGlobal(ips2d[j].p);
                        double cu = curve.PositionOf(ip);
                        if (cu > -Precision.eps && cu < 1 + Precision.eps)
                        {
                            GeoPoint2D uv = PositionOf(ip);
                            SurfaceHelper.AdjustPeriodic(this, uvExtent, ref uv);
                            if (uvExtent.ContainsEps(uv, Precision.eps))
                            {
                                lips.Add(ip);
                                luvOnFaces.Add(uv);
                                luOnCurve3Ds.Add(cu);
                            }
                        }
                    }
                    // Schnitt mit der anderen Seite, als basiscurve um 180 rotiert, in der selben Ebene
                    ips2d = basisCurve2D.GetModified(new ModOp2D(-1, 0, 0, 0, 1, 0)).Intersect(prc);
                    for (int j = 0; j < ips2d.Length; j++)
                    {
                        GeoPoint ip = toSurface * rp.ToGlobal(ips2d[j].p);
                        double cu = curve.PositionOf(ip);
                        if (cu > -Precision.eps && cu < 1 + Precision.eps)
                        {
                            GeoPoint2D uv = PositionOf(ip);
                            SurfaceHelper.AdjustPeriodic(this, uvExtent, ref uv);
                            if (uvExtent.ContainsEps(uv, Precision.eps))
                            {
                                lips.Add(ip);
                                luvOnFaces.Add(uv);
                                luOnCurve3Ds.Add(cu);
                            }
                        }
                    }
                    ips = lips.ToArray();
                    uvOnFaces = luvOnFaces.ToArray();
                    uOnCurve3Ds = luOnCurve3Ds.ToArray();
                    return;
                }
            }
            base.Intersect(curve, uvExtent, out ips, out uvOnFaces, out uOnCurve3Ds);
        }
        public override bool Oriented
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.Orientation (GeoPoint)"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public override double Orientation(GeoPoint p)
        {   // soll vermutlich feststellen ob außerhalb oder innerhalb der Fläche im Bezug auf die Achse
            // Richards code verwendet das aufwendige GetLineIntersection.
            // Hier die bessere Version, die das Problem ins zweidimensionale projiziert:
            GeoPoint up = fromSurface * p;
            double x = Math.Sqrt(up.z * up.z + up.x * up.x);
            double y = up.y;
            GeoPoint2D toCeck = new GeoPoint2D(x, y);
            BoundingRect ext = basisCurve2D.GetExtent();
            GeoPoint2D sp, ep;
            if (ext.Left > 0)
            {
                sp = new GeoPoint2D(ext.Left, y);
                ep = new GeoPoint2D(ext.Right, y);
            }
            else
            {
                sp = new GeoPoint2D(ext.Right, y);
                ep = new GeoPoint2D(ext.Left, y);
            }
            GeoPoint2DWithParameter[] isp = basisCurve2D.Intersect(sp, ep);
            // muss die Kurve immer rechts von der Achse liegen? Durch sp und ep sid wir davon unabhängig, es sei denn die Kurve schneidet die Achse
            SortedList<double, GeoPoint2D> par = new SortedList<double, GeoPoint2D>();
            for (int i = 0; i < isp.Length; ++i)
            {
                if (isp[i].par2 >= 0.0 && isp[i].par2 <= 1.0 && isp[i].par1 >= 0.0 && isp[i].par1 <= 1.0)
                {   // nur wenn echter Schnitt
                    par[isp[i].par2] = isp[i].p; // nach Position auf der horizontalen Linie
                }
            }
            if (par.Count == 0) return 0.0; // Seite kann nicht bestimmt werden, da völlig außerhalb. Das Vorzeichen von 0.0
            // ist aber verschieden von +1 und von -1, so dass wenn es andere konkrete Punkte gibt also ein Test gemacht wird
            double ppar = Geometry.LinePar(sp, ep, toCeck);
            for (int i = 0; i < par.Count; ++i)
            {
                if (ppar < par.Keys[i])
                {
                    if ((i & 1) == 0) // gerade, also innerhalb
                        return -(toCeck | par.Values[i]); // innerhalb ist negativ
                    else
                        return (toCeck | par.Values[i]); // außerhalb positiv
                }
            }
            // größer als alle
            if ((par.Count & 1) == 1) // ungerade, also letzer Punkt innerhalb
                return (toCeck | par.Values[par.Count - 1]); // außerhalb positiv
            else
                return -(toCeck | par.Values[par.Count - 1]); // innerhalb ist negativ


            // Richards code:
            //GeoPoint gp = fromSurface * p;
            //double d = gp.y;
            //double e = Math.Abs(d);
            //GeoPoint ac = new GeoPoint(0, d, 0);
            //GeoPoint2D[] sp = GetLineIntersection(p, toSurface * (ac - gp));
            //int n = 0;
            //for (int i = 0; i < sp.Length; ++i)
            //{
            //    double ds = (sp[i] - GeoPoint2D.Origin).Length - e;
            //    if (Math.Abs(ds) > Precision.eps)
            //    {
            //        if (ds < 0)
            //            n++;
            //    }
            //    else return 0;
            //}
            //if (n % 4 == 2)
            //    return (-e);
            //else
            //    return e;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.HitTest (BoundingCube, out GeoPoint2D)"/>
        /// </summary>
        /// <param name="bc"></param>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override bool HitTest(BoundingCube bc, out GeoPoint2D uv)
        {
            //  any vertex on the Surface?
            GeoPoint[] points = bc.Points;
            double[] ori = new double[8];
            for (int i = 0; i < 8; ++i)
            {
                GeoPoint g = points[i];
                double d = Orientation(g);
                if (d == 0)
                {
                    uv = PositionOf(g);
                    return true;
                }
                ori[i] = d;
            }

            //  any edge hitting the surface?
            int[,] l = bc.LineNumbers;
            for (int k = 0; k < 12; ++k)
            {
                int i = l[k, 0];
                int j = l[k, 1];
                GeoPoint a = points[i];
                GeoPoint b = points[j];
                double d = (b - a).Length;
                double e, f;
                GeoPoint2D[] sp = GetLineIntersection(a, b - a);
                for (int m = 0; m < sp.Length; ++m)
                {
                    GeoPoint g = PointAt(sp[m]);
                    e = (b - g).Length;
                    f = (g - a).Length;
                    if (Math.Abs(e + f - d) < Precision.eps)
                    {
                        uv = sp[m];
                        return true;
                    }
                }
                // folgendes tritt auf, ich weiß aber nicht warum. Vorerst auskommentiert
                // nochmal genauer untersuchen
                //if (ori[i] * ori[j] < 0)
                //    throw new Exception("this case should not happen");
            }

            ////  any face hitting the surface?
            //PlaneSurface[] planes = new PlaneSurface[6];
            //planes[0] = new PlaneSurface(new Plane(points[0], points[1], points[2]));
            //planes[1] = new PlaneSurface(new Plane(points[0], points[2], points[4]));
            //planes[2] = new PlaneSurface(new Plane(points[0], points[4], points[1]));
            //planes[3] = new PlaneSurface(new Plane(points[7], points[5], points[3]));
            //planes[4] = new PlaneSurface(new Plane(points[7], points[3], points[6]));
            //planes[5] = new PlaneSurface(new Plane(points[7], points[6], points[5]));
            //for (int i = 0; i < 6; ++i)
            //{
            //    IDualSurfaceCurve[] php = GetPlaneIntersection(planes[i], 0, 0, 0, 0, Precision.eps);
            //    for (int j = 0; j < php.Length; ++j)
            //    {
            //        GeoPoint hp = php[j].Curve3D.EndPoint;
            //        if (bc.IsOnBounds(hp, bc.Size * 1e-8))
            //        {
            //            uv = PositionOf(hp);
            //            return true;
            //        }
            //    }
            //    //GeoPoint[] hps = GetPlaneIntersection(planes[i]);
            //    //for (int j = 0; j < hps.Length; ++j)
            //    //{
            //    //    GeoPoint hp = hps[j];
            //    //    if(bc.IsOnBounds(hp, bc.Size * 1e-8))
            //    //    {
            //    //        uv = PositionOf(hp);
            //    //        return true;
            //    //    }
            //    //}
            //}

            //  complete Surface in the cube?
            uv = basisCurve2D.EndPoint;
            return (bc.Contains(PointAt(uv)));
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.ReverseOrientation ()"/>
        /// </summary>
        /// <returns></returns>
        public override ModOp2D ReverseOrientation()
        {   // umkehrung der Y-Richtung
            boxedSurfaceEx = null;
            //double tmp = curveStartParameter;
            //curveStartParameter = curveEndParameter;
            //curveEndParameter = tmp;
            basisCurve2D.Reverse(); // damit Helper das richtige erzeugt
            // die Frage ist natürlich: was macht reverse mit dem v-Parameter
            // bei BSpline bleiben sie erhalten
            // die basisCurve2D wird immer mit 0..1 angesprochen, insofern muss der v-Bereich umgeklappt werden
            // curveParameterOffset scheint nur von Bedeutung, wenn das Objekt gerade erzeugt wird, sonst wohl immer 0.0
            return new ModOp2D(1.0, 0.0, 0.0, 0.0, -1.0, curveStartParameter + curveEndParameter);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.FixedU (double, double, double)"/>
        /// </summary>
        /// <param name="u"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        /// <returns></returns>
        public override ICurve FixedU(double u, double vmin, double vmax)
        {
            if (vmin == vmax) return null;
            ICurve2D btr = basisCurve2D.Trim(GetPos(vmin), GetPos(vmax));
            ICurve b3d = btr.MakeGeoObject(Plane.XYPlane) as ICurve;
            ModOp rot = ModOp.Rotate(1, (SweepAngle)u);
            return b3d.CloneModified(toSurface * rot);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.FixedV (double, double, double)"/>
        /// </summary>
        /// <param name="v"></param>
        /// <param name="umin"></param>
        /// <param name="umax"></param>
        /// <returns></returns>
        public override ICurve FixedV(double v, double umin, double umax)
        {
            if (umin == umax) return null;
            GeoPoint2D p2d = basisCurve2D.PointAt(GetPos(v));
            Ellipse e = Ellipse.Construct();
            e.SetArcPlaneCenterRadiusAngles(new Plane(Plane.StandardPlane.XZPlane, -p2d.y), new GeoPoint(0, p2d.y, 0), p2d.x, -umin, -(umax - umin));
            e.Modify(toSurface);
            return e as ICurve;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.GetSafeParameterSteps (double, double, double, double, out double[], out double[])"/>
        /// </summary>
        /// <param name="umin"></param>
        /// <param name="umax"></param>
        /// <param name="vmin"></param>
        /// <param name="vmax"></param>
        /// <param name="intu"></param>
        /// <param name="intv"></param>
        public override void GetSafeParameterSteps(double umin, double umax, double vmin, double vmax, out double[] intu, out double[] intv)
        {   // leider gibt es die entsprechende Methode für die 2D Kurve nicht, erstmal so probieren

            if (basisCurve2D is GeneralCurve2D)
            {
                GeoPoint2D[] pnts;
                (basisCurve2D as GeneralCurve2D).GetTriangulationPoints(out pnts, out intv);
                List<double> usteps = new List<double>();
                List<double> vsteps = new List<double>();
                vsteps.Add(vmin);
                vsteps.Add(vmax);
                for (int i = 0; i < intv.Length; ++i)
                {

                    double d = curveStartParameter + intv[i] * (curveEndParameter - curveStartParameter);
                    if (d > vmin && d < vmax) vsteps.Add(d);
                }
                vsteps.Sort();
                for (int i = vsteps.Count - 1; i > 0; --i)
                {
                    if (vsteps.Count > 2 && vsteps[i] - vsteps[i - 1] < (curveEndParameter - curveStartParameter) * 1e-5)
                    {
                        if (i == 1) vsteps.RemoveAt(1);
                        else vsteps.RemoveAt(i - 1);
                    }
                }
                double udiff = umax - umin;
                int n = (int)Math.Ceiling(udiff / (Math.PI / 2.0)) + 1;
                double du = udiff / n;
                for (int i = 0; i < n; i++)
                {
                    usteps.Add(umin + i * du);
                }
                usteps.Add(umax);
                intu = usteps.ToArray();
                intv = vsteps.ToArray();
                return;
            }
            base.GetSafeParameterSteps(0, 2 * Math.PI, curveStartParameter, curveEndParameter, out intu, out intv);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.SameGeometry (BoundingRect, ISurface, BoundingRect, double, out ModOp2D)"/>
        /// </summary>
        /// <param name="thisBounds"></param>
        /// <param name="other"></param>
        /// <param name="otherBounds"></param>
        /// <param name="precision"></param>
        /// <param name="firstToSecond"></param>
        /// <returns></returns>
        public override bool SameGeometry(BoundingRect thisBounds, ISurface other, BoundingRect otherBounds, double precision, out ModOp2D firstToSecond)
        {
            if (other is SurfaceOfRevolution)
            {
                firstToSecond = ModOp2D.Null;
                SurfaceOfRevolution srother = other as SurfaceOfRevolution;
                bool reverse;
                if (!Curves.SameGeometry(BasisCurve, srother.BasisCurve, precision, out reverse)) return false;
                if ((Location | srother.Location) > precision) return false;
                GeoPoint2D uv1 = new GeoPoint2D(curveStartParameter, 0.0);
                GeoPoint p1 = PointAt(uv1);
                GeoPoint2D uv2 = srother.PositionOf(p1);
                GeoPoint p2 = srother.PointAt(uv2);
                if ((p1 | p2) > precision) return false;
                uv1 = new GeoPoint2D(curveEndParameter, 0.0);
                p1 = PointAt(uv1);
                uv2 = srother.PositionOf(p1);
                p2 = srother.PointAt(uv2);
                if ((p1 | p2) > precision) return false;
                firstToSecond = ModOp2D.Translate(uv2 - uv1);
                return true;
            }
            return base.SameGeometry(thisBounds, other, otherBounds, precision, out firstToSecond);
        }
        public override ISurface GetOffsetSurface(double offset)
        {
            if (basisCurve2D is Line2D || basisCurve2D is Arc2D)
            {
                ICurve2D parallel = basisCurve2D.Parallel(offset, true, Precision.eps, Math.PI);
                return new SurfaceOfRevolution(parallel, toSurface, curveStartParameter, curveEndParameter, curveParameterOffset);
            }
            ICurve2D c2dp = basisCurve2D.Parallel(offset, true, Precision.eps, Math.PI);
            if (c2dp != null) return new SurfaceOfRevolution(c2dp, toSurface, curveStartParameter, curveEndParameter, curveParameterOffset);
            return base.GetOffsetSurface(offset);
        }
        public override ISurface GetCanonicalForm(double precision, BoundingRect? bounds)
        {
            ICurve2D c2d = basisCurve2D;
            if (c2d is BSpline2D)
            {
                (c2d as BSpline2D).GetSimpleCurve(precision, out c2d);
            }
            if (c2d is Line2D)
            {
                GeoPoint2DWithParameter[] isp = c2d.Intersect(GeoPoint2D.Origin, new GeoPoint2D(0, 1));
                if (isp.Length == 1)
                {
                    GeoPoint apex = toSurface * new GeoPoint(isp[0].p);
                    Angle semiAngle = c2d.StartDirection.Angle;
                    if (semiAngle > Math.PI) semiAngle = 2 * Math.PI - semiAngle; // now 0..PI
                    if (semiAngle > Math.PI / 2.0) semiAngle = Math.PI - semiAngle; // now 0..PI/2
                    // we need the angle to the y axis:
                    semiAngle = Math.PI / 2 - semiAngle;
                    ConicalSurface cs = new ConicalSurface(apex, toSurface * GeoVector.XAxis, toSurface * GeoVector.ZAxis, toSurface * GeoVector.YAxis, semiAngle, 0);
                    GeoVector n1 = GetNormal(new GeoPoint2D(0, 0.5));
                    GeoPoint2D ncs = cs.PositionOf(PointAt(new GeoPoint2D(0, 0.5)));
                    GeoVector n2 = cs.GetNormal(ncs);
                    if (n1 * n2 < 0) cs.ReverseOrientation();
                    return cs;
                }
            }
            if (c2d is Circle2D) // Arc2D is derived from Circle2D
            {
                GeoPoint2D cnt2d = (c2d as Circle2D).Center;
                if (Math.Abs(cnt2d.x)<precision)
                {
                    double r = (c2d as Circle2D).Radius;
                    SphericalSurface ss = new SphericalSurface(toSurface * new GeoPoint(cnt2d), toSurface * (r * GeoVector.XAxis), toSurface * (r * GeoVector.ZAxis), toSurface * (r * GeoVector.YAxis));
                    return ss;
                }
                else
                {   // make a toriodal surface

                }
            }
            return base.GetCanonicalForm(precision, bounds);
        }
        #endregion
        #region IShowProperty Members
        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.Added (IPropertyTreeView)"/>
        /// </summary>
        /// <param name="propertyTreeView"></param>
        public override void Added(IPropertyPage propertyTreeView)
        {
            base.Added(propertyTreeView);
            resourceId = "SurfaceOfRevolution";
        }
        public override ShowPropertyEntryType EntryType
        {
            get
            {
                return ShowPropertyEntryType.GroupTitle;
            }
        }
        public override int SubEntriesCount
        {
            get
            {
                return SubEntries.Length;
            }
        }
        private IShowProperty[] subEntries;
        public override IShowProperty[] SubEntries
        {
            get
            {
                if (subEntries == null)
                {
                    List<IShowProperty> se = new List<IShowProperty>();
                    subEntries = se.ToArray();
                }
                return subEntries;
            }
        }
        #endregion
        #region ISerializable Members
        protected SurfaceOfRevolution(SerializationInfo info, StreamingContext context) : base()
        {
            basisCurve2D = info.GetValue("BasisCurve2D", typeof(ICurve2D)) as ICurve2D;
            toSurface = (ModOp)info.GetValue("ToSurface", typeof(ModOp));
            fromSurface = toSurface.GetInverse(); // müsste eigentlich da sein, da nur struct
            curveStartParameter = (double)info.GetValue("CurveStartParameter", typeof(double));
            curveEndParameter = (double)info.GetValue("CurveEndParameter", typeof(double));
            try
            {
                curveParameterOffset = (double)info.GetValue("CurveParameterOffset", typeof(double));
            }
            catch (SerializationException)
            {
                curveParameterOffset = 0;
            }
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("BasisCurve2D", basisCurve2D, typeof(ICurve2D));
            info.AddValue("ToSurface", toSurface, typeof(ModOp));
            info.AddValue("CurveStartParameter", curveStartParameter, typeof(double));
            info.AddValue("CurveEndParameter", curveEndParameter, typeof(double));
            info.AddValue("CurveParameterOffset", curveParameterOffset, typeof(double));
        }

        int IExportStep.Export(ExportStep export, bool topLevel)
        {
            IGeoObject go = basisCurve2D.MakeGeoObject(Plane.XYPlane);
            go.Modify(toSurface);
            int nc = (go as IExportStep).Export(export, false);
            GeoPoint axisLocation = toSurface * GeoPoint.Origin;
            GeoVector axisDirection = toSurface * GeoVector.YAxis;
            int na = export.WriteAxis1Placement3d(axisLocation, axisDirection);
            return export.WriteDefinition("SURFACE_OF_REVOLUTION('',#" + nc.ToString() + ",#" + na.ToString() + ")");
        }
        #endregion
        #region ISurfaceOfRevolution Members
        Axis ISurfaceOfRevolution.Axis
        {
            get
            {
                return new Axis(Location, Axis);
            }
        }
        ICurve ISurfaceOfRevolution.Curve
        {
            get
            {
                return BasisCurve;
            }
        }
        #endregion
    }
}
