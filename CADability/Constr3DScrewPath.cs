using CADability.Curve2D;
using CADability.GeoObject;
using CADability.Shapes;
using System;

namespace CADability.Actions
{
    /// <summary>
    /// Action to screw a path along an axis.
    /// Path and axis must reside in a common plane.
    /// An open path will be screwed with a pitch connectiong the start- and endpoint (a line is added when needed)
    /// and a solid object is created with this path and a cylindrical kernel.
    /// A closed path can be screwed with aa arbitrary pitch.
    /// </summary>
    internal class Constr3DScrewPath : ConstructAction
    {
        private CurveInput pathInput;
        private CurveInput rotateLineInput;
        private GeoVectorInput axisVectorInput;
        private GeoPointInput axisPointInput;
        private MultipleChoiceInput orientation;
        private bool handed; // true: righthanded, false: lefthanded
        GeoVector axisVector;
        double angleRotation;
        private Axis axis; // the axis to rotate about
        private Plane plane; // the plane, in which the data is defined
        private CompoundShape shape; // a closed shape, maybe with holes to rotate
        private Path2D path; // alternatively to shape, an open path. either shape==null or path==null

        private Constr3DScrewPath()
        {
        }

        public Constr3DScrewPath(GeoObjectList selectedObjects)
        {
            GeoObjectList clones = selectedObjects.CloneObjects();
            if (selectedObjects.Count == 1)
            {
                if (selectedObjects[0] is Hatch)
                {
                    shape = (selectedObjects[0] as Hatch).CompoundShape;
                    plane = (selectedObjects[0] as Hatch).Plane;
                    return;
                }
                else if (selectedObjects[0] is Face)
                {
                    if ((selectedObjects[0] as Face).Surface is PlaneSurface)
                    {
                        // here we could directely set shape and plane
                    }
                    clones.Clear();
                    foreach (Edge edg in (selectedObjects[0] as Face).AllEdgesIterated())
                    {
                        if (edg.Curve3D != null) clones.Add(edg.Curve3D as IGeoObject);
                    }
                }
            }
            shape = CompoundShape.CreateFromList(clones, Precision.eps, out plane);
            if (shape == null)
            {
                Path toRotate = Path.Construct();
                toRotate.Set(clones, false, Precision.eps);
                if (toRotate.CurveCount > 0)
                {
                    if (toRotate.GetPlanarState() == PlanarState.Planar)
                    {
                        plane = toRotate.GetPlane();
                        path = toRotate.GetProjectedCurve(plane) as Path2D;
                    }
                }
            }
        }

        static public bool canUseList(GeoObjectList selectedObjects)
        {
            Path toRotate = Path.Construct();
            GeoObjectList clones = selectedObjects.CloneObjects();
            if (selectedObjects.Count == 1)
            {
                if (selectedObjects[0] is Hatch)
                {
                    return true;
                }
                if (selectedObjects[0] is Face)
                {
                    if ((selectedObjects[0] as Face).Surface is PlaneSurface) return true;
                    clones.Clear();
                    foreach (Edge edg in (selectedObjects[0] as Face).AllEdgesIterated())
                    {
                        if (edg.Curve3D != null) clones.Add(edg.Curve3D as IGeoObject);
                    }
                }
            }
            toRotate.Set(clones, false, Precision.eps);
            if (toRotate.CurveCount > 0)
            {
                if (toRotate.GetPlanarState() == PlanarState.UnderDetermined || toRotate.GetPlanarState() == PlanarState.Planar) return true;
            }
            return false;
        }

        public override string GetID()
        {
            return "Constr.Face.ScrewPath";
        }

        private void Recalculate()
        {
            if (path != null)
            {
                if (path.IsClosed)
                {
                    Solid sld = Make3D.MakeHelicalSolid(plane.Project(axis.Location), plane.Project(axis.Direction), plane, path, 2.5, handed);
                    if (sld != null) ActiveObject = sld;
                }
                else
                {
                    Solid sld = Make3D.MakeHelicalSolid(plane.Project(axis.Location), plane, path, 2.5, handed);
                    if (sld != null) ActiveObject = sld;
                }
            }
            else if (shape != null)
            {
                Solid sld = Make3D.MakeHelicalSolid(plane.Project(axis.Location), plane.Project(axis.Direction), plane, shape.SimpleShapes[0].Outline.AsPath(), 2.5, handed);
                if (sld != null) ActiveObject = sld;
            }
        }
        public override void OnSetAction()
        {
            base.ActiveObject = Face.Construct();
            handed = true;

            if (axisVector.IsNullVector()) axisVector = GeoVector.XAxis;
            if (angleRotation == 0.0) angleRotation = Math.PI;

            base.TitleId = "Constr.Face.ScrewPath";

            pathInput = new CurveInput("Constr.Face.ScrewPath.Path");
            //curveInput.MouseOverCurvesEvent += new CurveInput.MouseOverCurvesDelegate(curveInputPath);
            //curveInput.CurveSelectionChangedEvent += new CurveInput.CurveSelectionChangedDelegate(curveInputPathChanged);
            if (path != null || shape != null) pathInput.Fixed = true;

            rotateLineInput = new CurveInput("Constr.Face.ScrewPath.AxisLine");
            rotateLineInput.Decomposed = true; // nur Einzelelemente, auch bei Polyline und Pfad
            rotateLineInput.MouseOverCurvesEvent += new CurveInput.MouseOverCurvesDelegate(mouseOverAxis);
            //rotateLineInput.CurveSelectionChangedEvent += new CurveInput.CurveSelectionChangedDelegate(RotateLineChanged);

            orientation = new MultipleChoiceInput("Constr.Face.ScrewPath.Oreintation", "Constr.Face.ScrewPath.Oreintation.Values", 0);
            orientation.SetChoiceEvent += Orientation_SetChoiceEvent;
            orientation.GetChoiceEvent += Orientation_GetChoiceEvent;
            base.SetInput(pathInput, rotateLineInput, orientation); // , axisPointInput, axisVectorInput);
            base.ShowAttributes = true;
            base.OnSetAction();

        }

        private int Orientation_GetChoiceEvent()
        {
            if (handed) return 0;
            else return 1;
        }

        private void Orientation_SetChoiceEvent(int val)
        {
            handed = val == 0;
            Recalculate();
        }

        private bool mouseOverAxis(CurveInput sender, ICurve[] theCurves, bool up)
        {
            bool found = false;
            for (int i = 0; i < theCurves.Length; i++)
            {
                if (theCurves[i] is Line)
                {
                    if (plane.Distance(theCurves[i].StartPoint) < Precision.eps && plane.Distance(theCurves[i].EndPoint) < Precision.eps)
                    {
                        found = true;
                        axis = new Axis(theCurves[i].StartPoint, theCurves[i].StartDirection);
                        break;
                    }
                }
            }
            if (found) Recalculate();
            return found;
        }
    }
}
