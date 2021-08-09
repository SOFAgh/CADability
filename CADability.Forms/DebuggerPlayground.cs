using CADability.Attribute;
using CADability.Curve2D;
using CADability.GeoObject;
using CADability.Shapes;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CADability.Forms
{
    /// <summary>
    /// This class is not part of CADability OpenSource. I use it to write test-code for debugging
    /// </summary>
    internal class DebuggerPlayground : ICommandHandler
    {
        private IFrame frame;
        public DebuggerPlayground(IFrame frame)
        {
            this.frame = frame;
        }
        public static MenuWithHandler[] Connect(IFrame frame, MenuWithHandler[] mainMenu)
        {
            MenuWithHandler newItem = new MenuWithHandler();
            newItem.ID = "DebuggerPlayground.Debug";
            newItem.Text = "Debug";
            newItem.Target = new DebuggerPlayground(frame);
            List<MenuWithHandler> tmp = new List<MenuWithHandler>(mainMenu);
            tmp.Add(newItem);
            return tmp.ToArray();
        }

        public bool OnCommand(string MenuId)
        {
            if (MenuId == "DebuggerPlayground.Debug")
            {
                //string[] allFiles = Directory.GetFiles("C:/Zeichnungen/DxfDwg");
                //for (int i = 0; i < allFiles.Length; i++)
                //{
                //    if (allFiles[i].EndsWith(".dxf") || allFiles[i].EndsWith(".DXF"))
                //    {
                //        try
                //        {
                //            Project read = Project.ReadFromFile(allFiles[i], "dxf");
                //        } catch (Exception e)
                //        {
                //            System.Diagnostics.Trace.WriteLine("Read dxf exception: " + e.Message);
                //        }
                //    }
                //}
                //Unfold u = new Unfold(frame.Project);
                //GeoObjectList result = u.GetResult();
                //return true;
            }
            return false;
        }
        private void TestCylinderNP()
        {
            Model model = frame.Project.GetActiveModel();
            if (model[0] is Face fctor && fctor.Surface is ToroidalSurface ts)
            {
                Polynom plm = ts.GetImplicitPolynomial();
                double d = plm.Eval(ts.PointAt(new GeoPoint2D(1, 1)));
                GeoPoint p = ts.PointAt(new GeoPoint2D(1.5, 3.5));
                NonPeriodicSurface tsnp = new NonPeriodicSurface(ts, fctor.Domain);
                GeoPoint2D uv = tsnp.PositionOf(p);
                tsnp.Derivation2At(uv, out GeoPoint location, out GeoVector du, out GeoVector dv, out GeoVector duu, out GeoVector dvv, out GeoVector duv);
                ICurve fu = tsnp.FixedU(uv.x, -1, 1);
                ICurve fv = tsnp.FixedV(uv.y, -1, 1);
                Ellipse elliu = Ellipse.Construct();
                elliu.SetCirclePlaneCenterRadius(new Plane(location, du, duu), location + duu, duu.Length);
                Ellipse elliv = Ellipse.Construct();
                elliv.SetCirclePlaneCenterRadius(new Plane(location, dv, dvv), location + dvv, dvv.Length);
                Line lu = Line.MakeLine(location, location + du);
                Line lv = Line.MakeLine(location, location + dv);
                GeoObjectList dbg = new GeoObjectList(fctor, elliu, elliv, lu, lv, fu as IGeoObject, fv as IGeoObject);
            }
            if (model[0] is Face fccone && model[1] is Line line)
            {
                fccone.Surface.GetLineIntersection(line.StartPoint, line.EndPoint - line.StartPoint);
            }
            return;
            if (model[0] is Face fc1)
            {
                GeoObjectList dbgs = new GeoObjectList();
                BoundingRect ext = fc1.Area.GetExtent();
                GeoPoint2D dbgpos = fc1.Surface.PositionOf(fc1.Surface.PointAt(ext.GetCenter()));
                for (int i = 0; i < 10; i++)
                {
                    double u = ext.Left + i * ext.Width / 9.0;
                    double v = ext.Bottom + i * ext.Height / 9.0;
                    Line2D l2d = new Line2D(new GeoPoint2D(u, ext.Bottom), new GeoPoint2D(u, ext.Top));
                    double[] parts = fc1.Area.Clip(l2d, true);
                    for (int j = 0; j < parts.Length; j += 2)
                    {
                        dbgs.Add(fc1.Surface.Make3dCurve(l2d.Trim(parts[j], parts[j + 1])) as IGeoObject);
                    }
                    l2d = new Line2D(new GeoPoint2D(ext.Left, v), new GeoPoint2D(ext.Right, v));
                    parts = fc1.Area.Clip(l2d, true);
                    for (int j = 0; j < parts.Length; j += 2)
                    {
                        dbgs.Add(fc1.Surface.Make3dCurve(l2d.Trim(parts[j], parts[j + 1])) as IGeoObject);
                    }
                    Line l = Line.Construct();
                    GeoPoint2D uv = new GeoPoint2D(u, v);
                    GeoVector diru = fc1.Surface.UDirection(uv);
                    GeoVector dirv = fc1.Surface.VDirection(uv);
                    diru.Length = 0.1;
                    dirv.Length = 0.1;
                    l.SetTwoPoints(fc1.Surface.PointAt(uv), fc1.Surface.PointAt(uv) + diru);
                    dbgs.Add(l);
                    l = Line.Construct();
                    l.SetTwoPoints(fc1.Surface.PointAt(uv), fc1.Surface.PointAt(uv) + dirv);
                    dbgs.Add(l);
                }
            }
            GeoObjectList allSphericals = new GeoObjectList();
            foreach (IGeoObject go in model)
            {
                Shell sh = null;
                if (go is Solid sld) sh = sld.Shells[0];
                else if (go is Shell shell) sh = shell;
                else if (go is Face fc)
                {
                    sh = Shell.Construct();
                    sh.SetFaces(new Face[] { fc });
                }
                if (sh != null)
                {
                    foreach (Face face in sh.Faces)
                    {
                        if (face.Surface is CylindricalSurface cs)
                        {
                            List<ICurve> crvs = new List<ICurve>();
                            foreach (Edge edge in face.AllEdges)
                            {
                                if (edge.Curve3D != null)
                                {
                                    if (edge.Forward(face)) crvs.Add(edge.Curve3D.Clone());
                                    else
                                    {
                                        ICurve r = edge.Curve3D.Clone();
                                        r.Reverse();
                                        crvs.Add(r);
                                    }
                                }
                            }
                            CylindricalSurfaceNP csnp = new CylindricalSurfaceNP(cs.Location, cs.RadiusX, cs.ZAxis, cs.OutwardOriented, crvs.ToArray());
                            Face fc = Face.Construct();
                            Edge[][] edges = new Edge[face.HoleCount + 1][];
                            List<Edge> ledge = new List<Edge>();
                            for (int i = 0; i < face.OutlineEdges.Length; i++)
                            {
                                if (face.OutlineEdges[i].Curve3D != null)
                                {
                                    ICurve crv = face.OutlineEdges[i].Curve3D.Clone();
                                    if (!face.OutlineEdges[i].Forward(face)) crv.Reverse();
                                    Edge ne = new Edge(fc, crv, fc, csnp.GetProjectedCurve(crv, 0.0), true);
                                    ledge.Add(ne);
                                }
                                else { }
                            }
                            edges[0] = ledge.ToArray();
                            for (int j = 0; j < face.HoleCount; j++)
                            {
                                ledge.Clear();
                                for (int i = 0; i < face.HoleEdges(j).Length; i++)
                                {
                                    if (face.HoleEdges(j)[i].Curve3D != null)
                                    {
                                        ICurve crv = face.HoleEdges(j)[i].Curve3D.Clone();
                                        if (!face.HoleEdges(j)[i].Forward(face)) crv.Reverse();
                                        Edge ne = new Edge(fc, crv, fc, csnp.GetProjectedCurve(crv, 0.0), true);
                                        ledge.Add(ne);
                                    }
                                }
                                edges[j + 1] = ledge.ToArray();
                            }
                            fc.Set(csnp, edges);
                            allSphericals.Add(fc);
                            //for (int i = 0; i < 10; i++)
                            //{
                            //    double d = i / 10.0 - 0.5;
                            //    ICurve cu = ssnp.FixedU(d, -0.5, 0.5);
                            //    allSphericals.Add(cu as IGeoObject);
                            //    ICurve cv = ssnp.FixedV(d, -0.5, 0.5);
                            //    allSphericals.Add(cv as IGeoObject);
                            //}
                        }
                    }
                }
            }
        }
        private void TestSphericalNP()
        {
            Model model = frame.Project.GetActiveModel();
            GeoObjectList allSphericals = new GeoObjectList();
            foreach (IGeoObject go in model)
            {
                Shell sh = null;
                if (go is Solid sld) sh = sld.Shells[0];
                else if (go is Shell shell) sh = shell;
                else if (go is Face fc)
                {
                    sh = Shell.Construct();
                    sh.SetFaces(new Face[] { fc });
                }
                if (sh != null)
                {
                    foreach (Face face in sh.Faces)
                    {
                        if (face.Surface is SphericalSurface ss)
                        {
                            List<ICurve> crvs = new List<ICurve>();
                            foreach (Edge edge in face.AllEdges)
                            {
                                if (edge.Curve3D != null)
                                {
                                    if (edge.Forward(face)) crvs.Add(edge.Curve3D.Clone());
                                    else
                                    {
                                        ICurve r = edge.Curve3D.Clone();
                                        r.Reverse();
                                        crvs.Add(r);
                                    }
                                }
                            }
                            SphericalSurfaceNP ssnp = new SphericalSurfaceNP(ss.Location, ss.RadiusX, ss.OutwardOriented, crvs.ToArray());
                            Face fc = Face.Construct();
                            Edge[][] edges = new Edge[face.HoleCount + 1][];
                            List<Edge> ledge = new List<Edge>();
                            for (int i = 0; i < face.OutlineEdges.Length; i++)
                            {
                                if (face.OutlineEdges[i].Curve3D != null)
                                {
                                    ICurve crv = face.OutlineEdges[i].Curve3D.Clone();
                                    if (!face.OutlineEdges[i].Forward(face)) crv.Reverse();
                                    Edge ne = new Edge(fc, crv, fc, ssnp.GetProjectedCurve(crv, 0.0), true);
                                    ledge.Add(ne);
                                }
                                else { }
                            }
                            edges[0] = ledge.ToArray();
                            for (int j = 0; j < face.HoleCount; j++)
                            {
                                ledge.Clear();
                                for (int i = 0; i < face.HoleEdges(j).Length; i++)
                                {
                                    if (face.HoleEdges(j)[i].Curve3D != null)
                                    {
                                        ICurve crv = face.HoleEdges(j)[i].Curve3D.Clone();
                                        if (!face.HoleEdges(j)[i].Forward(face)) crv.Reverse();
                                        Edge ne = new Edge(fc, crv, fc, ssnp.GetProjectedCurve(crv, 0.0), true);
                                        ledge.Add(ne);
                                    }
                                }
                                edges[j + 1] = ledge.ToArray();
                            }
                            fc.Set(ssnp, edges);
                            allSphericals.Add(fc);
                            //for (int i = 0; i < 10; i++)
                            //{
                            //    double d = i / 10.0 - 0.5;
                            //    ICurve cu = ssnp.FixedU(d, -0.5, 0.5);
                            //    allSphericals.Add(cu as IGeoObject);
                            //    ICurve cv = ssnp.FixedV(d, -0.5, 0.5);
                            //    allSphericals.Add(cv as IGeoObject);
                            //}
                        }
                    }
                }
            }
        }

        private void TestBSpline()
        {
            GeoPoint[] pole = new GeoPoint[5];
            pole[0] = new GeoPoint(100, 100, 100);
            pole[1] = new GeoPoint(201, 201, 30);
            pole[2] = new GeoPoint(202, 302, 210);
            pole[3] = new GeoPoint(303, 303, 20);
            pole[4] = new GeoPoint(404, 204, 120);
            double[] weight = new double[] { 0.9, 0.8, 0.7, 0.6, 0.5 };
            double[] knots = new double[] { 0, 1 };
            BSpline bsp = BSpline.Construct();
            bsp.SetData(4, pole, weight, knots, new int[] { 5, 5 }, false);
            bsp.PointAtParam(0.2);
        }
        private void TestPoleRemoval()
        {
            Model model = frame.Project.GetActiveModel();

            //BSpline bsp = BSpline2D.MakeCircle(new GeoPoint2D(100, 100), 50).MakeGeoObject(Plane.XYPlane) as BSpline;
            //SurfaceOfLinearExtrusion sl = new SurfaceOfLinearExtrusion(bsp, GeoVector.ZAxis, 0.0, 1.0);
            //Face slface = Face.MakeFace(sl, new BoundingRect(0, 0, 1, 100));
            //model.Add(slface);
            foreach (IGeoObject go in model)
            {
                if (go is Face face)
                {
                    BoundingRect natBounds = new BoundingRect();
                    face.Surface.GetNaturalBounds(out natBounds.Left, out natBounds.Right, out natBounds.Bottom, out natBounds.Top);
                    NonPeriodicSurface nps = new NonPeriodicSurface(face.Surface, natBounds); // face.Domain);
                    GeoObjectList dbgc = new GeoObjectList();
                    ColorDef blue = new ColorDef("blue", System.Drawing.Color.Blue);
                    ColorDef green = new ColorDef("green", System.Drawing.Color.Green);
                    for (int i = 0; i < 11; i++)
                    {
                        nps.GetNaturalBounds(out double umin, out double umax, out double vmin, out double vmax);
                        Line2D fu = new Line2D(new GeoPoint2D(umin + i / 10.0 * (umax - umin), vmin), new GeoPoint2D(umin + i / 10.0 * (umax - umin), vmax));
                        Line2D fv = new Line2D(new GeoPoint2D(umin, vmin + i / 10.0 * (vmax - vmin)), new GeoPoint2D(umax, vmin + i / 10.0 * (vmax - vmin)));
                        double[] uparts = nps.Clip(fu);
                        double[] vparts = nps.Clip(fv);
                        for (int k = 0; k < uparts.Length; k += 2)
                        {
                            ICurve2D fup = fu.Trim(uparts[k], uparts[k + 1]);
                            ICurve crvu = nps.Make3dCurve(fup);
                            Polyline plu = Polyline.Construct();
                            GeoPoint[] pnts = new GeoPoint[51];
                            for (int j = 0; j < 51; j++)
                            {
                                pnts[j] = crvu.PointAt(j / 50.0);
                            }
                            try
                            {
                                plu.SetPoints(pnts, false);
                                plu.ColorDef = blue;
                                dbgc.Add(plu);
                            }
                            catch { }
                        }
                        for (int k = 0; k < vparts.Length; k += 2)
                        {
                            ICurve2D fvp = fv.Trim(vparts[k], vparts[k + 1]);
                            ICurve crvv = nps.Make3dCurve(fvp);
                            Polyline plv = Polyline.Construct();
                            GeoPoint[] pnts = new GeoPoint[51];
                            for (int j = 0; j < 51; j++)
                            {
                                pnts[j] = crvv.PointAt(j / 50.0);
                            }
                            try
                            {
                                plv.SetPoints(pnts, false);
                                plv.ColorDef = green;
                                dbgc.Add(plv);
                            }
                            catch { }
                        }
                    }
                    GeoVector diru = nps.UDirection(new GeoPoint2D(0.3, 0.7));
                    GeoVector diru1 = 100000 * (nps.PointAt(new GeoPoint2D(0.30001, 0.7)) - nps.PointAt(new GeoPoint2D(0.3, 0.7)));
                    double ffu = diru1.Length / diru.Length;
                    double cu = (diru ^ diru1).Length;
                    GeoVector dirv = nps.VDirection(new GeoPoint2D(0.3, 0.7));
                    GeoVector dirv1 = 100000 * (nps.PointAt(new GeoPoint2D(0.3, 0.70001)) - nps.PointAt(new GeoPoint2D(0.3, 0.7)));
                    double ffv = dirv1.Length / dirv.Length;
                    double cv = (dirv ^ dirv1).Length;
                    ColorDef red = new ColorDef("red", System.Drawing.Color.Red);
                    for (int i = 0; i < 11; i++)
                    {
                        for (int j = 0; j < 11; j++)
                        {
                            // GeoPoint2D uv = new GeoPoint2D(3 * i / 10.0 - 1.5, 3 * j / 10.0 - 1.5);
                            GeoPoint2D uv = new GeoPoint2D(i / 10.0, j / 10.0);
                            Line l = Line.MakeLine(nps.PointAt(uv), nps.PointAt(uv) + 0.00001 * nps.UDirection(uv).Normalized);
                            l.ColorDef = red;
                            dbgc.Add(l);
                            l = Line.MakeLine(nps.PointAt(uv), nps.PointAt(uv) + 0.00001 * nps.VDirection(uv).Normalized);
                            l.ColorDef = green;
                            dbgc.Add(l);
                        }
                    }
                    List<ICurve2D> projectedEdges = new List<ICurve2D>();
                    foreach (Edge edge in face.AllEdges)
                    {
                        if (edge.Curve3D != null) projectedEdges.Add(nps.GetProjectedCurve(edge.Curve3D, 0.0));
                    }

                }
            }
        }
        private void TestTorusImplicit()
        {
            ToroidalSurface ts = null;
            Model model = frame.Project.GetActiveModel();
            foreach (IGeoObject go in model)
            {
                if (go is Face face && face.Surface is ToroidalSurface toroidalSurface)
                {
                    ts = toroidalSurface;
                    break;
                }
            }
            //(sqrt((ny * (z - cz) - nz * (y - cy)) ^ 2 + (nz * (x - cx) - nx * (z - cz)) ^ 2 + (nx * (y - cy) - ny * (x - cx)) ^ 2) - r) ^ 2 + (nz * (z - cz) + ny * (y - cy) + nx * (x - cx)) ^ 2 - s
            // according to https://www.geometrictools.com/Documentation/DistanceToCircle3.pdf
            if (ts != null)
            {
                GeoPoint somePoint = ts.PointAt(new GeoPoint2D(0.5, 0.5));
                GeoVector d = somePoint - ts.Location;
                GeoVector n = ts.ZAxis.Normalized;
                double majorRadius = ts.XAxis.Length;
                double r = (n * d) * (n * d) + ((n ^ d).Length - majorRadius) * ((n ^ d).Length - majorRadius);
                // yes! the line above (-r²) is an implicit form of the torus, we can use this in maxima!
            }
        }
        private void TestSphereNP()
        {
            GeoVector a = new GeoVector(100, 100, 100);
            a.ArbitraryNormals(out GeoVector dirx, out GeoVector diry);
            SphericalSurfaceNP ss = new SphericalSurfaceNP(new GeoPoint(50, 50, 50), 100 * dirx.Normalized, 100 * diry.Normalized, 100 * a.Normalized);
            //SphericalSurfaceNP ss = new SphericalSurfaceNP(new GeoPoint(50, 50, 50), 100* GeoVector.XAxis, 100 * GeoVector.YAxis, 100 * GeoVector.ZAxis);
            //GeoObjectList dbg = ss.DebugDirectionsGrid;
            GeoPoint pp = ss.PointAt(new GeoPoint2D(-3, 4));
            GeoPoint2D po = ss.PositionOf(pp);
        }
        private void CloseShell()
        {
            if (frame.SelectedObjects.Count == 1 && frame.SelectedObjects[0] is Shell shell)
                shell.CloseOpenEdges();
        }
        private void TestParametrics()
        {
            if (frame.SelectedObjects.Count == 1 && frame.SelectedObjects[0] is Face face && face.Owner is Shell shell)
            {
                Parametrics pm = new Parametrics(shell);
                GeoVector offset = face.Surface.GetNormal(face.Domain.GetCenter());
                offset.Length = 10;
                pm.MoveFace(face, offset);
                Shell res = pm.Result(out HashSet<Face> dumy);
                if (res != null && shell.Owner is Solid sld)
                {
                    sld.SetShell(res);
                }
            }


        }

        public bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            return false;
        }
        void ICommandHandler.OnSelected(MenuWithHandler selectedMenuItem, bool selected) { }
        private void ExtendBSpline()
        {
            if (frame.SelectedObjects.Count == 1 && frame.SelectedObjects[0] is BSpline bspl)
            {
                //BSpline extended = bspl.Extend(0.1, 0.1);
            }
        }
        private void TestVoxelTree()
        {
            Line l = Line.MakeLine(new GeoPoint(2, 4, 6), new GeoPoint(100, 98, 96));
            VoxelTree vt = new VoxelTree(l, 6);
            GeoObjectList dbg = vt.Debug;
        }
        private void DrawUVGrid()
        {
            GeoObjectList list = new GeoObjectList();
            if (frame.SelectedObjects.Count == 1 && frame.SelectedObjects[0] is Face face)
            {
                for (int i = 0; i < 10; i++)
                {
                    ICurve fixedu = face.Surface.FixedU(face.Domain.Left + i * (face.Domain.Width) / 9.0, face.Domain.Bottom, face.Domain.Top);
                    list.Add(fixedu as IGeoObject);
                    ICurve fixedv = face.Surface.FixedV(face.Domain.Bottom + i * (face.Domain.Height) / 9.0, face.Domain.Left, face.Domain.Right);
                    list.Add(fixedv as IGeoObject);
                }
            }

        }
        private void SomeTestCode()
        {
            string path = @"C:\Zeichnungen\STEP\protocol.txt";
            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Protocol step files extent");
                }
            }
            bool skip = true;
            string lastSkipFile = "03_PN_7539_S_1059_2_I03_C03_DS_P202.stp";
            lastSkipFile = "1230_14_TLF_SECONDA_FASE.stp";
            lastSkipFile = "1249_MF1_ELETTRODI_INTERO.stp";
            lastSkipFile = "24636_P200_02-01.stp";
            lastSkipFile = "7907011770.stp";
            lastSkipFile = "C0175101_SHE_rechts_gespiegelt_gedreht.stp"; // syntaxfehler
            lastSkipFile = "exp1.stp";
            foreach (string file in Directory.EnumerateFiles(@"C:\Zeichnungen\STEP", "*.stp"))
            {
                if (skip)
                {
                    if (file.EndsWith(lastSkipFile)) skip = false;
                    continue;
                }
                try
                {
                    System.Diagnostics.Trace.WriteLine("importing: " + file);
                    Project pr = Project.ReadFromFile(file, "stp");
                    if (pr != null)
                    {
                        BoundingCube ext = pr.GetModel(0).Extent;
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.WriteLine(file + ", " + ext.Xmin.ToString(CultureInfo.InvariantCulture) + ", " + ext.Xmax.ToString(CultureInfo.InvariantCulture) + ", " + ext.Ymin.ToString(CultureInfo.InvariantCulture) +
                                ", " + ext.Ymax.ToString(CultureInfo.InvariantCulture) + ", " + ext.Zmin.ToString(CultureInfo.InvariantCulture) + ", " + ext.Zmax.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null) System.Diagnostics.Trace.WriteLine(file + ": " + ex.InnerException.Message);
                    else System.Diagnostics.Trace.WriteLine(file + ": " + ex.Message);
                }
            }
        }

    }
}
