//using System;
using CADability.Actions;
using CADability.UserInterface;

//using SystemAction = System.Action;
using MouseEventArgs = CADability.Substitutes.MouseEventArgs;
using DragEventArgs = CADability.Substitutes.DragEventArgs;
using MouseButtons = CADability.Substitutes.MouseButtons;
using DragDropEffects = CADability.Substitutes.DragDropEffects;
using Keys = CADability.Substitutes.Keys;

namespace CADability
{
    class ScrollAction : Action, IIntermediateConstruction
    {
        private System.Drawing.Point lastPanPosition;
        ModelView modelView; // dort wo das MouseDown stattgefunden hat
        public override string GetID()
        {
            return "Scroll[LeaveSelectProperties]";
        }
        public override void OnMouseDown(MouseEventArgs e, IView vw)
        {
            lastPanPosition = new System.Drawing.Point(e.X, e.Y);
            modelView = vw as ModelView; // nur da soll gedreht werden
        }
        public override void OnMouseMove(MouseEventArgs e, IView vw)
        {
            if (e.Button != MouseButtons.Left) return;
            if (modelView == null) return;
            vw.Canvas.Cursor = "SizeAll";
            int HScrollOffset = e.X - lastPanPosition.X;
            int VScrollOffset = e.Y - lastPanPosition.Y;
            modelView.Scroll(HScrollOffset, VScrollOffset);
            lastPanPosition = new System.Drawing.Point(e.X, e.Y);
        }
        public override void OnMouseUp(MouseEventArgs e, IView vw)
        {
            base.RemoveThisAction();
        }
        public override void OnInactivate(Action NewActiveAction, bool RemovingAction)
        {
            if (!RemovingAction) base.RemoveThisAction();
            base.OnInactivate(NewActiveAction, RemovingAction);
        }
        public override bool OnEscape()
        {
            base.RemoveThisAction();
            base.OnEscape();
            return true;
        }
#region IIntermediateConstruction Members
        IPropertyEntry IIntermediateConstruction.GetHandledProperty()
        {
            return null;
        }
        bool IIntermediateConstruction.Succeeded
        {
            get { return true; }
        }
#endregion
    }
}
