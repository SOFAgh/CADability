using CADability.Attribute;
using CADability.Curve2D;
using CADability.GeoObject;
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
    /// This class is not part of CADability OpenSource. I use it to write testcode for debugging
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
                TestPoleRemoval();
                return true;
            }
            return false;
        }
        private void TestPoleRemoval()
        {
            Model model = frame.Project.GetActiveModel();
            foreach (IGeoObject go in model)
            {
                if (go is Face face)
                {
                    BoundingRect natBounds = new BoundingRect();
                    face.Surface.GetNaturalBounds(out natBounds.Left, out natBounds.Right, out natBounds.Bottom, out natBounds.Top);
                    NonPeriodicSurface nps = new NonPeriodicSurface(face.Surface, natBounds);
                    GeoObjectList dbgc = new GeoObjectList();
                    ColorDef blue = new ColorDef("blue", System.Drawing.Color.Blue);
                    for (int i = 0; i < 11; i++)
                    {
                        ICurve crvu = nps.FixedU(3 * i / 10.0 - 1.5, -1.5, 1.5);
                        ICurve crvv = nps.FixedV(3 * i / 10.0 - 1.5, -1.5, 1.5);
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
                        Polyline plv = Polyline.Construct();
                        for (int j = 0; j < 51; j++)
                        {
                            pnts[j] = crvv.PointAt(j / 50.0);
                        }
                        try
                        {
                            plv.SetPoints(pnts, false);
                            plv.ColorDef = blue;
                            dbgc.Add(plv);
                        }
                        catch { }
                    }
                    ColorDef red = new ColorDef("red", System.Drawing.Color.Red);
                    for (int i = 0; i < 11; i++)
                    {
                        for (int j = 0; j < 11; j++)
                        {
                            GeoPoint2D uv = new GeoPoint2D(i / 10.0, j / 10.0);
                            Line l = Line.MakeLine(nps.PointAt(uv), nps.PointAt(uv) + 0.00001* nps.VDirection(uv).Normalized);
                            l.ColorDef = red;
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
        private void TestSphereNP()
        {
            GeoVector a = new GeoVector(100, 100, 100);
            a.ArbitraryNormals(out GeoVector dirx, out GeoVector diry);
            SphericalSurfaceNP ss = new SphericalSurfaceNP(new GeoPoint(50, 50, 50), 100 * dirx.Normalized, 100 * diry.Normalized, 100 * a.Normalized);
            //SphericalSurfaceNP ss = new SphericalSurfaceNP(new GeoPoint(50, 50, 50), 100* GeoVector.XAxis, 100 * GeoVector.YAxis, 100 * GeoVector.ZAxis);
            GeoObjectList dbg = ss.DebugDirectionsGrid;
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
