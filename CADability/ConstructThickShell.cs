using CADability.GeoObject;
using System;

namespace CADability.Actions
{
    internal class ConstructThickShell : ConstructAction
    {
        private LengthInput distance1, distance2;
        private static DefaultLength defaultDistance = new DefaultLength(ConstructAction.DefaultLength.StartValue.ViewWidth40);
        private Shell toShell;
        private double theDistance1, theDistance2;
        public ConstructThickShell(Shell toThisShell)
        {
            toShell = toThisShell; // null oder gegeben
        }
        public override string GetID()
        {
            return "ConstructThickShell";
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

            distance1 = new LengthInput("ConstructAequidist.Distance1");
            distance1.GetLengthEvent += new LengthInput.GetLengthDelegate(OnGetDistance1);
            distance1.SetLengthEvent += new LengthInput.SetLengthDelegate(OnSetDistance1);
            theDistance1 = 0.0;
            distance2 = new LengthInput("ConstructAequidist.Distance2");
            distance2.GetLengthEvent += new LengthInput.GetLengthDelegate(OnGetDistance2);
            distance2.SetLengthEvent += new LengthInput.SetLengthDelegate(OnSetDistance2);
            distance2.DefaultLength = defaultDistance;
            theDistance2 = defaultDistance;


            SetInput(distance1, distance2);
            base.OnSetAction();
        }
        private double OnGetDistance1()
        {
            return theDistance1;
        }
        private bool OnSetDistance1(double d)
        {
            theDistance1 = d;
            return Recalc();
        }
        private double OnGetDistance2()
        {
            return theDistance2;
        }
        private bool OnSetDistance2(double d)
        {
            theDistance2 = d;
            return Recalc();
        }
        private bool Recalc()
        {
            if (toShell == null) return false;
            Solid sld = toShell.MakeThick(Math.Min(theDistance1, theDistance2), Math.Max(theDistance1, theDistance2));
            if (sld != null) base.ActiveObject = sld;
            return true;
        }
        //private double OnCalculateDist(GeoPoint MousePosition)
        //{
        //    double mindist = double.MaxValue;
        //    if (toShell != null)
        //    {
        //        for (int i = 0; i < toShell.Faces.Length; i++)
        //        {
        //            mindist = Math.Min(mindist, toShell.Faces[i].Distance(MousePosition));
        //        }
        //    }
        //    return mindist;
        //}

    }
}
