using CADability.Actions;
using CADability.Attribute;
using CADability.GeoObject;
using CADability.Substitutes;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wintellect.PowerCollections;
using static CADability.Actions.SelectObjectsAction;

namespace CADability
{
    class SelectActionContextMenu : ICommandHandler
    {
        private SelectObjectsAction soa;
        private List<Face> faces;
        private List<Shell> shells;
        private List<Solid> solids;
        private GeoObjectList curves;
        private List<Edge> edges;
        private GeoObjectList currentMenuSelection;
        private IPaintTo3DList displayList;
        private IView currentView;
        public SelectActionContextMenu(SelectObjectsAction soa)
        {
            this.soa = soa;
            currentMenuSelection = new GeoObjectList();
        }
        public void FilterMouseMessages(MouseAction mouseAction, MouseEventArgs e, IView vw, ref bool handled)
        {
            if (mouseAction == MouseAction.MouseUp && e.Button == MouseButtons.Right)
            {
                handled = true;
                ShowContextMenu(e.Location, vw);
            }
        }
        private void ShowContextMenu(System.Drawing.Point mousePoint, IView vw)
        {
            currentView = vw;
            GeoObjectList result = new GeoObjectList();

            int pickRadius = soa.Frame.GetIntSetting("Select.PickRadius", 5);
            Projection.PickArea pa = vw.Projection.GetPickSpace(new System.Drawing.Rectangle(mousePoint.X - pickRadius, mousePoint.Y - pickRadius, pickRadius * 2, pickRadius * 2));
            IActionInputView pm = vw as IActionInputView;
            GeoObjectList fl = vw.Model.GetObjectsFromRect(pa, new Set<Layer>(pm.GetVisibleLayers()), PickMode.onlyFaces, null); // returns all the face under the cursor
            // in most cases there is only a single face, which is of interest, only when we have two solids with same or overlapping faces
            // and one of them is not selectable without also selecting the other, we want both.
            faces = new List<Face>();
            shells = new List<Shell>(); // only Shells, which have are not part of a Solid
            solids = new List<Solid>();
            double delta = vw.Model.Extent.Size * 1e-4;
            double mindist = double.MaxValue;
            for (int i = 0; i < fl.Count; i++)
            {
                if (fl[i] is Face face) // this should always be the case
                {
                    double z = face.Position(pa.FrontCenter, pa.Direction, 0);
                    if (z < mindist)
                    {
                        if (z < mindist - delta) faces.Clear();
                        faces.Add(face);
                        mindist = z;
                    }
                }
            }
            HashSet<Edge> relevantEdges = new HashSet<Edge>();
            for (int i = 0; i < faces.Count; i++)
            {
                relevantEdges.UnionWith(faces[i].AllEdges);
                if (faces[i].Owner is Shell shell)
                {
                    if (shell.Owner is Solid sld) { if (!solids.Contains(sld)) solids.Add(sld); }
                    else { if (!shells.Contains(shell)) shells.Add(shell); }
                }
            }
            curves = vw.Model.GetObjectsFromRect(pa, new Set<Layer>(pm.GetVisibleLayers()), PickMode.onlyEdges, null); // returns edges and curves
            edges = new List<Edge>();
            // we only accept edges, which belong to one of the selected faces
            for (int i = curves.Count - 1; i >= 0; --i)
            {
                if (curves[i].Owner is Edge edge)
                {
                    if (relevantEdges.Contains(edge)) edges.Add(edge);
                    curves.Remove(i);
                }
                else
                {
                    double z = curves[i].Position(pa.FrontCenter, pa.Direction, 0);
                    if (z - delta > mindist) curves.Remove(i);
                }
            }

            // now we have curves, edges, faces, shells and solids (text, hatch, block, dimension not yet implemented)

            List<MenuWithHandler> cm = new List<MenuWithHandler>();
            MenuWithHandler mhdumy = new MenuWithHandler();
            mhdumy.ID = "MenuId.dumy";
            mhdumy.Text = "dummy menu entry";
            for (int i = 0; i < curves.Count; i++)
            {
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Curve." + i.ToString();
                mh.Text = curves[i].Description;
                mh.SubMenus = new MenuWithHandler[] { mhdumy };
                mh.Target = this;
                cm.Add(mh);
            }
            for (int i = 0; i < edges.Count; i++)
            {
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Edge." + i.ToString();
                mh.Text = StringTable.GetString("MenuId.Edge", StringTable.Category.label);
                mh.SubMenus = edges[i].GetContextMenu(soa.Frame);
                mh.Target = this;
                cm.Add(mh);
            }
            for (int i = 0; i < faces.Count; i++)
            {
                MenuWithHandler mh = new MenuWithHandler();
                cm.AddRange(GetFacesSubmenus(faces));
            }
            for (int i = 0; i < shells.Count; i++)
            {
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Shell." + i.ToString();
                mh.Text = StringTable.GetString("MenuId.Shell", StringTable.Category.label);
                mh.SubMenus = shells[i].GetContextMenu(soa.Frame);
                mh.Target = this;
                cm.Add(mh);
            }
            for (int i = 0; i < solids.Count; i++)
            {
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Solid." + i.ToString();
                mh.Text = StringTable.GetString("MenuId.Solid", StringTable.Category.label);
                mh.SubMenus = solids[i].GetContextMenu(soa.Frame);
                mh.Target = this;
                cm.Add(mh);
            }
            {   // selection independent menu items
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Show";
                mh.Text = StringTable.GetString("MenuId.Show", StringTable.Category.label);
                MenuWithHandler mhShowHidden = new MenuWithHandler();
                mhShowHidden.ID = "MenuId.ShowHidden";
                mhShowHidden.Text = StringTable.GetString("MenuId.ShowHidden", StringTable.Category.label);
                mhShowHidden.OnCommand = (menuId) =>
                {
                    foreach (IGeoObject geoObject in vw.Model)
                    {
                        if (geoObject.Layer != null && geoObject.Layer.Name == "CADability.Hidden")
                        {
                            Layer layer = geoObject.UserData.GetData("CADability.OriginalLayer") as Layer;
                            if (layer != null) geoObject.Layer = layer;
                        }
                    }
                    return true;
                };
                MenuWithHandler mhShowAxis = new MenuWithHandler();
                mhShowAxis.ID = "MenuId.ShowAxis";
                mhShowAxis.Text = StringTable.GetString("MenuId.ShowAxis", StringTable.Category.label);
                mhShowAxis.OnCommand = (menuId) =>
                {
                    bool isOn = false;
                    foreach (IGeoObject geoObject in vw.Model)
                    {
                        Shell shell = null;
                        if (geoObject is Solid solid) shell = solid.Shells[0];
                        else if (geoObject is Shell sh) shell = sh;
                        if (shell != null && shell.FeatureAxis.Count > 0)
                        {
                            isOn = shell.FeatureAxis[0].IsVisible;
                            break;
                        }
                    }

                    using (vw.Canvas.Frame.Project.Undo.UndoFrame)
                    {
                        foreach (IGeoObject geoObject in vw.Model)
                        {
                            if (geoObject is Solid solid) solid.ShowFeatureAxis = !isOn;
                            else if (geoObject is Shell shell) shell.ShowFeatureAxis = !isOn;
                        }
                    }
                    return true;
                };
                mhShowAxis.OnUpdateCommand = (menuId, commandState) =>
                {
                    bool isOn = false;
                    foreach (IGeoObject geoObject in vw.Model)
                    {
                        Shell shell = null;
                        if (geoObject is Solid solid) shell = solid.Shells[0];
                        else if (geoObject is Shell sh) shell = sh;
                        if (shell != null && shell.FeatureAxis.Count > 0)
                        {
                            isOn = shell.FeatureAxis[0].IsVisible;
                            break;
                        }
                    }
                    commandState.Checked = isOn;
                    return true;
                };
                mh.SubMenus = new MenuWithHandler[] { mhShowHidden, mhShowAxis };
                mh.Target = this;
                cm.Add(mh);
            }
            vw.SetPaintHandler(PaintBuffer.DrawingAspect.Select, new PaintView(OnRepaintSelect));
            mousePoint.X += vw.DisplayRectangle.Width / 8; // find a better place for the menu position, using the extent of the objects
            vw.Canvas.ShowContextMenu(cm.ToArray(), mousePoint, ContextMenuCollapsed);
        }

        private class FeatureCommandHandler : MenuWithHandler, ICommandHandler
        {
            public Face[] involvedFaces;
            SelectActionContextMenu selectActionContextMenu;
            public FeatureCommandHandler(Face[] involvedFaces, SelectActionContextMenu selectActionContextMenu)
            {
                this.involvedFaces = involvedFaces;
                this.selectActionContextMenu = selectActionContextMenu;
            }
            bool ICommandHandler.OnCommand(string MenuId)
            {
                switch (MenuId)
                {
                    case "MenuId.Fillet.ChangeRadius":
                        ParametricsRadius pr = new ParametricsRadius(involvedFaces, selectActionContextMenu.soa.Frame, true);
                        selectActionContextMenu.soa.Frame.SetAction(pr);
                        return true;
                    case "MenuId.Fillet.Remove":
                        Shell orgShell = involvedFaces[0].Owner as Shell;
                        if (orgShell != null)
                        {
                            RemoveFillet rf = new RemoveFillet(involvedFaces[0].Owner as Shell, new HashSet<Face>(involvedFaces));
                            Shell sh = rf.Result();
                            if (sh != null)
                            {
                                using (selectActionContextMenu.soa.Frame.Project.Undo.UndoFrame)
                                {
                                    sh.CopyAttributes(orgShell);
                                    IGeoObjectOwner owner = orgShell.Owner;
                                    owner.Remove(orgShell);
                                    owner.Add(sh);
                                }
                            }
                        }
                        return true;
                }
                return false;
            }

            void ICommandHandler.OnSelected(MenuWithHandler selectedMenuItem, bool selected)
            {
                selectActionContextMenu.currentMenuSelection.Clear();
                selectActionContextMenu.currentMenuSelection.AddRange(involvedFaces);
                selectActionContextMenu.currentView.Invalidate(PaintBuffer.DrawingAspect.Select, selectActionContextMenu.currentView.DisplayRectangle);
            }

            bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
            {
                return true;
            }
        }
        private List<MenuWithHandler> GetFacesSubmenus(List<Face> faces)
        {
            List<MenuWithHandler> res = new List<MenuWithHandler>();
            for (int i = 0; i < faces.Count; i++)
            {
                if (faces[i].IsFillet())
                {
                    HashSet<Face> connectedFillets = new HashSet<Face>();
                    CollectConnectedFillets(faces[i], connectedFillets);
                    FeatureCommandHandler fch = new FeatureCommandHandler(connectedFillets.ToArray(), this);
                    MenuWithHandler mFillet = new MenuWithHandler("MenuId.Fillet");
                    mFillet.OnCommand = (menuId) => { return true; }; // it has sub-menus, nothing to do here
                    mFillet.OnSelected = (mh, selected) =>
                    {
                        currentMenuSelection.Clear();
                        currentMenuSelection.AddRange(connectedFillets.ToArray());
                        currentView.Invalidate(PaintBuffer.DrawingAspect.Select, currentView.DisplayRectangle);
                    };
                    MenuWithHandler fr = new MenuWithHandler("MenuId.Fillet.ChangeRadius");
                    fr.OnCommand = (menuId) =>
                    {
                        ParametricsRadius pr = new ParametricsRadius(connectedFillets.ToArray(), soa.Frame, true);
                        soa.Frame.SetAction(pr);
                        return true;
                    };
                    MenuWithHandler fd = new MenuWithHandler("MenuId.Fillet.Remove");
                    fd.OnCommand = (menuId) =>
                    {
                        Face[] involvedFaces = connectedFillets.ToArray();
                        Shell orgShell = involvedFaces[0].Owner as Shell;
                        if (orgShell != null)
                        {
                            RemoveFillet rf = new RemoveFillet(involvedFaces[0].Owner as Shell, new HashSet<Face>(involvedFaces));
                            Shell sh = rf.Result();
                            if (sh != null)
                            {
                                using (soa.Frame.Project.Undo.UndoFrame)
                                {
                                    sh.CopyAttributes(orgShell);
                                    IGeoObjectOwner owner = orgShell.Owner;
                                    owner.Remove(orgShell);
                                    owner.Add(sh);
                                }
                            }
                        }
                        return true;
                    };
                    fch.SubMenus = new MenuWithHandler[] { fr, fd };
                    res.Add(fch);
                }
            }
            for (int i = 0; i < faces.Count; i++)
            {
                IEnumerable<Face> connected = faces[i].GetSameSurfaceConnected();
                if (connected.Any())
                {
                    List<Face> lconnected = new List<Face>(connected);
                    lconnected.Add(faces[i]);
                    // maybe a full sphere, cone, cylinder or torus:
                    // except for the sphere: position axis
                    // except for the cone: change radius or diameter
                    // for the cone: smaller and larger diameter
                    // for cone and cylinder: total length
                    if (faces[i].Surface is CylindricalSurface || faces[i].Surface is ToroidalSurface)
                    {
                        MenuWithHandler mh = new MenuWithHandler("MenuId.FeatureDiameter");
                        mh.OnCommand = (menuId) =>
                        {
                            ParametricsRadius pr = new ParametricsRadius(lconnected.ToArray(), soa.Frame, false);
                            soa.Frame.SetAction(pr);
                            return true;
                        };
                        res.Add(mh);
                    }
                    if (faces[i].Surface is CylindricalSurface || faces[i].Surface is ConicalSurface)
                    {
                        Line axis = null;
                        if (faces[i].Surface is CylindricalSurface cyl) axis = cyl.AxisLine(faces[i].Domain.Bottom, faces[i].Domain.Top);
                        if (faces[i].Surface is ConicalSurface cone) axis = cone.AxisLine(faces[i].Domain.Bottom, faces[i].Domain.Top);
                        MenuWithHandler mh = new MenuWithHandler("MenuId.AxisPosition");
                        mh.OnCommand = (menuId) =>
                        {
                            ParametricsDistance pd = new ParametricsDistance(lconnected, axis);
                            soa.Frame.SetAction(pd);
                            return true;
                        };
                        res.Add(mh);
                    }
                }
            }
            for (int i = 0; i < faces.Count; i++)
            {
                if (faces[i].Surface is PlaneSurface pls)
                {
                    // try to find parallel outline edges to modify the distance
                    Edge[] outline = faces[i].OutlineEdges;
                    for (int j = 0; j < outline.Length - 1; j++)
                    {
                        for (int k = j + 1; k < outline.Length; k++)
                        {
                            if (outline[j].Curve3D is Line l1 && outline[k].Curve3D is Line l2)
                            {
                                if (Precision.SameDirection(l1.StartDirection, l2.StartDirection, false))
                                {
                                    // two parallel outline lines, we could parametrize the distance
                                    MenuWithHandler mh = new MenuWithHandler("MenuId.EdgeDistance");
                                    Edge o1 = outline[j];
                                    Edge o2 = outline[k]; // outline[i] is not captured correctly for the anonymous method. I don't know why. With local copies, it works.
                                    double lmin = 0.0;
                                    double lmax = 1.0;
                                    double p = Geometry.LinePar(l1.StartPoint, l1.EndPoint, l2.StartPoint);
                                    lmin = Math.Min(lmin, p);
                                    lmax = Math.Max(lmax, p);
                                    p = Geometry.LinePar(l1.StartPoint, l1.EndPoint, l2.EndPoint);
                                    lmin = Math.Min(lmin, p);
                                    lmax = Math.Max(lmax, p);
                                    GeoPoint p1 = Geometry.LinePos(l1.StartPoint, l1.EndPoint, (lmin + lmax) / 2.0);
                                    GeoPoint p2 = Geometry.DropPL(p1, l2.StartPoint, l2.EndPoint);
                                    Line feedback = Line.TwoPoints(p1, p2);
                                    mh.OnCommand = (menuId) =>
                                    {
                                        ParametricsDistance pd = new ParametricsDistance(o1, o2, feedback);
                                        soa.Frame.SetAction(pd);
                                        return true;
                                    };
                                    mh.OnSelected = (m, selected) =>
                                    {
                                        currentMenuSelection.Clear();
                                        currentMenuSelection.Add(feedback);
                                        currentView.Invalidate(PaintBuffer.DrawingAspect.Select, currentView.DisplayRectangle);
                                    };
                                    res.Add(mh);
                                }
                            }
                        }
                    }
                }
            }
            return res;
        }

        private void CollectConnectedFillets(Face face, HashSet<Face> connectedFillets)
        {
            if (!connectedFillets.Contains(face))
            {
                connectedFillets.Add(face);
                if (face.Surface is ISurfaceOfArcExtrusion extrusion)
                {
                    foreach (Edge edge in face.OutlineEdges)
                    {
                        Face otherFace = edge.OtherFace(face);
                        if (otherFace.IsFillet() && otherFace.Surface is ISurfaceOfArcExtrusion otherExtrusion && Precision.IsEqual(extrusion.Radius, otherExtrusion.Radius))
                        {
                            // if (edge.Curve2D(face).DirectionAt(0.5).IsMoreHorizontal == extrusion.ExtrusionDirectionIsV && otherFace.Surface is ISurfaceOfArcExtrusion arcExtrusion)
                            {
                                CollectConnectedFillets(otherFace, connectedFillets);
                            }
                        }
                        else if (otherFace.Surface is SphericalSurface ss && Precision.IsEqual(ss.RadiusX, extrusion.Radius))
                        {
                            CollectConnectedFillets(otherFace, connectedFillets);
                        }
                    }
                }
                else if (face.Surface is SphericalSurface ss)
                {   // at a sphere a fillet might branch out
                    foreach (Edge edge in face.OutlineEdges)
                    {
                        Face otherFace = edge.OtherFace(face);
                        if (edge.IsTangentialEdge() && otherFace.IsFillet() && otherFace.Surface is ISurfaceOfArcExtrusion otherExtrusion && Precision.IsEqual(ss.RadiusX, otherExtrusion.Radius))
                        {
                            CollectConnectedFillets(otherFace, connectedFillets);
                        }

                    }
                }
            }
        }

        private void OnRepaintSelect(System.Drawing.Rectangle IsInvalid, IView View, IPaintTo3D PaintToSelect)
        {
            PaintToSelect.SelectMode = true;
            PaintToSelect.SelectColor = System.Drawing.Color.Yellow;
            int wobbleWidth = 3;
            if ((PaintToSelect.Capabilities & PaintCapabilities.ZoomIndependentDisplayList) != 0)
            {
                PaintToSelect.OpenList();
                foreach (IGeoObject go in currentMenuSelection)
                {
                    go.PaintTo3D(PaintToSelect);
                }
                displayList = PaintToSelect.CloseList();
                PaintToSelect.SelectedList(displayList, wobbleWidth);
            }
        }

        private void ContextMenuCollapsed(int dumy)
        {
            currentView.RemovePaintHandler(PaintBuffer.DrawingAspect.Select, new PaintView(OnRepaintSelect));
            currentMenuSelection.Clear();
        }
        bool ICommandHandler.OnCommand(string MenuId)
        {
            return true;
        }

        void ICommandHandler.OnSelected(MenuWithHandler selectedMenu, bool selected)
        {
            currentMenuSelection.Clear();
            if (selectedMenu is FeatureCommandHandler fch)
            {
                currentMenuSelection.AddRange(fch.involvedFaces);
            }
            else if (selectedMenu.ID.StartsWith("MenuId.Curve."))
            {
                int ind = int.Parse(selectedMenu.ID.Substring("MenuId.Curve.".Length));
                currentMenuSelection.Add(curves[ind]);
            }
            else if (selectedMenu.ID.StartsWith("MenuId.Edge."))
            {
                int ind = int.Parse(selectedMenu.ID.Substring("MenuId.Edge.".Length));
                currentMenuSelection.Add(edges[ind].Curve3D as IGeoObject);
            }
            else if (selectedMenu.ID.StartsWith("MenuId.Face."))
            {
                int ind = int.Parse(selectedMenu.ID.Substring("MenuId.Face.".Length));
                currentMenuSelection.Add(faces[ind]);
            }
            else if (selectedMenu.ID.StartsWith("MenuId.Shell."))
            {
                int ind = int.Parse(selectedMenu.ID.Substring("MenuId.Shell.".Length));
                currentMenuSelection.Add(shells[ind]);
            }
            else if (selectedMenu.ID.StartsWith("MenuId.Solid."))
            {
                int ind = int.Parse(selectedMenu.ID.Substring("MenuId.Solid.".Length));
                currentMenuSelection.Add(solids[ind]);
            }
            else currentMenuSelection.Clear();
            currentView.Invalidate(PaintBuffer.DrawingAspect.Select, currentView.DisplayRectangle);
        }

        bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            CommandState.Enabled = true;
            CommandState.Checked = false;
            return true;
        }

        private void ShowContextMenu(System.Drawing.Point pos)
        {
        }
    }
}
