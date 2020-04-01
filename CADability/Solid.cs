﻿using CADability.Actions;
using CADability.Attribute;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace CADability.GeoObject
{
    class ShowPropertySolid : IShowPropertyImpl, IGeoObjectShowProperty, ICommandHandler
    {
        private Solid solid;
        private IShowProperty[] attributeProperties; // Anzeigen für die Attribute (Ebene, Farbe u.s.w)
        public ShowPropertySolid(Solid solid, IFrame frame)
            : base(frame)
        {
            this.solid = solid;
            resourceId = "Solid.Object";
            attributeProperties = solid.GetAttributeProperties(frame);
        }
        #region IShowPropertyImpl overrides
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                return ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.Selectable;
            }
        }
        public override string LabelText
        {
            get
            {
                //if (solid.Name != null)
                //{
                //    return StringTable.GetFormattedString("Solid.NamedObject", solid.Name);
                //}
                return base.LabelText;
            }
            set
            {
                base.LabelText = value;
            }
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
#if DEBUG
                    IntegerProperty dbghashcode = new IntegerProperty(solid.UniqueId, "Debug.Hashcode");
                    se.Add(dbghashcode);
#endif
                    // Volume dauert zu lange
                    //DoubleProperty vol = new DoubleProperty("Solid.Volume", base.Frame);
                    //vol.DoubleValue = solid.Volume;
                    //vol.ReadOnly = true;
                    //se.Add(vol);
                    se.Add(new NameProperty(this.solid, "Name", "Solid.Name"));
                    foreach (Shell shell in solid.Shells)
                    {
                        IShowProperty sp = shell.GetShowProperties(base.Frame);
                        sp.ReadOnly = true;
                        se.Add(sp);
                    }
                    se.AddRange(attributeProperties);
                    subEntries = se.ToArray();
                }
                return subEntries;
            }
        }
        public override MenuWithHandler[] ContextMenu
        {
            get
            {
                return MenuResource.LoadMenuDefinition("MenuId.Object.Solid", false, this);
            }
        }
        #endregion
        #region IGeoObjectShowProperty Members

        public event CreateContextMenueDelegate CreateContextMenueEvent;
        IGeoObject IGeoObjectShowProperty.GetGeoObject()
        {
            return solid;
        }

        string IGeoObjectShowProperty.GetContextMenuId()
        {
            return "MenuId.Object.Solid";
        }

#endregion
#region ICommandHandler Members

        bool ICommandHandler.OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.Explode":
                    if (Frame.ActiveAction is SelectObjectsAction)
                    {
                        using (Frame.Project.Undo.UndoFrame)
                        {
                            IGeoObjectOwner addTo = solid.Owner;
                            if (addTo == null) addTo = Frame.ActiveView.Model;
                            GeoObjectList toSelect = solid.Decompose();
                            addTo.Remove(solid);
                            for (int i = 0; i < toSelect.Count; ++i)
                            {
                                addTo.Add(toSelect[i]);
                            }
                            SelectObjectsAction soa = Frame.ActiveAction as SelectObjectsAction;
                            soa.SetSelectedObjects(toSelect); // alle Teilobjekte markieren
                        }
                    }
                    return true;
                case "MenuId.SplitByPlane":
                    {
                        Frame.SetAction(new SplitSolidByPlane(solid));
                    }
                    return true;
            }
            return false;
        }

        bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            switch (MenuId)
            {
                case "MenuId.Explode":
                    CommandState.Enabled = true; // naja isses ja immer
                    return true;
                case "MenuId.SplitByPlane":
                    CommandState.Enabled = true; // naja isses ja immer
                    return true;
            }
            return false;
        }

#endregion
    }


    /// <summary>
    /// A Solid is a <see cref="IGeoObject"/> implementation that represents a solid body.
    /// Its main data is a collection of oriented faces. The normal vector on any point of the face
    /// points to the outside of the body. Solids have one set of faces that represent the outer hull
    /// and any number of cavyties that reside totally inside the outer hull. All cavyties are disjoint.
    /// </summary>
    // created by MakeClassComVisible
    [Serializable()]
    public class Solid : IGeoObjectImpl, ISerializable, IColorDef, IGetSubShapes, IGeoObjectOwner, IDeserializationCallback, IJsonSerialize, IJsonSerializeDone, IExportStep
    {
        internal static bool octTreeAlsoCheckInside = false; // global setting: auch das innere der Solids für den octTree überprüfen (Anforderung von Hilgers)
        private Shell[] shells; // List of Shells defining this Solid. The first (and in most cases only) entry is the outer hull, the following are non intersecting inner holes (faces oriented to point into the inside of the hole)
        private string name; // aus STEP oder IGES kommen benannte solids (Shells, Faces?)
        [FlagsAttribute] private enum Flags { oriented = 1 };
        private Flags flags;
        private Edge[] edges; // secondary data, not serialized
        /// <summary>
        /// Returns all the edges of this Shell. Each egde is unique in the array 
        /// but may belong to two different faces.
        /// </summary>
        public Edge[] Edges
        {
            get
            {
                if (edges == null)
                {
                    HashSet<Edge> edgelist = new HashSet<Edge>();
                    foreach (Shell shell in shells)
                    {
                        foreach (Face fc in shell.Faces)
                        {
                            foreach (Edge ed in fc.AllEdges)
                            {
                                edgelist.Add(ed);
                            }
                        }
                    }
                    edges = new Edge[edgelist.Count];
                    edgelist.CopyTo(edges, 0);
                }
                return edges;
            }
        }
#region polymorph construction
        public delegate Solid ConstructionDelegate();
        public static ConstructionDelegate Constructor;
        public static Solid Construct()
        {
            if (Constructor != null) return Constructor();
            return new Solid();
        }
        public delegate void ConstructedDelegate(Solid justConstructed);
        public static ConstructedDelegate Constructed;
#endregion
        protected Solid()
            : base()
        {
            if (Constructed != null) Constructed(this);
        }
        /// <summary>
        /// Returns a list of all shells that bound this solid. In most cases there is only a single shell.
        /// If there is more than one shell the shell with index 0 is the outer hull and all subsequent shells
        /// describe cavyties that reside totally inside the outer hull.
        /// </summary>
        public Shell[] Shells
        {
            get
            {
                return shells;
            }
        }
        public void SetShell(Shell sh)
        {
            shells = new Shell[] { sh };
            sh.Owner = this;
            edges = null;
        }
        public static Solid MakeSolid(Shell sh)
        {
            Solid res = Solid.Construct();
            res.SetShell(sh);
            return res;
        }
        public void SetFaces(Shell sh, Face[] faces)
        {   // nur wg. Undo
            Face[] clonedFaces = (Face[])sh.Faces.Clone();
            Dictionary<Edge, Edge> clonedEdges = new Dictionary<Edge, Edge>();
            for (int i = 0; i < clonedFaces.Length; i++)
            {
                clonedFaces[i] = clonedFaces[i].Clone(clonedEdges);
            }
            using (new Changing(this, "SetFaces", sh, clonedFaces))
            {
                sh.SetFaces(faces);
            }
        }
        /// <summary>
        /// The name of the solid. 
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                using (new Changing(this, false, true, "Name", name))
                {
                    name = value;
                }
            }
        }
        public string NameOrEmpty
        {
            get
            {
                if (name == null) return "";
                else return name;
            }
        }
        /// <summary>
        /// Returns the curves that result from a planar intersection of this shell with the provided plane.
        /// The curves are properly clipped.
        /// </summary>
        /// <param name="pl">The plane to intersect with</param>
        /// <returns>Array of intersection curves</returns>
        public ICurve[] GetPlaneIntersection(PlaneSurface pl)
        {
            List<ICurve> res = new List<ICurve>();
            foreach (Shell shell in shells)
            {
                res.AddRange(shell.GetPlaneIntersection(pl));
            }
            return res.ToArray();
        }
        /// <summary>
        /// Returns the volume (capacity) of this solid.
        /// </summary>
        public double Volume
        {
            get
            {
                return 0.0;
            }
        }
#region IGeoObject Members
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.GetShowProperties (IFrame)"/>
        /// </summary>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public override IShowProperty GetShowProperties(IFrame Frame)
        {
            return new ShowPropertySolid(this, Frame);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.Modify (ModOp)"/>
        /// </summary>
        /// <param name="m"></param>
        public override void Modify(ModOp m)
        {
            // wir gehen hier davon aus, dass die einzelnen Shells voneinander unabhängig sind,
            // d.h. keine gemeinsamen Kanten oder Faces besitzen. Wäre dem aber so,
            // so würden die Kanten oder Faces mehrfach modifiziert, was natürlich falsch wäre
            using (new Changing(this, "ModifyInverse", m))
            {
                for (int i = 0; i < shells.Length; ++i)
                {
                    shells[i].ModifyNoEdges(m);
                }
                for (int i = 0; i < Edges.Length; ++i)
                {
                    IGeoObject go = edges[i].Curve3D as IGeoObject;
                    if (go != null)
                    {
                        go.Modify(m);
                    }
                }
                for (int i = 0; i < shells.Length; ++i)
                {
                    foreach (Vertex vtx in shells[i].Vertices)
                    {
                        vtx.Modify(m);
                    }
                }
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.Clone ()"/>
        /// </summary>
        /// <returns></returns>
        public override IGeoObject Clone()
        {
            // auch hier gehen wir von unabhängigen Shells aus, die keine gemeinsamen Faces oder Edges haben
            // das sollte immer so gewährleistet sein!
            Solid res = Solid.Construct();
            res.CopyAttributes(this); // shells gibt es hier noch nicht, damit sollten die ihre Farbe behalten
            if (shells != null)
            {
                res.shells = new Shell[shells.Length];
                for (int i = 0; i < shells.Length; ++i)
                {
                    res.shells[i] = shells[i].Clone() as Shell;
                    res.shells[i].Owner = res;
                }
            }
            res.name = name;
            return res;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.CopyGeometry (IGeoObject)"/>
        /// </summary>
        /// <param name="ToCopyFrom"></param>
        public override void CopyGeometry(IGeoObject ToCopyFrom)
        {
            Solid cc = ToCopyFrom as Solid;
            using (new Changing(this))
            {
                for (int i = 0; i < shells.Length; ++i)
                {
                    shells[i].CopyGeometryNoEdges(cc.shells[i]);
                }
                for (int i = 0; i < Edges.Length; ++i)
                {
                    edges[i].RecalcCurve3D();
                }
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.FindSnapPoint (SnapPointFinder)"/>
        /// </summary>
        /// <param name="spf"></param>
        public override void FindSnapPoint(SnapPointFinder spf)
        {
            foreach (Shell shell in shells)
            {
                shell.FindSnapPoint(spf);
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.GetBoundingCube ()"/>
        /// </summary>
        /// <returns></returns>
        public override BoundingCube GetBoundingCube()
        {
            BoundingCube res = BoundingCube.EmptyBoundingCube;
            for (int i = 0; i < shells.Length; ++i)
            {
                res.MinMax(shells[i].GetBoundingCube());
            }
            return res;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.PaintTo3DList (IPaintTo3D, ICategorizedDislayLists)"/>
        /// </summary>
        /// <param name="paintTo3D"></param>
        /// <param name="lists"></param>
        public override void PaintTo3DList(IPaintTo3D paintTo3D, ICategorizedDislayLists lists)
        {   // wenn einzelne Faces verschiedene layer haben, dann...?
            if (Layer != null && Layer.Transparency > 0)
                lists.Add(Layer, false, false, this); // in PaintTo3D wirds dann richtig gemacht
            else
                lists.Add(Layer, true, true, this); // in PaintTo3D wirds dann richtig gemacht
            // paintTo3D.PaintSurfaces und paintTo3D.PaintEdges muss richtig eingestellt und zweimal aufgerufen werden
        }
        public delegate bool PaintTo3DDelegate(Solid toPaint, IPaintTo3D paintTo3D);
        public static PaintTo3DDelegate OnPaintTo3D;
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.PaintTo3D (IPaintTo3D)"/>
        /// </summary>
        /// <param name="paintTo3D"></param>
        public override void PaintTo3D(IPaintTo3D paintTo3D)
        {
            // hier könnte man noch das "Culling" einschalten
            if (OnPaintTo3D != null && OnPaintTo3D(this, paintTo3D)) return;
            for (int i = 0; i < shells.Length; ++i)
            {
                shells[i].PaintTo3D(paintTo3D);
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.PrepareDisplayList (double)"/>
        /// </summary>
        /// <param name="precision"></param>
        public override void PrepareDisplayList(double precision)
        {
            for (int i = 0; i < Shells.Length; ++i)
            {
                shells[i].PrepareDisplayList(precision);
            }
        }
        public override Style.EDefaultFor PreferredStyle
        {
            get
            {
                return Style.EDefaultFor.Solids;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.GetExtent (Projection, ExtentPrecision)"/>
        /// </summary>
        /// <param name="projection"></param>
        /// <param name="extentPrecision"></param>
        /// <returns></returns>
        public override BoundingRect GetExtent(Projection projection, ExtentPrecision extentPrecision)
        {
            BoundingRect res = BoundingRect.EmptyBoundingRect;
            for (int i = 0; i < Shells.Length; ++i)
            {
                res.MinMax(shells[i].GetExtent(projection, extentPrecision));
            }
            return res;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.GetQuadTreeItem (Projection, ExtentPrecision)"/>
        /// </summary>
        /// <param name="projection"></param>
        /// <param name="extentPrecision"></param>
        /// <returns></returns>
        public override IQuadTreeInsertableZ GetQuadTreeItem(Projection projection, ExtentPrecision extentPrecision)
        {
            QuadTreeCollection res = new QuadTreeCollection(this, projection);
            for (int i = 0; i < Shells.Length; ++i)
            {
                res.Add(shells[i].GetQuadTreeItem(projection, extentPrecision));
            }
            return res;
        }
        public override Layer Layer
        {
            get
            {
                return base.Layer;
            }
            set
            {
                base.Layer = value;
                if (shells != null)
                {
                    for (int i = 0; i < shells.Length; ++i)
                    {
                        shells[i].Layer = value;
                    }
                }
            }
        }
        public override IGeoObject[] OwnedItems
        {
            get
            {
                return Shells;
            }
        }
        public override string Description
        {
            get
            {
                //if (this.Name != null)
                //{
                //    return StringTable.GetFormattedString("Solid.NamedObject", this.Name);
                //}
                return StringTable.GetString("Solid.Object");
            }
        }
#endregion
#region IOctTreeInsertable members
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.GetExtent (double)"/>
        /// </summary>
        /// <param name="precision"></param>
        /// <returns></returns>
        public override BoundingCube GetExtent(double precision)
        {
            return GetBoundingCube();
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.HitTest (ref BoundingCube, double)"/>
        /// </summary>
        /// <param name="cube"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public override bool HitTest(ref BoundingCube cube, double precision)
        {
            foreach (Shell sh in Shells)
            {
                if (sh.HitTest(ref cube, precision)) return true;
            }
            if (octTreeAlsoCheckInside)
            {   // es könnte sein, dass die Box ganz innerhalb des Solids liegt
                if (GetBoundingCube().Contains(cube))
                {
                    if (shells[0].IsInside(cube.GetCenter())) return true;
                }
            }
            // Edges braucht man doch nicht auch noch, oder?
            return false;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.HitTest (Projection, BoundingRect, bool)"/>
        /// </summary>
        /// <param name="projection"></param>
        /// <param name="rect"></param>
        /// <param name="onlyInside"></param>
        /// <returns></returns>
        public override bool HitTest(Projection projection, BoundingRect rect, bool onlyInside)
        {
            if (onlyInside)
            {
                foreach (Shell sh in Shells)
                {
                    if (!sh.HitTest(projection, rect, onlyInside)) return false;
                }
                return true;
            }
            else
            {
                foreach (Shell sh in Shells)
                {
                    if (sh.HitTest(projection, rect, onlyInside)) return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.HitTest (Projection.PickArea, bool)"/>
        /// </summary>
        /// <param name="area"></param>
        /// <param name="onlyInside"></param>
        /// <returns></returns>
        public override bool HitTest(Projection.PickArea area, bool onlyInside)
        {
            if (onlyInside)
            {
                foreach (Shell sh in Shells)
                {
                    if (!sh.HitTest(area, onlyInside)) return false;
                }
                return true;
            }
            else
            {
                foreach (Shell sh in Shells)
                {
                    if (sh.HitTest(area, onlyInside)) return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.Position (GeoPoint, GeoVector, double)"/>
        /// </summary>
        /// <param name="fromHere"></param>
        /// <param name="direction"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public override double Position(GeoPoint fromHere, GeoVector direction, double precision)
        {
            double res = double.MaxValue;
            foreach (Shell sh in Shells)
            {
                double d = sh.Position(fromHere, direction, precision);
                if (d < res) res = d;
            }
            return res;
        }
#endregion
#if DEBUG
        internal void Debug()
        {
            // DEBUG
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            DebuggerContainer dbg = new DebuggerContainer();
            for (int i = 0; i < shells.Length; ++i)
            {
                for (int j = 0; j < shells[i].Faces.Length; ++j)
                {
                    Face face = shells[i].Faces[j];
                    dbg.Add(face);
                    double umin, umax, vmin, vmax;
                    face.GetUVBounds(out umin, out umax, out vmin, out vmax);
                    GeoPoint2D uv = new GeoPoint2D((umin + umax) / 2.0, (vmin + vmax) / 2.0);
                    GeoPoint p0 = face.Surface.PointAt(uv);
                    GeoVector d0 = face.Surface.GetNormal(uv);
                    if (!face.OrientedOutward) d0 = -d0;
                    dbg.Add(p0, d0, 10, System.Drawing.Color.Red);
                }
            }
            // DebuggerVisualizer.TestShowVisualizer(dbg);
            // END DEBUG
        }
#endif
        public void FreeCachedMemory()
        {
            for (int i = 0; i < shells.Length; i++)
            {
                shells[i].FreeCachedMemory();
            }
        }
        /// <summary>
        /// Unites the two solids and returns the union. If the solids are disjunct, null is returned.
        /// </summary>
        /// <param name="solid1">first solid</param>
        /// <param name="solid2">second solid</param>
        /// <returns>union or null</returns>
        static public Solid Unite(Solid solid1, Solid solid2)
        {
            if (Settings.GlobalSettings.GetBoolValue("UseNewBrepOperations", false))
            {
                BRepOperation bro = new BRepOperation(solid1.shells[0], solid2.shells[0], BRepOperation.Operation.union);
                Shell[] res = bro.Result();
                if (res.Length == 1)
                {
                    return MakeSolid(res[0]);
                }
                else
                {
                    return null;
                }
            }
            else
            {
            return null;
            }
        }
        /// <summary>
        /// Returns the intersection of two solids, i.e. those parts that are common to both solids. If the 
        /// solids are disjunct, an empty array is returned. There may be more than one solid body as a restult.
        /// </summary>
        /// <param name="solid1">first solid</param>
        /// <param name="solid2">second solid</param>
        /// <returns>array of common parts</returns>
        static public Solid[] Intersect(Solid solid1, Solid solid2)
        {
            if (Settings.GlobalSettings.GetBoolValue("UseNewBrepOperations", false))
            {
                // hier fehlt noch der Löcherkäse: Solids mit mehreren Shells
                BRepOperation bro = new BRepOperation(solid1.shells[0], solid2.shells[0], BRepOperation.Operation.intersection);
                Shell[] res = bro.Result();
                Solid[] sres = new Solid[res.Length];
                for (int i = 0; i < res.Length; i++)
                {
                    sres[i] = MakeSolid(res[i]);
                }
                return sres;
            }
            return null;
        }
        /// <summary>
        /// Returns the difference of two solids. The second solid is removed from the first solid. If the
        /// solids are disjunct, (a clone of the) the first solid is returned. If the second solid contains the
        /// first solid, an empty array is returned.
        /// </summary>
        /// <param name="first">first solid</param>
        /// <param name="second">second solid</param>
        /// <returns>the difference</returns>
        static public Solid[] Subtract(Solid first, Solid second)
        {
            if (Settings.GlobalSettings.GetBoolValue("UseNewBrepOperations", false))
            {
                // hier fehlt noch der Löcherkäse: Solids mit mehreren Shells
                BRepOperation bro = new BRepOperation(first.shells[0], second.shells[0], BRepOperation.Operation.difference);
                Shell[] res = bro.Result();
                Solid[] sres = new Solid[res.Length];
                for (int i = 0; i < res.Length; i++)
                {
                    sres[i] = MakeSolid(res[i]);
                }
                return sres;
            }

            return null;
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.IGeoObjectImpl.Decompose ()"/>
        /// </summary>
        /// <returns></returns>
        public override GeoObjectList Decompose()
        {
            Shell[] shells = this.Shells;
            GeoObjectList decomp = new GeoObjectList();
            for (int i = 0; i < shells.Length; ++i)
            {
                IGeoObject go = shells[i].Clone();
                go.CopyAttributes(this);
                decomp.Add(go);
            }
            return decomp;
        }
        internal void PreCalcTriangulation(double precisiton)
        {
            for (int i = 0; i < shells.Length; ++i)
            {
                shells[i].PreCalcTriangulation(precisiton);
            }
        }
        internal bool Simplify(double precision)
        {
            bool simplified = false;
            for (int i = 0; i < shells.Length; i++)
            {
                simplified |= shells[i].Simplify(precision);
            }
            return simplified;
        }
        public Solid[] SplitByPlane(Plane pln)
        {
            BRepOperation brepOp = new BRepOperation(Shells[0], pln);
            Shell[] parts = brepOp.Result();
            Solid[] res = new Solid[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                res[i] = Solid.Construct();
                res[i].SetShell(parts[i]);
            }
            return res;
        }
#region ISerializable Members
        protected Solid(SerializationInfo info, StreamingContext context)
            : base(context)
        {
            SerializationInfoEnumerator e = info.GetEnumerator();
            while (e.MoveNext())
            {
                switch (e.Name)
                {   // um Exceptions zu verhindern und Def bzw. Colordef gleichermaßen zu lesen
                    default:
                        base.SetSerializationValue(e.Name, e.Value);
                        break;
                    case "Shells":
                        shells = e.Value as Shell[];
                        break;
                    case "Def": // hieß früher dummerweise "Def"
                    case "ColorDef":
                        colorDef = e.Value as ColorDef;
                        break;
                    case "Name":
                        name = e.Value as string;
                        break;
                }
            }
            //shells = info.GetValue("Shells", typeof(Shell[])) as Shell[];
            //colorDef = ColorDef.Read(info, context);
            //if (colorDef == null)
            //{
            //    colorDef = ColorDef.Read("Def", info, context);
            //}
            //try
            //{
            //    name = info.GetString("Name");
            //}
            //catch (SerializationException)
            //{
            //    name = null;
            //}
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Shells", shells, typeof(Shell[]));
            info.AddValue("ColorDef", colorDef);
            info.AddValue("Name", name);
        }
        void IJsonSerialize.SetObjectData(IJsonReadData data)
        {
            base.JsonSetObjectData(data);
            shells = data.GetProperty<Shell[]>("Shells");
            colorDef = data.GetPropertyOrDefault<ColorDef>("ColorDef");
            name = data.GetPropertyOrDefault<string>("Name");
            flags = data.GetPropertyOrDefault<Flags>("Flags");
            data.RegisterForSerializationDoneCallback(this);
        }
        void IJsonSerialize.GetObjectData(IJsonWriteData data)
        {
            base.JsonGetObjectData(data);
            data.AddProperty("Shells", shells);
            data.AddProperty("ColorDef", colorDef);
            data.AddProperty("Name", name);
            data.AddProperty("Flags", flags);
        }
#endregion
#region IDeserializationCallback Members
        void IDeserializationCallback.OnDeserialization(object sender)
        {
            for (int i = 0; i < shells.Length; ++i)
            {
                shells[i].Owner = this;
                shells[i].Layer = this.Layer;
            }
            if (Constructed != null) Constructed(this);
        }
        void IJsonSerializeDone.SerializationDone()
        {
            for (int i = 0; i < shells.Length; ++i)
            {
                shells[i].Owner = this;
                shells[i].Layer = this.Layer;
            }
            if (Constructed != null) Constructed(this);
        }
#endregion
#region IColorDef Members
        private ColorDef colorDef;
        public ColorDef ColorDef
        {
            get
            {
                return colorDef;
            }
            set
            {
                using (new ChangingAttribute(this, "ColorDef", colorDef))
                {
                    colorDef = value;
                    if (shells != null)
                    {
                        for (int i = 0; i < shells.Length; ++i)
                        {
                            shells[i].ColorDef = value;
                        }
                    }
                }
            }
        }
        void IColorDef.SetTopLevel(ColorDef newValue)
        {
            colorDef = newValue;
        }
        void IColorDef.SetTopLevel(ColorDef newValue, bool overwriteChildNullColor)
        {
            colorDef = newValue;
            if (overwriteChildNullColor)
            {
                if (shells != null)
                {
                    for (int i = 0; i < shells.Length; ++i)
                    {
                        if (shells[i].ColorDef == null) (shells[i] as IColorDef).SetTopLevel(newValue, true);
                    }
                }
            }
        }
#endregion
#region IGetSubShapes Members
        IGeoObject IGetSubShapes.GetEdge(int[] id, int index)
        {
            for (int i = 0; i < shells.Length; ++i)
            {
                if (shells[i].UniqueId == id[index])
                    return (shells[i] as IGetSubShapes).GetEdge(id, index + 1);
            }
            return null; // sollte nicht vorkommen
        }
        IGeoObject IGetSubShapes.GetFace(int[] id, int index)
        {
            for (int i = 0; i < shells.Length; ++i)
            {
                if (shells[i].UniqueId == id[index])
                    return (shells[i] as IGetSubShapes).GetFace(id, index + 1);
            }
            return null; // sollte nicht vorkommen
        }
#endregion
#region IGeoObjectOwner Members
        void IGeoObjectOwner.Remove(IGeoObject toRemove)
        {
            // Remove sollte nicht drankommen, es sei denn beim Zerlegen.
            // dann könnte es ein Problem mit dem Undo geben. Dort sollte besser gecloned werden
        }
        void IGeoObjectOwner.Add(IGeoObject toAdd)
        {
            // Add machen wir nur selbst, wenn das Objekt erzeugt wird, da bleibt hier nichts zu tun
        }
#endregion

        internal void AssertOutwardOrientation()
        {
            shells[0].AssertOutwardOrientation(); // there must be at least one Shell (and in most cases this is the only one)
            for (int i = 1; i < shells.Length; ++i)
            {   // all inner Shells are reverse oriented, i.e. the (normals of the) faces point away from the solid, into the inner part of the holes
                shells[i].AssertOutwardOrientation();
                shells[i].ReverseOrientation();
            }
            flags |= Flags.oriented;
        }

        int IExportStep.Export(ExportStep export, bool topLevel)
        {   // MANIFOLD_SOLID_BREP is a Geometric_Representation_Item
            int shellnr = (shells[0] as IExportStep).Export(export, false);
            int msb = export.WriteDefinition("MANIFOLD_SOLID_BREP('" + NameOrEmpty + "',#" + shellnr.ToString() + ")");
            if (colorDef != null)
            {
                colorDef.MakeStepStyle(msb, export);
            }
            //#731 = PRODUCT( 'A0501_SASIL_plus_00_50_185_3_polig', 'A0501_SASIL_plus_00_50_185_3_polig', 'PART-A0501_SASIL_plus_00_50_185_3_polig-DESC', ( #2 ) );
            //#1442 = PRODUCT_DEFINITION_FORMATION_WITH_SPECIFIED_SOURCE( ' ', 'NONE', #731, .NOT_KNOWN. );
            //#732 = PRODUCT_DEFINITION( 'NONE', 'NONE', #1442, #3 );
            //#253 = PRODUCT_DEFINITION_SHAPE( 'NONE', 'NONE', #732 );
            //#254 = ADVANCED_BREP_SHAPE_REPRESENTATION( 'A0501_SASIL_plus_00_50_185_3_polig', ( #733 ), #4 );
            //#9 = SHAPE_DEFINITION_REPRESENTATION( #253, #254 );
            int product = export.WriteDefinition("PRODUCT( '" + NameOrEmpty + "','" + NameOrEmpty + "','',(#2))");
            int pdf = export.WriteDefinition("PRODUCT_DEFINITION_FORMATION_WITH_SPECIFIED_SOURCE( ' ', 'NONE', #"+product.ToString()+", .NOT_KNOWN. )");
            int pd = export.WriteDefinition("PRODUCT_DEFINITION( 'NONE', 'NONE', #" + pdf.ToString() + ", #3 )");
            int pds = export.WriteDefinition("PRODUCT_DEFINITION_SHAPE( 'NONE', 'NONE', #" + pd.ToString() + " )");
            int brep = export.WriteDefinition("ADVANCED_BREP_SHAPE_REPRESENTATION('" + NameOrEmpty + "', ( #" + msb.ToString() + "), #4 )");
            export.WriteDefinition("SHAPE_DEFINITION_REPRESENTATION( #" + pds.ToString() + ", #" + brep.ToString() + ")");
            return brep;
        }
    }
}
