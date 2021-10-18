using CADability.GeoObject;
using CADability.UserInterface;
using MouseEventArgs = CADability.Substitutes.MouseEventArgs;

namespace CADability.Actions
{
    /// <summary>
    /// 
    /// </summary>

    public class GeneralLengthAction : Action
    {
        private LengthProperty lengthProperty;
        private double InitialLengthValue;
        private GeoPoint fixPoint;
        private GeoPoint linePoint;
        private GeoVector lineDirection;
        private enum Mode { fromPoint, fromLine }
        private Mode mode;
        public GeneralLengthAction(LengthProperty lengthProperty, GeoPoint fixPoint)
        {
            this.lengthProperty = lengthProperty;
            this.fixPoint = fixPoint;
            mode = Mode.fromPoint;
        }
        public GeneralLengthAction(LengthProperty lengthProperty, GeoPoint fixPoint, IGeoObject ignoreSnap) : this(lengthProperty, fixPoint)
        {
            if (ignoreSnap != null)
            {
                base.IgnoreForSnap = new GeoObjectList(ignoreSnap);
            }
        }
        public GeneralLengthAction(LengthProperty lengthProperty, GeoPoint linePoint, GeoVector lineDirection, IGeoObject ignoreSnap) : this(lengthProperty, linePoint, lineDirection)
        {
            if (ignoreSnap != null)
            {
                base.IgnoreForSnap = new GeoObjectList(ignoreSnap);
            }
        }

        public GeneralLengthAction(LengthProperty lengthProperty, GeoPoint linePoint, GeoVector lineDirection)
        {
            this.lengthProperty = lengthProperty;
            this.linePoint = linePoint;
            this.lineDirection = lineDirection;
            mode = Mode.fromLine;
        }

        private void SetLength(double l)
        {
            lengthProperty.SetLength(l);
            lengthProperty.LengthChanged();
        }
        private double GetLength()
        {
            return lengthProperty.GetLength();
        }

        /// <summary>
        /// Implements <see cref="Action.OnSetAction"/>. If you override this method
        /// don't forget to call the bas implementation.
        /// </summary>
        public override void OnSetAction()
        {
            InitialLengthValue = GetLength();
        }
        /// <summary>
        /// Implements <see cref="Action.OnMouseMove"/>. If you override this method
        /// don't forget to call the bas implementation.
        /// </summary>
        /// <param name="e"><paramref name="Action.OnMouseMove.e"/></param>
        /// <param name="vw"><paramref name="Action.OnMouseMove.vw"/></param>
        public override void OnMouseMove(MouseEventArgs e, IView vw)
        {
            using (Frame.Project.Undo.ContextFrame(this))
            {
                SnapPointFinder.DidSnapModes DidSnap;
                switch (mode)
                {
                    case Mode.fromPoint:
                        SetLength(Geometry.Dist(base.SnapPoint(e, fixPoint, vw, out DidSnap), fixPoint));
                        break;
                    case Mode.fromLine:
                        SetLength(Geometry.DistPL(base.SnapPoint(e, fixPoint, vw, out DidSnap), linePoint, lineDirection));
                        break;
                }
            }
        }
        /// <summary>
        /// Implements <see cref="Action.GetID"/>.
        /// </summary>
        public override string GetID()
        {
            return "GeneralLengthAction[LeaveSelectProperties]";
        }

        /// <summary>
        /// Overrides <see cref="CADability.Actions.Action.OnRemoveAction ()"/>
        /// </summary>
		public override void OnRemoveAction()
        {
        }

        /// <summary>
        /// Implements <see cref="Action.OnMouseUp"/>. If you override this method
        /// don't forget to call the bas implementation.
        /// </summary>
        /// <param name="e"><paramref name="Action.OnMouseUp.e"/></param>
        /// <param name="vw"><paramref name="Action.OnMouseUp.vw"/></param>
        public override void OnMouseUp(MouseEventArgs e, IView vw)
        {
            OnMouseMove(e, vw);
            Frame.Project.Undo.ClearContext(); // also die nächsten Änderungen sind ein neuer undo Schritt
            base.RemoveThisAction();
        }
        /// <summary>
        /// Implements <see cref="Action.OnActivate"/>. If you override this method
        /// don't forget to call the bas implementation.
        /// </summary>
        public override void OnActivate(Action OldActiveAction, bool SettingAction)
        {
            lengthProperty.CheckMouseButton(true);
            lengthProperty.FireModifyWithMouse(true);
        }
        /// <summary>
        /// Implements <see cref="Action.OnInactivate"/>. If you override this method
        /// don't forget to call the bas implementation.
        /// </summary>
        /// <param name="NewActiveAction"><paramref name="Action.OnInactivate.NewActiveAction"/></param>
        /// <param name="RemovingAction"><paramref name="Action.OnInactivate.RemovingAction"/></param>
        public override void OnInactivate(Action NewActiveAction, bool RemovingAction)
        {
            lengthProperty.FireModifyWithMouse(false);
            lengthProperty.CheckMouseButton(false);
            if (!RemovingAction)
            {
                SetLength(InitialLengthValue);
                base.RemoveThisAction();
            }
        }

        /// <summary>
        /// Overrides <see cref="CADability.Actions.Action.OnEscape ()"/>
        /// </summary>
		public override bool OnEscape()
        {
            SetLength(InitialLengthValue);
            base.RemoveThisAction();
            return true;
        }

        /// <summary>
        /// Overrides <see cref="CADability.Actions.Action.OnEnter ()"/>
        /// </summary>
		public override bool OnEnter()
        {
            base.RemoveThisAction();
            return true;
        }
    }
}
