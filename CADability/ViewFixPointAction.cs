using CADability.Actions;
using CADability.GeoObject;
using CADability.UserInterface;

namespace CADability
{
    class ViewFixPointAction : ConstructAction, IIntermediateConstruction
    {
        GeoPoint current;
        public override string GetID()
        {
            return "MenuId.ViewFixPoint";
        }
        public override void OnSetAction()
        {
            base.TitleId = "MenuId.ViewFixPoint";
            ModelView mv = base.Frame.ActiveView as ModelView;
            if (mv != null) current = mv.FixPoint;
            ConstructAction.GeoPointInput gpi = new GeoPointInput("ViewFixPoint.Point");
            gpi.GetGeoPointEvent += new GeoPointInput.GetGeoPointDelegate(OnGetFixPoint);
            gpi.SetGeoPointEvent += new GeoPointInput.SetGeoPointDelegate(OnSetFixPoint);
            base.SetInput(gpi);
            base.OnSetAction();
            base.Frame.SnapMode |= SnapPointFinder.SnapModes.SnapToFaceSurface;
        }
        public override void OnDone()
        {
            ModelView mv = base.Frame.ActiveView as ModelView;
            if (mv != null) mv.FixPoint = current;
            base.OnDone();
        }
        public override void OnRemoveAction()
        {
            base.OnRemoveAction();
            base.Frame.SnapMode &= ~SnapPointFinder.SnapModes.SnapToFaceSurface;
        }
        public override bool AutoRepeat()
        {
            return false;
        }
        void OnSetFixPoint(GeoPoint p)
        {
            current = p;
        }
        GeoPoint OnGetFixPoint()
        {
            return current;
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
