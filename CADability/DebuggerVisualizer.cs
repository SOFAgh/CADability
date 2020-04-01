using System;
using System.Collections.Generic;
#if DEBUG
using Microsoft.VisualStudio.DebuggerVisualizers;
#endif
using CADability.Shapes;
using CADability.UserInterface;
using CADability.Curve2D;
using CADability.GeoObject;
using CADability.Attribute;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace CADability
{
    internal interface IDebuggerVisualizer
    {
        GeoObjectList GetList();
    }
#if DEBUG
    internal class Trace
    {
        static public void Clear()
        {
            if (File.Exists(@"C:\Temp\CADability.Trace.txt"))
                File.Delete(@"C:\Temp\CADability.Trace.txt");
        }
        static public void WriteLine(string text)
        {
            lock (typeof(Trace))
            {
                using (StreamWriter w = File.AppendText(@"C:\Temp\CADability.Trace.txt"))
                {
                    w.WriteLine(text);
                }
            }
        }
    }

    internal class CheckInstanceCounters
    {
        public static void Check()
        {
            Assembly ThisAssembly = Assembly.GetExecutingAssembly();
            Type[] types = ThisAssembly.GetTypes();
            System.Diagnostics.Trace.WriteLine("--- Instance Counters ---");
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            long mem = System.GC.GetTotalMemory(true);
            System.Diagnostics.Trace.WriteLine("memory used: " + mem.ToString());
            for (int i = 0; i < types.Length; ++i)
            {
                FieldInfo fi = types[i].GetField("InstanceCounter", BindingFlags.Static | BindingFlags.NonPublic);
                if (fi != null)
                {
                    object val = fi.GetValue(null);
                    try
                    {
                        int n = (int)val;
                        System.Diagnostics.Trace.WriteLine(types[i].Name + ": " + n.ToString());
                    }
                    catch (InvalidCastException)
                    {
                    }
                }
            }
            System.Diagnostics.Trace.WriteLine("--- End End   End End ---");
        }
    }

    public interface IDebugForm
    {
        Model Model { get; }
        void ShowDialog(IDialogVisualizerService windowService);
    }

    /// <summary>
    /// Creates a DebugForm as defined in CADability.Forms. 
    /// CADability.Forms.exe must be accessible at runtime to be able to debug
    /// </summary>
    static class CF
    {
        /// <summary>
        /// Load the assembly of CADability.Forms and instantiate the class
        /// </summary>
        public static IDebugForm DebugForm
        {
            get
            {
                Assembly cf = Assembly.Load("CADability.Forms");
                Type tp = cf.GetType("CADability.Forms.DebugForm");
                if (tp != null)
                {
                    ConstructorInfo ci = tp.GetConstructor(new Type[0]);
                    if (ci != null)
                    {
                        object df = ci.Invoke(new object[0]);
                        return df as IDebugForm;
                    }
                }
                return null;
            }
        }
    }
    public static class DebuggerExtensions
    {
        public static DebuggerContainer Show(this IEnumerable<object> obj)
        {
            return DebuggerContainer.Show(obj);
        }
    }

    /*
     * Die Visualisierungsobjekte in diesem file sind zum Anzeigen der jeweils behandelten Objekte beim Debuggen
     * Die Klassen müssen mit dem Attribut
     *     [System.Diagnostics.DebuggerVisualizer(typeof(XxxVisualizer))]
     * versehen werden, damit der Debugger weiß, welcher Visualizer für den jeweiligen Typ verwendet werden muss.
     * Es können auch mehrere Klassen für einen Typ sein.
     */

    /// <summary>
    /// Ein Kontainer für den DebuggerVisualizer, der verschiedene Dinge (Punkte, Vektoren, GeoObjekte) im Zusammenhang
    /// darstellen kann.
    /// Wenn man spontan Arrays von etwas darstellbarem zeigen will, dann in CommandWindow gehen und ??DebuggerContainer.Show(array) eingeben
    /// </summary>
    // created by MakeClassComVisible
    [System.Diagnostics.DebuggerVisualizer(typeof(GeneralDebuggerVisualizer))]
    [Serializable()]
    public class DebuggerContainer : IDebuggerVisualizer
    {
        public static System.Drawing.Color FromInt(int c)
        {
            int[] rgb = new int[3];
            for (int i = 0; i < 16; i++)
            {
                if ((c & (1 << i)) != 0)
                {
                    rgb[i % 3] += 128 >> (i / 3);
                }
            }
            return System.Drawing.Color.FromArgb(rgb[0], rgb[1], rgb[2]);
        }
        public GeoObjectList toShow;
        public DebuggerContainer()
        {
            toShow = new GeoObjectList();
        }
        public void Add(IGeoObject toAdd, System.Drawing.Color color, int debugHint = 0)
        {
            if (toAdd != null)
            {
                ColorDef cd = new ColorDef(color.Name, color);
                if (toAdd is IColorDef) (toAdd as IColorDef).ColorDef = cd;
                toShow.Add(toAdd);
            }

        }
        public void Add(IGeoObject toAdd)
        {
            if (toAdd != null) toShow.Add(toAdd);
        }
        public void Add(IGeoObject toAdd, int debugHint)
        {
            if (toAdd == null) return;
            IntegerProperty ip = new IntegerProperty(debugHint, "Debug.Hint");
            toAdd.UserData.Add("Debug", ip);
            toShow.Add(toAdd);
        }
        public void Add(GeoObjectList toAdd)
        {
            for (int i = 0; i < toAdd.Count; i++)
            {
                IGeoObject clone = toAdd[i].Clone();
                IntegerProperty ip = new IntegerProperty(i, "Debug.Hint");
                clone.UserData.Add("Debug", ip);
                toShow.Add(clone);
            }
        }
        public void Add(GeoPoint from, GeoVector dir, double size, System.Drawing.Color color)
        {
            Line l = Line.Construct();
            l.StartPoint = from;
            l.EndPoint = from + size * dir.Normalized;
            ColorDef cd = new ColorDef(color.Name, color);
            l.ColorDef = cd;
            toShow.Add(l);
            GeoVector perp = dir ^ GeoVector.ZAxis;
            if (Precision.IsNullVector(perp))
            {
                perp = dir ^ GeoVector.XAxis;
            }
            GeoPoint p1 = from + size * 0.9 * dir.Normalized + size * 0.1 * perp.Normalized;
            GeoPoint p2 = from + size * 0.9 * dir.Normalized - size * 0.1 * perp.Normalized;
            Line l1 = Line.Construct();
            l1.StartPoint = l.EndPoint;
            l1.EndPoint = p1;
            l1.ColorDef = cd;
            toShow.Add(l1);
            Line l2 = Line.Construct();
            l2.StartPoint = l.EndPoint;
            l2.EndPoint = p2;
            l2.ColorDef = cd;
            toShow.Add(l2);
        }
        public void Add(ICurve2D c2d)
        {
            toShow.Add(c2d.MakeGeoObject(Plane.XYPlane));
        }
        public void Add(ICurve2D c2d, System.Drawing.Color color, int debugHint)
        {
            if (c2d == null) return;
            IGeoObject go = c2d.MakeGeoObject(Plane.XYPlane);
            ColorDef cd = new ColorDef(color.Name, color);
            (go as IColorDef).ColorDef = cd;
            IntegerProperty ip = new IntegerProperty(debugHint, "Debug.Hint");
            go.UserData.Add("Debug", ip);
            //if (c2d.UserData!=null) // userdata kopieren
            //{
            //    foreach (KeyValuePair<string,object> item in c2d.UserData)
            //    {
            //        go.UserData.Add(item.Key, item.Value);
            //    }
            //}
            toShow.Add(go);
        }
        public void Add(ICurve2D c2d, System.Drawing.Color color, string debugHint)
        {
            if (c2d == null) return;
            IGeoObject go = c2d.MakeGeoObject(Plane.XYPlane);
            ColorDef cd = new ColorDef(color.Name, color);
            (go as IColorDef).ColorDef = cd;
            StringProperty sp = new StringProperty(debugHint, "Debug.Hint");
            go.UserData.Add("Debug", sp);
            toShow.Add(go);
        }
        public void Add(Border bdr, System.Drawing.Color color, int debugHint)
        {
            IGeoObject go = bdr.AsPath().MakeGeoObject(Plane.XYPlane);
            ColorDef cd = new ColorDef(color.Name, color);
            (go as IColorDef).ColorDef = cd;
            IntegerProperty ip = new IntegerProperty(debugHint, "Debug.Hint");
            go.UserData.Add("Debug", ip);
            if (go is CADability.GeoObject.Path)
            {
                CADability.GeoObject.Path path = (go as CADability.GeoObject.Path);
                for (int i = 0; i < path.CurveCount; ++i)
                {
                    (path.Curves[i] as IGeoObject).UserData.Add("Debug", ip);
                    toShow.Add(path.Curves[i] as IGeoObject);
                }
            }
        }
        public void Add(ICurve2D[] cvs)
        {
            for (int i = 0; i < cvs.Length; ++i)
            {
                Add(cvs[i], System.Drawing.Color.Red, i);
            }
        }
        public void Add(GeoPoint2D pnt, System.Drawing.Color color, int debugHint)
        {
            Point point = Point.Construct();
            point.Location = new GeoPoint(pnt);
            point.Symbol = PointSymbol.Circle;
            ColorDef cd = new ColorDef(color.Name, color);
            point.ColorDef = cd;
            IntegerProperty ip = new IntegerProperty(debugHint, "Debug.Hint");
            point.UserData.Add("Debug", ip);
            toShow.Add(point);
        }
        public void Add(GeoPoint2D pnt, System.Drawing.Color color, string debugHint)
        {
            Point point = Point.Construct();
            point.Location = new GeoPoint(pnt);
            point.Symbol = PointSymbol.Circle;
            ColorDef cd = new ColorDef(color.Name, color);
            point.ColorDef = cd;
            StringProperty sp = new StringProperty(debugHint, "Debug.Hint");
            point.UserData.Add("Debug", sp);
            toShow.Add(point);
        }
        public void Add(GeoPoint pnt, System.Drawing.Color color, int debugHint)
        {
            Point point = Point.Construct();
            point.Location = pnt;
            point.Symbol = PointSymbol.Circle;
            ColorDef cd = new ColorDef(color.Name, color);
            point.ColorDef = cd;
            IntegerProperty ip = new IntegerProperty(debugHint, "Debug.Hint");
            point.UserData.Add("Debug", ip);
            toShow.Add(point);
        }
#region IDebuggerVisualizer Members
        GeoObjectList IDebuggerVisualizer.GetList()
        {
            return toShow;
        }
#endregion
        public GeoObjectList Squared2DList
        {
            get
            {
                BoundingCube bc = toShow.GetExtent();
                ModOp mop;
                if (bc.Xmax - bc.Xmin > bc.Ymax - bc.Ymin)
                {
                    mop = ModOp.Scale(1.0, (bc.Xmax - bc.Xmin) / (bc.Ymax - bc.Ymin), 1.0);
                }
                else
                {
                    mop = ModOp.Scale((bc.Ymax - bc.Ymin) / (bc.Xmax - bc.Xmin), 1.0, 1.0);
                }
                GeoObjectList res = toShow.CloneObjects();
                res.Modify(mop);
                return res;
            }
        }
        //public static DebuggerContainer Show(object[] obj)
        //{
        //    DebuggerContainer res = new CADability.DebuggerContainer();
        //    for (int i = 0; i < obj.Length; i++)
        //    {
        //        if (obj[i] is Face) res.Add(obj[i] as IGeoObject, (obj[i] as Face).GetHashCode());
        //        else if (obj[i] is IGeoObject) res.Add(obj[i] as IGeoObject);
        //        else if (obj[i] is Edge && (obj[i] as Edge).Curve3D!=null) res.Add((obj[i] as Edge).Curve3D as IGeoObject, (obj[i] as Edge).GetHashCode());
        //        else if (obj[i] is ICurve2D) res.Add(obj[i] as ICurve2D);
        //    }
        //    return res;
        //}
        public static DebuggerContainer Show(IArray<GeoPoint> pnts)
        {
            return Show(pnts.ToArray());
        }
        public static DebuggerContainer Show(IEnumerable<object> obj)
        {
            DebuggerContainer res = new CADability.DebuggerContainer();
            ColorDef cd = new ColorDef("debug", System.Drawing.Color.Red);
            int i = 0;
            foreach (object obji in obj)
            {

                if (obji is Face)
                {
                    res.Add(obji as IGeoObject, (obji as Face).GetHashCode());
                    Face fc = obji as Face;
                    double ll = fc.GetExtent(0.0).Size * 0.01;
                    SimpleShape ss = fc.Area;
                    GeoPoint2D c = ss.GetExtent().GetCenter();
                    GeoPoint pc = fc.Surface.PointAt(c);
                    GeoVector nc = fc.Surface.GetNormal(c);
                    Line l = Line.TwoPoints(pc, pc + ll * nc.Normalized);
                    l.ColorDef = cd;
                    res.Add(l);
                }
                else if (obji is IGeoObject) res.Add(obji as IGeoObject);
                else if (obji is Edge && (obji as Edge).Curve3D != null) res.Add((obji as Edge).Curve3D as IGeoObject, (obji as Edge).GetHashCode());
                else if (obji is ICurve2D) res.Add(obji as ICurve2D, System.Drawing.Color.Red, i);
                else if (obji is Vertex) res.Add((obji as Vertex).DebugPoint, (obji as Vertex).GetHashCode());
                else if (obji is IDebuggerVisualizer) res.Add((obji as IDebuggerVisualizer).GetList());
                ++i;
            }
            return res;
        }
        public static DebuggerContainer Show(GeoPoint2D[] points)
        {
            DebuggerContainer res = new CADability.DebuggerContainer();
            Polyline2D pl2d = new Polyline2D(points);
            res.Add(pl2d);
            return res;
        }
        public static DebuggerContainer Show(GeoPoint[] points)
        {
            DebuggerContainer res = new CADability.DebuggerContainer();
            Polyline pl = Polyline.Construct();
            pl.SetPoints(points, false);
            res.Add(pl);
            return res;
        }
        public static DebuggerContainer Show(GeoPoint[,] points)
        {
            DebuggerContainer res = new CADability.DebuggerContainer();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                GeoPoint[] pnts = new GeoPoint[points.GetLength(1)];
                for (int j = 0; j < points.GetLength(1); j++)
                {
                    pnts[j] = points[i, j];
                }
                Polyline pl = Polyline.Construct();
                pl.SetPoints(pnts, false);
                res.Add(pl, i);
            }
            return res;
        }
        public static object SerializeAndDeserialize(ISerializable obj)
        {
            BinaryFormatter formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.File, null));
            formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            // formatter.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.XsdString; // XsdString macht das wechseln von .NET Frameworks schwierig
            MemoryStream ms = new MemoryStream();
            formatter.Serialize(ms, obj);
            formatter = new BinaryFormatter();
            ms.Seek(0, SeekOrigin.Begin);
            try
            {
                return formatter.Deserialize(ms);
            }
            catch
            {
                return null;
            }
        }

        internal void Add(IEnumerable<Edge> edges, Face onThisFace, double arrowsize, System.Drawing.Color clr, int debugHint)
        {
            Random rnd = new Random();
            foreach (Edge edg in edges)
            {
                if (edg.Curve2D(onThisFace) == null) continue;
                int dbgh = debugHint;
                if (debugHint == -1) dbgh = edg.GetHashCode();
                Add(edg.Curve2D(onThisFace), clr, dbgh);
                // noch einen Richtungspfeil zufügen
                GeoPoint2D[] arrowpnts = new GeoPoint2D[3];
                double pos = 0.3 + rnd.NextDouble() * 0.4; // to have different positions when the same curve is displayed twice
                GeoVector2D dir = edg.Curve2D(onThisFace).DirectionAt(pos).Normalized;
                arrowpnts[1] = edg.Curve2D(onThisFace).PointAt(pos);
                arrowpnts[0] = arrowpnts[1] - arrowsize * dir + arrowsize * dir.ToLeft();
                arrowpnts[2] = arrowpnts[1] - arrowsize * dir + arrowsize * dir.ToRight();
                Polyline2D pl2d = new Polyline2D(arrowpnts);
                Add(pl2d, clr, edg.GetHashCode());
            }
        }
        internal void Add(IEnumerable<ICurve2D> crvs, double arrowsize, System.Drawing.Color clr, int debugHint)
        {
            int i = 0;
            foreach (ICurve2D crv in crvs)
            {
                Add(crv, clr, debugHint);
                // noch einen Richtungspfeil zufügen
                GeoPoint2D[] arrowpnts = new GeoPoint2D[3];
                GeoVector2D dir = crv.DirectionAt(0.5).Normalized;
                arrowpnts[1] = crv.PointAt(0.5);
                arrowpnts[0] = arrowpnts[1] - arrowsize * dir + arrowsize * dir.ToLeft();
                arrowpnts[2] = arrowpnts[1] - arrowsize * dir + arrowsize * dir.ToRight();
                Polyline2D pl2d = new Polyline2D(arrowpnts);
                Add(pl2d, clr, debugHint * 100 + i);
                ++i;
            }
        }
    }

    /* So benutzt man den DebuggerVisualizer aus dem Command Window:
     * ? GeneralDebuggerVisualizer.TestShowVisualizer(res.DebugEdges3D);
     */
    public class GeneralDebuggerVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;
            object o = objectProvider.GetObject();
            if (o is IDebuggerVisualizer)
            {
                IDebuggerVisualizer dv = (IDebuggerVisualizer)objectProvider.GetObject();
                m.Add(dv.GetList());
            }
            else if (o is IGeoObject)
            {
                m.Add(o as IGeoObject);
            }
            form.ShowDialog(windowService);
        }
        public static void TestShowVisualizer(object objectToVisualize)
        {
            VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(GeneralDebuggerVisualizer));
            visualizerHost.ShowVisualizer();
        }
        // ? GeneralDebuggerVisualizer.TestShowVisualizer(face.DebugEdges3D); // aus Command Window
    }

    internal class GeoObjectVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            IGeoObjectImpl go = (IGeoObjectImpl)objectProvider.GetObject();
            m.Add(go);

            form.ShowDialog(windowService);
        }

        /// <summary>
        /// Damit kann man den Visualizer zum Debuggen im Context von CADability aufrufen, sonst läuft er immer im
        /// Context des Debuggers
        /// </summary>
        /// <param name="objectToVisualize">The object to display in the visualizer.</param>
        public static void TestShowVisualizer(object objectToVisualize)
        {
            VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(GeoObjectVisualizer));
            visualizerHost.ShowVisualizer();
        }
    }

    internal class GeoObjectListVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            GeoObjectList list = (GeoObjectList)objectProvider.GetObject();
            for (int i = 0; i < list.Count; ++i)
            {
                IntegerProperty ip = new IntegerProperty(i, "Debug.Hint");
                list[i].UserData.Add("ListIndex", ip);
                m.Add(list[i]);
            }
            m.Add(list);

            form.ShowDialog(windowService);
        }
        public static void TestShowVisualizer(object objectToVisualize)
        {
            VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(GeoObjectListVisualizer));
            visualizerHost.ShowVisualizer();
        }
    }

    internal class BorderVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            Border bdr = (Border)objectProvider.GetObject();
            for (int i = 0; i < bdr.DebugList.Count; ++i)
            {
                IGeoObject toAdd = bdr.DebugList[i];
                IntegerProperty ip = new IntegerProperty(i, "Debug.Hint");
                toAdd.UserData.Add("Debug", ip);
                m.Add(toAdd);
            }

            form.ShowDialog(windowService);
        }
    }

    internal class Curve2DVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            ICurve2D gc2d = (ICurve2D)objectProvider.GetObject();
            m.Add(gc2d.MakeGeoObject(Plane.XYPlane));

            form.ShowDialog(windowService);
        }
    }

    internal class CurveVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            IGeoObject go = (IGeoObject)objectProvider.GetObject();
            m.Add(go);

            form.ShowDialog(windowService);
        }
    }

    internal class GeoPoint2DVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            GeoPoint2D p = (GeoPoint2D)objectProvider.GetObject();
            Point pnt = Point.Construct();
            pnt.Location = new GeoPoint(p);
            pnt.Symbol = PointSymbol.Cross;
            m.Add(pnt);

            form.ShowDialog(windowService);
        }
    }

    internal class GeoPointVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            GeoPoint p = (GeoPoint)objectProvider.GetObject();
            Point pnt = Point.Construct();
            pnt.Location = p;
            pnt.Symbol = PointSymbol.Cross;
            m.Add(pnt);

            form.ShowDialog(windowService);
        }
    }

    internal class CompoundShapeVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            CompoundShape compoundShape = (CompoundShape)objectProvider.GetObject();
            m.Add(compoundShape.DebugList);

            form.ShowDialog(windowService);
        }
    }

    internal class SimpleShapeVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            IDebugForm form = CF.DebugForm;
            Model m = form.Model;

            SimpleShape simpleShape = (SimpleShape)objectProvider.GetObject();
            m.Add(simpleShape.DebugList);

            form.ShowDialog(windowService);
        }

        /// <summary>
        /// Damit kann man den Visualizer zum Debuggen im Context von CADability aufrufen, sonst läuft er immer im
        /// Context des Debuggers
        /// </summary>
        /// <param name="objectToVisualize">The object to display in the visualizer.</param>
        public static void TestShowVisualizer(object objectToVisualize)
        {
            VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(SimpleShapeVisualizer));
            visualizerHost.ShowVisualizer();
        }
    }

#endif
        }
