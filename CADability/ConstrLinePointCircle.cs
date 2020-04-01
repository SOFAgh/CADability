using CADability.Curve2D;
using CADability.GeoObject;
using System;
using System.Collections;

namespace CADability.Actions
{
    internal class ConstrLinePointTangent : ConstructAction
    {
        private Line line;
        private GeoPoint startPoint;
        private GeoPoint2D endPoint;
        private GeoPoint objectPoint;
        private ICurve[] tangCurves;
        private CurveInput curveInput;
        private GeoPointInput inputStartPoint;
        private int selected;
        private int selSol = 0; // wg. Unsymmetrie bei 0 wg Abs

        public ConstrLinePointTangent()
        {
        }
        private bool showLine()
        {
            ArrayList usableCurves = new ArrayList();
            //	ArrayList usablePoints = new ArrayList();
            double mindist = double.MaxValue;
            for (int i = 0; i < tangCurves.Length; ++i)
            {
                Plane pl;
                int solCount = 0;
                if (Curves.GetCommonPlane(startPoint, tangCurves[i], out pl))
                {
                    ICurve2D l2D = tangCurves[i].GetProjectedCurve(pl);
                    if (l2D is Path2D) (l2D as Path2D).Flatten();
                    GeoPoint2D[] tangentPoints = l2D.TangentPoints(pl.Project(startPoint), pl.Project(objectPoint)); //!!!
                    if (tangentPoints.Length > 0)
                    {	// eine gültige Kurve ist gefunden
                        solCount = tangentPoints.Length;
                        usableCurves.Add(tangCurves[i]); // zur lokalen Liste zufügen
                        for (int j = 0; j < tangentPoints.Length; ++j)
                        {
                            double dist = Geometry.Dist(tangentPoints[j], pl.Project(objectPoint));
                            if (dist < mindist)
                            {
                                mindist = dist;
                                selected = usableCurves.Count - 1; // merken, welche Kurve die aktuell benutzte ist
                                                                   // die Punkte werden als sortiert erwartet!! Hier nur über modulo (%) den Index bestimmen
                                endPoint = tangentPoints[Math.Abs(selSol) % tangentPoints.Length];
                            }

                        }
                    }
                }
                if (mindist < double.MaxValue)
                {
                    base.MultiSolutionCount = solCount;
                    line.SetTwoPoints(startPoint, pl.ToGlobal(endPoint));
                    tangCurves = (ICurve[])usableCurves.ToArray(typeof(ICurve)); // überschreibt die eigentliche Liste und wird unten an den Sender zurückgeliefert
                    base.ShowActiveObject = true;
                    return (true);
                }
            }
            base.MultiSolutionCount = 0;
            line.SetTwoPoints(startPoint, objectPoint);
            base.ShowActiveObject = true;
            return (false);
        }

        private bool inputTangCurves(CurveInput sender, ICurve[] Curves, bool up)
        {
            objectPoint = base.CurrentMousePosition;
            selected = 0;
            tangCurves = Curves; // lokale Liste, damit showline immer die Daten hat
            bool s = false;
            if (tangCurves.Length > 0)
            {
                s = true;
                if (inputStartPoint.Fixed)
                {
                    s = (showLine());
                }
            }
            else showLine();
            if (up)
            {
                if (tangCurves.Length == 0) sender.SetCurves(tangCurves, null); // ...die werden jetzt im ControlCenter dargestellt (nur bei up)
                else sender.SetCurves(tangCurves, tangCurves[selected]);
            }
            if (s) return true;
            return false;
        }

        private void inputTangCurvesChanged(CurveInput sender, ICurve SelectedCurve)
        {
            tangCurves = new ICurve[] { SelectedCurve };
            if (inputStartPoint.Fixed)
            { showLine(); }

        }

        private void onSetStartPoint(GeoPoint p)
        {
            startPoint = p;
            if (curveInput.Fixed) showLine();
        }

        public override void OnSolution(int sol)
        {
            if (sol == -1) selSol += 1;
            else
                if (sol == -2) selSol -= 1;
            else selSol = sol;
            showLine();
        }


        public override void OnSetAction()
        {
            line = Line.Construct();
            startPoint = ConstrDefaults.DefaultStartPoint;
            base.ActiveObject = line;

            base.TitleId = "Constr.Line.PointTangent";

            inputStartPoint = new GeoPointInput("Line.StartPoint");
            inputStartPoint.SetGeoPointEvent += new CADability.Actions.ConstructAction.GeoPointInput.SetGeoPointDelegate(onSetStartPoint);
            inputStartPoint.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;

            curveInput = new CurveInput("Constr.Line.Tangent.Object");
            curveInput.Decomposed = true; // nur Einzelelemente, auch bei Polyline und Pfad
            curveInput.MouseOverCurvesEvent += new CADability.Actions.ConstructAction.CurveInput.MouseOverCurvesDelegate(inputTangCurves);
            curveInput.CurveSelectionChangedEvent += new CADability.Actions.ConstructAction.CurveInput.CurveSelectionChangedDelegate(inputTangCurvesChanged);
            base.SetInput(inputStartPoint, curveInput);
            base.ShowAttributes = true;
            base.OnSetAction();
        }

        public override void OnRemoveAction()
        {
            base.OnRemoveAction();
        }

        public override string GetID()
        {
            return "Constr.Line.PointTangent";
        }

        public override void OnDone()
        {
            base.OnDone();
        }
    }
}
