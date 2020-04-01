using CADability.Actions;
using CADability.UserInterface;

using Action = CADability.Actions.Action;
using MouseEventArgs = CADability.Substitutes.MouseEventArgs;
using DragEventArgs = CADability.Substitutes.DragEventArgs;
using MouseButtons = CADability.Substitutes.MouseButtons;
using DragDropEffects = CADability.Substitutes.DragDropEffects;
using Keys = CADability.Substitutes.Keys;

namespace CADability
{
    class ZoomPlusMinusAction : Action, IIntermediateConstruction
    {
        private System.Drawing.Point lastPanPosition;
        ModelView modelView; // dort wo das MouseDown stattgefunden hat
        public override string GetID()
        {
            return "ZoomPlusMinus[LeaveSelectProperties]";
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
            int HScrollOffset = lastPanPosition.X - e.X;
            if (HScrollOffset == 0) return;
            double Factor = 1.0;
            if (HScrollOffset > 0) Factor = 1.0 + HScrollOffset / 100.0;
            else if (HScrollOffset < 0) Factor = 1.0 / (1.0 - HScrollOffset / 100.0);
            (modelView as ICondorViewInternal).ZoomDelta(Factor);
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
