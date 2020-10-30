using CADability.Actions;
using CADability.Attribute;
using CADability.GeoObject;
using CADability.Substitutes;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
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
        private IGeoObject currentMenuSelection;
        private IPaintTo3DList displayList;
        private IView currentView;
        public SelectActionContextMenu(SelectObjectsAction soa)
        {
            this.soa = soa;
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
            curves = vw.Model.GetObjectsFromRect(pa, new Set<Layer>(pm.GetVisibleLayers()), PickMode.onlyEdges, null); // returns edges and curves
            edges = new List<Edge>();
            for (int i = curves.Count - 1; i >= 0; --i)
            {
                if (curves[i].Owner is Edge edge)
                {
                    edges.Add(edge);
                    curves.Remove(i);
                }
            }
            GeoObjectList fl = vw.Model.GetObjectsFromRect(pa, new Set<Layer>(pm.GetVisibleLayers()), PickMode.onlyFaces, null); // returns only faces
            faces = new List<Face>();
            shells = new List<Shell>(); // only Shells, which have are not part of a Solid
            solids = new List<Solid>();
            for (int i = 0; i < fl.Count; i++)
            {
                if (fl[i] is Face face) faces.Add(face); // this should always be the case
                if (fl[i].Owner is Shell shell)
                {
                    if (shell.Owner is Solid sld) { if (!solids.Contains(sld)) solids.Add(sld); }
                    else { if (!shells.Contains(shell)) shells.Add(shell); }
                }
            }

            // now we have curves, edges, faces, shells and solids (text, hatch, block, dimension not yet implemented)

            List<MenuWithHandler> cm = new List<MenuWithHandler>();
            MenuWithHandler mhdumy = new MenuWithHandler();
            mhdumy.ID = "MenuId.dumy";
            mhdumy.Text = "dumy menu entry";
            for (int i = 0; i < curves.Count; i++)
            {
                double z = curves[i].Position(pa.FrontCenter, pa.Direction, vw.Model.displayListPrecision);
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Curve." + i.ToString();
                mh.Text = curves[i].Description + " " + z.ToString();
                mh.SubMenus = new MenuWithHandler[] { mhdumy };
                mh.Target = this;
                cm.Add(mh);
            }
            for (int i = 0; i < edges.Count; i++)
            {
                double z = (edges[i].Curve3D as IGeoObject).Position(pa.FrontCenter, pa.Direction, vw.Model.displayListPrecision);
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Edge." + i.ToString();
                mh.Text = StringTable.GetString("MenuId.Edge", StringTable.Category.label);
                mh.SubMenus = edges[i].GetContextMenu(soa.Frame);
                mh.Target = this;
                cm.Add(mh);
            }
            for (int i = 0; i < faces.Count; i++)
            {
                double z = faces[i].Position(pa.FrontCenter, pa.Direction, vw.Model.displayListPrecision);
                MenuWithHandler mh = new MenuWithHandler();
                mh.ID = "MenuId.Face." + i.ToString();
                mh.Text = StringTable.GetString("MenuId.Face", StringTable.Category.label);
                mh.SubMenus = faces[i].GetContextMenu(soa.Frame);
                mh.Target = this;
                cm.Add(mh);
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
        private void OnRepaintSelect(System.Drawing.Rectangle IsInvalid, IView View, IPaintTo3D PaintToSelect)
        {
            if (currentMenuSelection != null)
            {
                System.Diagnostics.Trace.WriteLine("currentMenuSelection: " + currentMenuSelection.Description);
                PaintToSelect.SelectMode = true;
                PaintToSelect.SelectColor = System.Drawing.Color.Yellow;
                int wobbleWidth = 3;
                if ((PaintToSelect.Capabilities & PaintCapabilities.ZoomIndependentDisplayList) != 0)
                {
                    PaintToSelect.OpenList();
                    currentMenuSelection.PaintTo3D(PaintToSelect);
                    displayList = PaintToSelect.CloseList();
                    PaintToSelect.SelectedList(displayList, wobbleWidth);
                }
            }
        }

        private void ContextMenuCollapsed(int dumy)
        {
            currentView.RemovePaintHandler(PaintBuffer.DrawingAspect.Select, new PaintView(OnRepaintSelect));
            currentMenuSelection = null;
        }
        bool ICommandHandler.OnCommand(string MenuId)
        {
            return true;
        }

        void ICommandHandler.OnSelected(string MenuId, bool selected)
        {
            System.Diagnostics.Trace.WriteLine("OnSelected: " + MenuId);
            if (MenuId.StartsWith("MenuId.Curve."))
            {
                int ind = int.Parse(MenuId.Substring("MenuId.Curve.".Length));
                currentMenuSelection = curves[ind];
            }
            else if (MenuId.StartsWith("MenuId.Edge."))
            {
                int ind = int.Parse(MenuId.Substring("MenuId.Edge.".Length));
                currentMenuSelection = edges[ind].Curve3D as IGeoObject;
            }
            else if (MenuId.StartsWith("MenuId.Face."))
            {
                int ind = int.Parse(MenuId.Substring("MenuId.Face.".Length));
                currentMenuSelection = faces[ind];
            }
            else if (MenuId.StartsWith("MenuId.Shell."))
            {
                int ind = int.Parse(MenuId.Substring("MenuId.Shell.".Length));
                currentMenuSelection = shells[ind];
            }
            else if (MenuId.StartsWith("MenuId.Solid."))
            {
                int ind = int.Parse(MenuId.Substring("MenuId.Solid.".Length));
                currentMenuSelection = solids[ind];
            }
            else currentMenuSelection = null;
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
