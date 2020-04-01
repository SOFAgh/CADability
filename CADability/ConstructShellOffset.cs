using CADability.GeoObject;
using System;

namespace CADability.Actions
{
    internal class ConstructShellOffset : ConstructAction
    {
        private LengthInput distance;
        private static DefaultLength defaultDistance = new DefaultLength(ConstructAction.DefaultLength.StartValue.ViewWidth40);
        private Shell toShell;
        private double theDistance;
        public ConstructShellOffset(Shell toThisShell)
        {
            toShell = toThisShell; // null oder gegeben
        }
        public override string GetID()
        {
            return "ConstructShellOffset";
        }
        public override void OnSetAction()
        {
            base.TitleId = "ConstructAequidist";
            if (toShell == null)
            {
                //baseCurve = new CurveInput("ConstructAequidist.BaseCurve");
                //baseCurve.MouseOverCurvesEvent += new CADability.Actions.ConstructAction.CurveInput.MouseOverCurvesDelegate(OnMouseOverCurves);
                //baseCurve.CurveSelectionChangedEvent += new CADability.Actions.ConstructAction.CurveInput.CurveSelectionChangedDelegate(OnCurveSelectionChanged);
                //baseCurve.HitCursor = CursorTable.GetCursor("RoundIn.cur");
                //baseCurve.ModifiableOnly = false;
                //baseCurve.PreferPath = true;
            }

            distance = new LengthInput("ConstructAequidist.Distance");
            distance.GetLengthEvent += new LengthInput.GetLengthDelegate(OnGetDistance);
            distance.SetLengthEvent += new LengthInput.SetLengthDelegate(OnSetDistance);
            distance.DefaultLength = defaultDistance;
            distance.CalculateLengthEvent += new LengthInput.CalculateLengthDelegate(OnCalculateDist);
            theDistance = defaultDistance;


            SetInput(distance);
            base.OnSetAction();
        }
        private double OnGetDistance()
        {
            return theDistance;
        }
        private bool OnSetDistance(double d)
        {
            theDistance = d;
            return Recalc();
        }
        private bool Recalc()
        {
            if (toShell == null) return false;
            Shell[] shell = toShell.GetOffset(theDistance, true);
            if (shell != null && shell.Length == 1) base.ActiveObject = shell[0];
            return true;
        }
        private double OnCalculateDist(GeoPoint MousePosition)
        {
            double mindist = double.MaxValue;
            if (toShell != null)
            {
                for (int i = 0; i < toShell.Faces.Length; i++)
                {
                    mindist = Math.Min(mindist, toShell.Faces[i].Distance(MousePosition));
                }
            }
            return mindist;
        }

    }
}
