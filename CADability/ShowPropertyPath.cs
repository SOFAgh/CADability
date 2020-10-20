using CADability.Actions;
using CADability.Curve2D;
using CADability.GeoObject;
using CADability.Shapes;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CADability.UserInterface
{
    /// <summary>
    /// Shows the properties of a path.
    /// </summary>

    public class ShowPropertyPath : IShowPropertyImpl, IDisplayHotSpots, ICommandHandler, IGeoObjectShowProperty
    {
        private Path path;
        private IFrame frame;
        private IShowProperty[] subEntries; // abhängig von der Form, also Rechteck, Parallelogramm
        private IShowProperty[] attributeProperties; // Anzeigen für die Attribute (Ebene, Farbe u.s.w)
        private DoubleProperty area, length;
        public ShowPropertyPath(Path path, IFrame frame)
        {
            this.path = path;
            this.frame = frame;
            base.resourceId = "Path.Object";
            InitSubEntries();
        }
        private class VertexCommandHandler : ICommandHandler
        {
            ShowPropertyPath showPropertyPath;
            int index;
            public VertexCommandHandler(ShowPropertyPath showPropertyPath, int index)
            {
                this.showPropertyPath = showPropertyPath;
                this.index = index;
            }
            #region ICommandHandler Members

            bool ICommandHandler.OnCommand(string MenuId)
            {
                switch (MenuId)
                {
                    case "MenuId.Path.Vertex.StartWithMe":
                        {
                            showPropertyPath.path.CyclicalPermutation(index);
                        }
                        return true;
                }
                return false;
            }

            bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
            {
                switch (MenuId)
                {
                    case "MenuId.Path.Vertex.StartWithMe":
                        {
                            CommandState.Enabled = showPropertyPath.path.IsClosed;
                        }
                        return true;
                }
                return false;
            }
            void ICommandHandler.OnSelected(string MenuId, bool selected) { }

            #endregion
        }
        private void InitSubEntries()
        {
            ArrayList gp = new ArrayList();
            // wenn der Pfad zu viele Eckpunkte hat, gibts Probleme mit der WindowHandles
            for (int i = 0; i <= path.CurveCount; ++i) // mit Endpunkt
            {
                GeoPointProperty vertex = new GeoPointProperty("Path.Vertex", frame, true);
                vertex.UserData.Add("Index", i);
                vertex.GetGeoPointEvent += new CADability.UserInterface.GeoPointProperty.GetGeoPointDelegate(OnGetVertexPoint);
                vertex.SetGeoPointEvent += new CADability.UserInterface.GeoPointProperty.SetGeoPointDelegate(OnSetVertexPoint);
                vertex.ModifyWithMouseEvent += new ModifyWithMouseDelegate(ModifyVertexWithMouse);
                vertex.StateChangedEvent += new StateChangedDelegate(OnVertexStateChanged);
                vertex.GeoPointChanged();
                if (path.IsClosed)
                {
                    vertex.PrependContextMenu = MenuResource.LoadMenuDefinition("MenuId.Path.Vertex", false, new VertexCommandHandler(this, i));
                }

                gp.Add(vertex);
            }
            area = new DoubleProperty("Path.Area", frame);
            area.ReadOnly = true;
            area.GetDoubleEvent += new CADability.UserInterface.DoubleProperty.GetDoubleDelegate(OnGetArea);
            area.Refresh();
            gp.Add(area);
            length = new DoubleProperty("Path.Length", frame);
            length.ReadOnly = true;
            length.GetDoubleEvent += new CADability.UserInterface.DoubleProperty.GetDoubleDelegate(OnGetLength);
            length.Refresh();
            gp.Add(length);
            subEntries = (IShowProperty[])gp.ToArray(typeof(IShowProperty));
            attributeProperties = path.GetAttributeProperties(frame);
        }

        void OnVertexStateChanged(IShowProperty sender, StateChangedArgs args)
        {
            if (HotspotChangedEvent != null)
            {
                GeoPointProperty vertexProperty = null;
                if (sender is GeoPointProperty)
                {
                    vertexProperty = (sender as GeoPointProperty);
                }
                if (vertexProperty == null) return;
                if (args.EventState == StateChangedArgs.State.Selected || args.EventState == StateChangedArgs.State.SubEntrySelected)
                {
                    HotspotChangedEvent(vertexProperty, HotspotChangeMode.Selected);
                }
                else if (args.EventState == StateChangedArgs.State.UnSelected)
                {
                    HotspotChangedEvent(vertexProperty, HotspotChangeMode.Deselected);
                }
            }
        }
        private void PathDidChange(IGeoObject Sender, GeoObjectChange Change)
        {
            area.Refresh();
        }
        public override void Opened(bool IsOpen)
        {
            if (HotspotChangedEvent != null)
            {
                for (int i = 0; i < subEntries.Length; ++i)
                {
                    IHotSpot hsp = subEntries[i] as IHotSpot;
                    if (hsp != null)
                    {
                        if (IsOpen)
                        {
                            HotspotChangedEvent(hsp, HotspotChangeMode.Visible);
                        }
                        else
                        {
                            HotspotChangedEvent(hsp, HotspotChangeMode.Invisible);
                        }
                    }
                }
            }
            base.Opened(IsOpen);
        }
#region IShowPropertyImpl Overrides
        public override ShowPropertyEntryType EntryType
        {
            get
            {
                return ShowPropertyEntryType.GroupTitle;
            }
        }
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                return ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.Selectable;
            }
        }
        public override MenuWithHandler[] ContextMenu
        {
            get
            {
                List<MenuWithHandler> items = new List<MenuWithHandler>(MenuResource.LoadMenuDefinition("MenuId.Object.Path", false, this));
                CreateContextMenueEvent?.Invoke(this, items);
                path.GetAdditionalContextMenue(this, Frame, items);
                return items.ToArray();
            }
        }
        public override int SubEntriesCount
        {
            get
            {
                return subEntries.Length + attributeProperties.Length;
            }
        }
        public override IShowProperty[] SubEntries
        {
            get
            {
                if (subEntries == null) return attributeProperties;
                else return IShowPropertyImpl.Concat(subEntries, attributeProperties);
            }
        }
        public override void Added(IPropertyPage propertyTreeView)
        {	// die events müssen in Added angemeldet und in Removed wieder abgemeldet werden,
            // sonst bleibt die ganze ShowProperty für immer an der Linie hängen
            path.DidChangeEvent += new ChangeDelegate(PathDidChange);
            path.UserData.UserDataAddedEvent += new UserData.UserDataAddedDelegate(OnUserDataAdded);
            path.UserData.UserDataRemovedEvent += new UserData.UserDataRemovedDelegate(OnUserDataAdded);
            base.Added(propertyTreeView);
        }
        void OnUserDataAdded(string name, object value)
        {
            this.subEntries = null;
            InitSubEntries();
            attributeProperties = path.GetAttributeProperties(frame);
            propertyTreeView.Refresh(this);
        }
        public override void Removed(IPropertyTreeView propertyTreeView)
        {
            path.DidChangeEvent -= new ChangeDelegate(PathDidChange);
            path.UserData.UserDataAddedEvent -= new UserData.UserDataAddedDelegate(OnUserDataAdded);
            path.UserData.UserDataRemovedEvent -= new UserData.UserDataRemovedDelegate(OnUserDataAdded);
            base.Removed(propertyTreeView);
        }
#endregion
#region IDisplayHotSpots Members

        public event CADability.HotspotChangedDelegate HotspotChangedEvent;

        public void ReloadProperties()
        {
            // TODO:  Add ShowPropertyPath.ReloadProperties implementation
        }

#endregion
        private GeoPoint OnGetVertexPoint(GeoPointProperty sender)
        {
            int index = (int)sender.UserData.GetData("Index");
            if (index < path.CurveCount) return path.Curve(index).StartPoint;
            return path.Curve(index - 1).EndPoint;
        }
        private void OnSetVertexPoint(GeoPointProperty sender, GeoPoint p)
        {
            int indexStartPoint = (int)sender.UserData.GetData("Index");
            GeoPoint[] vtx = path.Vertices;
            if (indexStartPoint == 0)
            {
                if (path.IsClosed)
                {
                    if (Precision.IsEqual(p, vtx[vtx.Length - 1])) return; // das letzte Segment wird 0
                    if (Precision.IsEqual(p, vtx[1])) return; // das letzte Segment wird 0
                }
            }
            else
            {
                if (Precision.IsEqual(vtx[indexStartPoint - 1], p)) return; // das Vorgänger-Segment wird 0
            }
            if (indexStartPoint == vtx.Length - 1)
            {
                if (path.IsClosed)
                {
                    if (Precision.IsEqual(p, vtx[1])) return; // das erste Segment wird 0
                }
            }
            else if (indexStartPoint < vtx.Length)
            {
                if (Precision.IsEqual(vtx[indexStartPoint + 1], p)) return; // das Nachfolger-Segment wird 0
            }
            int indexEndPoint = -1;
            if (indexStartPoint == 0 && path.IsClosed)
            {	// der 1. Punkt und geschlossen
                indexEndPoint = path.CurveCount - 1; // Endpunkt vom letzten Segment
            }
            else if (indexStartPoint == path.CurveCount)
            {	// der letzte Punkt
                if (path.IsClosed)
                {
                    indexEndPoint = indexStartPoint - 1;
                    indexStartPoint = 0;
                }
                else
                {
                    indexEndPoint = indexStartPoint - 1;
                    indexStartPoint = -1;
                }
            }
            if (indexStartPoint > 0) indexEndPoint = indexStartPoint - 1;
            bool dbg1 = path.IsClosed;
            Path dbgpath = path.Clone() as Path;
            path.SetPoint(indexStartPoint, p, Path.ModificationMode.keepArcRatio); // versuchsweise
            bool dbg2 = path.IsClosed;
            if (dbg1 != dbg2)
            {
                dbgpath.SetPoint(indexStartPoint, p, Path.ModificationMode.keepArcRatio); // versuchsweise
            }
            // path.SetPoint(indexEndPoint, indexStartPoint, p);
        }
        private void ModifyVertexWithMouse(IPropertyEntry sender, bool StartModifying)
        {
            GeneralGeoPointAction gpa = new GeneralGeoPointAction(sender as GeoPointProperty, path);
            frame.SetAction(gpa);
        }
#region ICommandHandler Members
        public bool OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.Reverse":
                    (path as ICurve).Reverse();
                    if (propertyTreeView != null)
                        propertyTreeView.Refresh(this);
                    return true;
                case "MenuId.CurveSplit":
                    frame.SetAction(new ConstrSplitCurve(path));
                    return true;
                case "MenuId.Approximate":
                    if (frame.ActiveAction is SelectObjectsAction)
                    {
                        Curves.Approximate(frame, path);
                    }
                    return true;
                case "MenuId.Explode":
                    if (frame.ActiveAction is SelectObjectsAction)
                    {
                        using (frame.Project.Undo.UndoFrame)
                        {
                            IGeoObjectOwner addTo = path.Owner;
                            if (addTo == null) addTo = frame.ActiveView.Model;
                            ICurve[] pathCurves = path.Curves;
                            GeoObjectList toSelect = path.Decompose();
                            addTo.Remove(path);
                            for (int i = toSelect.Count - 1; i >= 0; --i)
                            {
                                if (!toSelect[i].HasValidData()) toSelect.Remove(i);
                            }
                            for (int i = 0; i < toSelect.Count; i++)
                            {
                                addTo.Add(toSelect[i]);
                            }
                            SelectObjectsAction soa = frame.ActiveAction as SelectObjectsAction;
                            soa.SetSelectedObjects(toSelect); // alle Teilobjekte markieren
                        }
                    }
                    return true;
                case "MenuId.Aequidist":
                    frame.SetAction(new ConstructAequidist(path));
                    return true;
                case "MenuId.Reduce":
                    if (path.GetPlanarState() == PlanarState.Planar)
                    {
                        Plane pln = path.GetPlane();
                        Path2D p2d = path.GetProjectedCurve(pln) as Path2D;
                        if (p2d != null)
                        {
                            p2d.ForceConnected();
                            Reduce2D r2d = new Reduce2D();
                            r2d.Precision = Settings.GlobalSettings.GetDoubleValue("Approximate.Precision", 0.01);
                            r2d.Add(p2d.SubCurves);
                            r2d.OutputMode = Reduce2D.Mode.Paths;
                            ICurve2D[] red = r2d.Reduced;
                            if (red.Length == 1)
                            {
                                using (frame.Project.Undo.UndoFrame)
                                {
                                    IGeoObjectOwner addTo = path.Owner;
                                    if (addTo == null) addTo = frame.ActiveView.Model;
                                    addTo.Remove(path);
                                    Path redpath = red[0].MakeGeoObject(pln) as Path;
                                    if (redpath != null)
                                    {
                                        SelectObjectsAction soa = frame.ActiveAction as SelectObjectsAction;
                                        soa.SetSelectedObjects(new GeoObjectList(redpath));
                                    }
                                }
                            }
                        }
                    }
                    return true;
            }
            return false;
        }
        public bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            switch (MenuId)
            {
                case "MenuId.Reverse":
                case "MenuId.CurveSplit":
                case "MenuId.Approximate":
                case "MenuId.Explode":
                case "MenuId.Aequidist":
                    CommandState.Enabled = true; // hier müssen die Flächen rein
                    return true;
            }
            return false;
        }
        void ICommandHandler.OnSelected(string MenuId, bool selected) { }
        #endregion
        #region IGeoObjectShowProperty Members
        public event CADability.GeoObject.CreateContextMenueDelegate CreateContextMenueEvent;
        IGeoObject IGeoObjectShowProperty.GetGeoObject()
        {
            return path;
        }
        string IGeoObjectShowProperty.GetContextMenuId()
        {
            return "MenuId.Object.Path";
        }
#endregion
        private double OnGetArea(DoubleProperty sender)
        {
            if (path.IsClosed && path.GetPlanarState() == PlanarState.Planar)
            {
                Plane plane = path.GetPlane();
                ICurve2D cv = path.GetProjectedCurve(plane);
                Border bdr = new Border(cv);
                return Math.Abs(bdr.Area);
                // return cv.GetAreaFromPoint(cv.GetExtent().GetCenter());
            }
            return 0;
        }
        private double OnGetLength(DoubleProperty sender)
        {
            return path.Length;
        }
    }
}
