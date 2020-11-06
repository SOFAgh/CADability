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
                    if (z- delta > mindist) curves.Remove(i);
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
                    fch.ID = "MenuId.Fillet." + res.Count.ToString();
                    fch.Target = this;
                    fch.Text = StringTable.GetString("MenuId.Fillet", StringTable.Category.label);
                    MenuWithHandler fr = new MenuWithHandler();
                    fr.ID = "MenuId.Fillet.ChangeRadius";
                    fr.Target = fch;
                    fr.Text = StringTable.GetString("MenuId.Fillet.ChangeRadius", StringTable.Category.label);
                    MenuWithHandler fd = new MenuWithHandler();
                    fd.ID = "MenuId.Fillet.Remove";
                    fd.Target = fch;
                    fd.Text = StringTable.GetString("MenuId.Fillet.Remove", StringTable.Category.label);
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
                    FeatureCommandHandler fch = new FeatureCommandHandler(lconnected.ToArray(), this);
                    fch.ID = "MenuId.Fillet." + res.Count.ToString();
                    fch.Target = fch;
                    fch.Text = StringTable.GetString("MenuId.Fillet", StringTable.Category.label);
                    res.Add(fch);
                }
            }
            // TODO: a menu entry for the face alone is missing
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
                        if (edge.IsTangentialEdge())
                        {
                            if (edge.Curve2D(face).DirectionAt(0.5).IsMoreHorizontal == extrusion.ExtrusionDirectionIsV && otherFace.Surface is ISurfaceOfArcExtrusion arcExtrusion)
                            {
                                CollectConnectedFillets(otherFace, connectedFillets);
                            }
                        }

                    }
                }
                else if (face.Surface is SphericalSurface)
                {   // at a sphere a fillet might branch out
                    foreach (Edge edge in face.OutlineEdges)
                    {
                        Face otherFace = edge.OtherFace(face);
                        if (edge.IsTangentialEdge())
                        {
                            if (otherFace.Surface is ISurfaceOfArcExtrusion)
                            {
                                CollectConnectedFillets(otherFace, connectedFillets);
                            }
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
                    foreach (IGeoObject go  in currentMenuSelection)
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
