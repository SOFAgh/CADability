using CADability.Actions;
using CADability.UserInterface;
using System;
using System.Collections;
using System.Threading;
using Action = CADability.Actions.Action;
using MouseEventArgs = CADability.Substitutes.MouseEventArgs;
using DragEventArgs = CADability.Substitutes.DragEventArgs;
using MouseButtons = CADability.Substitutes.MouseButtons;
using DragDropEffects = CADability.Substitutes.DragDropEffects;
using Keys = CADability.Substitutes.Keys;
#if WEBASSEMBLY
using CADability.WebDrawing;
using Point = CADability.WebDrawing.Point;
#else
using System.Drawing;
using Point = System.Drawing.Point;
#endif

namespace CADability
{
    /// <summary>
    /// Parameter declaration for <see cref="ModelView.DisplayChangedEvent"/>.
    /// </summary>

    public class DisplayChangeArg
    {
        /// <summary>
        /// Reason
        /// </summary>
        public enum Reasons
        {
            /// <summary>
            /// unknown
            /// </summary>
            Unknown,
            /// <summary>
            /// The user is zooming in by using the mouse wheel or the toolbar/menu item.
            /// </summary>
            ZoomIn,
            /// <summary>
            /// The user is zooming out by using the mouse wheel or the toolbar/menu item.
            /// </summary>
            ZoomOut,
            /// <summary>
            /// The user is scrolling up
            /// </summary>
            ScrollUp,
            /// <summary>
            /// The user is scrolling down
            /// </summary>
            ScrollDown,
            /// <summary>
            /// The user is scrolling left
            /// </summary>
            ScrollLeft,
            /// <summary>
            /// The user is scrolling right
            /// </summary>
            ScrollRight
        }
        /// <summary>
        /// Reason why the display is changing
        /// </summary>
        public Reasons Reason;
        /// <summary>
        /// While dragging the scrollbarbutton Dragging is on
        /// </summary>
        public enum DraggingModes
        {
            /// <summary>
            /// The user is not dragging a scrollbar button
            /// </summary>
            NotDragging,
            /// <summary>
            /// The user started to drag a scrollbar button
            /// </summary>
            StartDragging,
            /// <summary>
            /// The user is dragging a scrollbar button
            /// </summary>
            Dragging,
            /// <summary>
            /// The user stopped to drag a scrollbar button
            /// </summary>
            StopDragging
        }
        /// <summary>
        /// See <see cref="DraggingModes"/>
        /// </summary>
        public DraggingModes DraggingMode;
        internal DisplayChangeArg(Reasons why)
        {
            Reason = why;
            DraggingMode = DraggingModes.NotDragging;
        }
    }

    /// <summary>
    /// A stack of actions. The top entry is the active action, the bottom entry usually is the <see cref="SelectObjectsAction"/>.
    /// </summary>
    public class ActionStack
    {
        private System.Collections.Stack Actions;
        private IFrame frame;
        private int liceneseCounter;
        public ActionStack(IFrame Frame)
        {
            Actions = new System.Collections.Stack(10); // halt mal mit 10 initialisieren, ist absolut unkritisch
            this.frame = Frame;
            liceneseCounter = 0;
        }
        public void SetAction(Action Action)
        {
            //			if (!Action.WorksOnLayoutView)
            //			{	// das ist jetzt besser gelöst mit "OnlyThisView" etc.
            //				frame.AssureModelView(); // geht nur mit ModelView!
            //			}
            if (Action.ChangeTabInControlCenter)
            {
                Action.returnToTabPage = frame.GetActivePropertyDisplay();
                frame.ShowPropertyDisplay("Action");
            }
            Action.ActionStack = this;
            Action.OnSetAction(); // erstes Ereigniss für die neue Aktion (noch nicht auf dem Stack)
            Frame .RaiseActionStartedEvent(Action);
            Action OldAction = null;
            if (Actions.Count > 0) OldAction = Actions.Peek() as Action;
            // folgende Schleife ist notwendig, da bei Inactivate eine Aktion sich
            // selbst entfernen kann. Dann aber bekommt die drunterliegende ein Activate
            // was gleich wieder von einem Inactivate gefolgt werden muss
            while (OldAction != null)
            {
                OldAction.OnInactivate(Action, false); // alte Aktion inaktivieren (kann sich auch verabschieden)
                if (Actions.Count > 0)
                {
                    if (OldAction != Actions.Peek())
                    {
                        OldAction = Actions.Peek() as Action;
                    }
                    else
                    {
                        OldAction = null;
                    }
                }
                else
                {
                    OldAction = null;
                }
            }
            Actions.Push(Action);
            Action.OnActivate(OldAction, true);
        }
        public IFrame Frame { get { return frame; } }
        public void RemoveActiveAction()
        {
            Action Action = ActiveAction;
            if (Action != null)
            {
                Action.OnInactivate(null, true);
                Actions.Pop();
                if (Action.ChangeTabInControlCenter)
                {
                    frame.ShowPropertyDisplay(Action.returnToTabPage);
                }
                Action NewActiveAction = ActiveAction;
                if (NewActiveAction != null) NewActiveAction.OnActivate(Action, false);
                Action.OnRemoveAction();
                Frame .RaiseActionTerminatedEvent(Action);
            }
            else
            {
                throw new ActionException("RemoveActiveAction: There is no Action object on the action stack");
            }
            // der Autonmatismus, der eine Aktion automatisch wiederholen lässt.
            if (Action.AutoRepeat())
            {	// die Aktion soll automatisch wiederholt werden.
                System.Reflection.ConstructorInfo ci = Action.GetType().GetConstructor(new Type[] { });
                if (ci != null)
                {
                    object repaction = ci.Invoke(new object[] { });
                    if (repaction != null && repaction is Action)
                    {
                        (repaction as Action).AutoRepeated();
                        this.SetAction(repaction as Action);
                    }
                }
                else
                {	// 2. Versuch: gibt es einen Kontruktor, der genau den gleichen Aktionstyp als einzigen Parameter nimmt
                    ci = Action.GetType().GetConstructor(new Type[] { Action.GetType() });
                    if (ci != null)
                    {
                        try
                        {
                            object repaction = ci.Invoke(new object[] { Action });
                            if (repaction != null && repaction is Action)
                            {
                                (repaction as Action).AutoRepeated();
                                this.SetAction(repaction as Action);
                            }
                        }
                        catch (Exception e)
                        {
                            if (e is ThreadAbortException) throw (e);
                        }
                    }
                }
            }
        }
        public Action ActiveAction
        {
            get
            {
                if (Actions.Count > 0) return Actions.Peek() as Action;
                else return null;
            }
        }
        public Action FindAction(Type typeOfAction)
        {
            // der Stack in ein Array liefert top of stack als erstes Element
            object[] allAction = Actions.ToArray();
            for (int i = 0; i < allAction.Length; ++i)
            {
                if (allAction[i].GetType() == typeOfAction) return allAction[i] as Action;
            }
            return null;
        }
        public void OnMouseDown(MouseEventArgs e, IView View)
        {
            try
            {
                Action a = (Actions.Count > 0) ? (Action)Actions.Peek() : null;
                if (a != null && a.AcceptMouseInput(View))
                {
                    a.SetCurrentMouseView(View);
                    a.OnMouseDown(e, View);
                }
                ++liceneseCounter; // overflow macht nix, ausprobiert!
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
        }
        public void OnMouseEnter(System.EventArgs e, IView View)
        {
            try
            {
                Action a = (Actions.Count > 0) ? (Action)Actions.Peek() : null;
                if (a != null && a.AcceptMouseInput(View))
                {
                    a.OnMouseEnter(e, View);
                }
                ++liceneseCounter; // overflow macht nix, ausprobiert!
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
        }
        public void OnMouseHover(System.EventArgs e, IView View)
        {
            try
            {
                Action a = (Actions.Count > 0) ? (Action)Actions.Peek() : null;
                if (a != null && a.AcceptMouseInput(View))
                {
                    a.OnMouseHover(e, View);
                }
                ++liceneseCounter; // overflow macht nix, ausprobiert!
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
        }
        public void OnMouseLeave(System.EventArgs e, IView View)
        {
            try
            {
                Action a = (Actions.Count > 0) ? (Action)Actions.Peek() : null;
                if (a != null && a.AcceptMouseInput(View))
                {
                    a.OnMouseLeave(e, View);
                }
                ++liceneseCounter; // overflow macht nix, ausprobiert!
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
        }
        public void OnMouseMove(MouseEventArgs e, IView View)
        {
            try
            {
                Action a = (Actions.Count > 0) ? (Action)Actions.Peek() : null;
                if (a != null && a.AcceptMouseInput(View))
                {
                    a.SetCurrentMouseView(View);
                    a.OnMouseMove(e, View);
                }
                else
                {
                    View.SetCursor("No");
                }
                ++liceneseCounter; // overflow macht nix, ausprobiert!
                if (liceneseCounter % 5000 == 10)
                {
                }
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
        }
        public void OnMouseUp(MouseEventArgs e, IView View)
        {
            try
            {
                Action a = (Actions.Count > 0) ? (Action)Actions.Peek() : null;
                if (a != null && a.AcceptMouseInput(View))
                {
                    a.SetCurrentMouseView(View);
                    a.OnMouseUp(e, View);
                }
                {   // folgende Zeile nur zum Aufwecken der open cascade dll
                    // dmit die Prüfung der Lizenz stattfindet
                }
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
        }
        public void OnMouseWheel(MouseEventArgs e, IView View)
        {
            try
            {
                Action a = (Actions.Count > 0) ? (Action)Actions.Peek() : null;
                if (a != null && a.AcceptMouseInput(View))
                {
                    a.OnMouseWheel(e, View);
                }
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
        }
        public int GetInfoProviderIndex(Point ScreenCursorPosition, IView View)
        {
            if (Actions.Count > 0) return ((Action)Actions.Peek()).GetInfoProviderIndex(ScreenCursorPosition, View);
            return -1;
        }
        public string GetInfoProviderText(int Index, CADability.UserInterface.InfoLevelMode Level, IView View)
        {
            if (Actions.Count > 0) return ((Action)Actions.Peek()).GetInfoProviderText(Index, Level, View);
            return null;
        }
        public int GetInfoProviderVerticalPosition(int Index, IView View)
        {
            if (Actions.Count > 0) return ((Action)Actions.Peek()).GetInfoProviderVerticalPosition(Index, View);
            return 0;
        }
        public bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {   // OnUpdateCommand klappert rückwärts durch den Action Stack
            // und ruft die einzelnen OnUpdateCommand auf, bis einer reagiert
            // das ist notwendig, da SelectObjectsAction einiges ein und ausschaltet
            // und sonst während des Zeichnens das Verschieben aktiv ist.
            try
            {
                object[] ar = Actions.ToArray();
                // top of stack wird 0
                for (int i = 0; i < ar.Length; ++i)
                {
                    Action a = ar[i] as Action;
                    if (a != null && a.OnUpdateCommand(MenuId, CommandState)) return true;
                }
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
            return false;
        }
        public bool OnCommand(string MenuId)
        {   // siehe OnUpdateCommand ...
            try
            {
                object[] ar = Actions.ToArray();
                for (int i = 0; i < ar.Length; ++i)
                {
                    Action a = ar[i] as Action;
                    if (a != null && a.OnCommand(MenuId)) return true;
                }
            }
            catch (Exception ex)
            {   // der ActionStack ist der Verteiler der MouseMessages. Falls dort eine Exception auftritt
                // wird die hier gefangen und stört nicht weiter. Das ist eine Forderung von ERSA. Ggf. 
                // hier eine Möglichkeit die Exceeption doch noch weiterzureichen vorsehen.
                if (ex is ThreadAbortException) throw (ex);
            }
            return false;
        }

        public void OnViewsChanged()
        {
            // foreach kann man hier nicht nehmen, denn bei dem Aufruf kann eine Aktion sich abmelden
            // außerdem möchte ich lieber rückwärts gehen, die aktive Aktion zuerst
            // foreach (Action a in Actions) a.OnViewsChanged();
            ArrayList al = new ArrayList(Actions);
            for (int i = al.Count - 1; i >= 0; --i)
            {
                (al[i] as Action).OnViewsChanged();
            }
        }
        private void OnDisplayChanged(object sender, DisplayChangeArg displayChangeArg)
        {
            // foreach (Action a in Actions) a.OnDisplayChanged(displayChangeArg);
            // siehe unter OnViewsChanged
            ArrayList al = new ArrayList(Actions);
            for (int i = al.Count - 1; i >= 0; --i)
            {
                (al[i] as Action).OnDisplayChanged(displayChangeArg);
            }
        }
        internal Action PrevoiusAction(Action action)
        {
            object[] allAction = Actions.ToArray();
            for (int i = 0; i < allAction.Length - 1; ++i)
            {
                if (allAction[i] == action) return allAction[i + 1] as Action;
            }
            return null;
        }
    }
}
