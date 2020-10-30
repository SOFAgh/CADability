using CADability.Actions;
using CADability.UserInterface;
using CADability.GeoObject;
using CADability.Attribute;
using System.Collections.Generic;
using System;

namespace CADability
{
    /// <summary>
    /// Action to modify the distance between two objects.
    /// The objects may be faces or edges or vertices (also a mix of types).
    /// First we calculate the offset vector: vertex->face: perpendicular foot point, vertex->edge: perpendicular foot point, vertex->vertex: connection
    /// edge->edge: minimum distance, edge->face: minimum distance, face->face: parallel or minimum distance. (objects with axis should also offer their axis for distance)
    /// </summary>
    internal class ParametricsDistance : ConstructAction
    {
        private object distanceFromHere, distanceToHere; // the two objects which define the distance (may be vertices, edges or faces)
        private GeoVector originalOffset; // the offset vector between the facesToMove and facesToKeep
        private GeoVector currentOffset; // the additional offset vector between the facesToMove and facesToKeep
        private Shell shell; // the shell containing the edge
        private List<Face> facesToMove; // list of the faces, which have to be moved
        private List<Face> facesToKeep; // list of the faces, which should stay in place (ore been moved in opposite direction when symmetric is choosen)
        private bool validResult;
        private IFrame frame;
        private GeoObjectInput otherObjectInput; // input field specifying the object to which the distance should be set
        private LengthInput distanceInput; // input filed of the distance
        private MultipleChoiceInput modeInput;
        private enum Mode { forward, symmetric, backward };
        private Mode mode;
        private GeoPoint offsetStartPoint;
        private Line offsetFeedBack;
        public ParametricsDistance(object distanceToHere, IFrame frame)
        {
            this.distanceToHere = distanceToHere;
            IGeoObject owner = null;
            if (distanceToHere is Vertex vtx)
            {
                owner = vtx.Edges[0].Curve3D as IGeoObject;
                offsetStartPoint = vtx.Position;
            }
            else if (distanceToHere is Edge edge)
            {
                owner = edge.PrimaryFace;
                offsetStartPoint = edge.Curve3D.PointAt(0.5);
            }
            else if (distanceToHere is Face fc)
            {
                owner = fc;
                GeoPoint2D inner = fc.Area.GetSomeInnerPoint();
                offsetStartPoint = fc.Surface.PointAt(inner);
            }
            else throw new ApplicationException("ParametricsDistance must be called with a vertex, edge or face");
            offsetFeedBack = Line.TwoPoints(offsetStartPoint, offsetStartPoint);
            while (owner != null && !(owner is Shell)) owner = owner.Owner as IGeoObject;
            shell = owner as Shell; // this is the shell to be modified
            this.frame = frame;
            facesToMove = new List<Face>();
            facesToKeep = new List<Face>();
        }

        public override string GetID()
        {
            return "Constr.Parametrics.DistanceTo";
        }
        public override bool OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.Parametrics.DistanceTo":
                    frame.SetAction(this); // this is the way this action comes to life
                    return true;
            }
            return false;
        }

        public override void OnSetAction()
        {
            base.TitleId = "Constr.Parametrics.DistanceTo";
            FeedBack.AddSelected(offsetFeedBack);
            base.ActiveObject = shell.Clone();
            shell.Layer.Transparency = 128;

            otherObjectInput = new GeoObjectInput("DistanceTo.OtherObject");
            otherObjectInput.FacesOnly = true;
            otherObjectInput.EdgesOnly = true;
            //otherObjectInput.MultipleInput = true;
            otherObjectInput.MouseOverGeoObjectsEvent += OtherObject_MouseOverGeoObjectsEvent;
            otherObjectInput.GeoObjectSelectionChangedEvent += OtherObject_GeoObjectSelectionChangedEvent;

            distanceInput = new LengthInput("DistanceTo.Distance");
            distanceInput.GetLengthEvent += DistanceInput_GetLengthEvent;
            distanceInput.SetLengthEvent += DistanceInput_SetLengthEvent;

            modeInput = new MultipleChoiceInput("DistanceTo.Mode", "DistanceTo.Mode.Values", 0);
            modeInput.SetChoiceEvent += ModeInput_SetChoiceEvent;
            //modeInput.GetChoiceEvent += ModeInput_GetChoiceEvent;
            base.SetInput(otherObjectInput, distanceInput, modeInput);
            base.OnSetAction();

            validResult = false;
        }

        private int ModeInput_GetChoiceEvent()
        {
            return (int)mode;
        }

        private void ModeInput_SetChoiceEvent(int val)
        {
            mode = (Mode)val;
            DistanceInput_SetLengthEvent((originalOffset + currentOffset).Length); // this is to refresh only
        }

        public override void OnDone()
        {
            if (validResult && ActiveObject != null)
            {
                Solid sld = shell.Owner as Solid;
                if (sld != null)
                {   // the shell was part of a Solid
                    IGeoObjectOwner owner = sld.Owner; // Model or Block
                    owner.Remove(sld);
                    Solid replacement = Solid.MakeSolid(ActiveObject as Shell);
                    owner.Add(replacement);
                }
                else
                {
                    IGeoObjectOwner owner = shell.Owner;
                    owner.Remove(shell);
                    owner.Add(ActiveObject);
                }
            }
            ActiveObject = null;
            base.OnDone();
        }
        public override void OnRemoveAction()
        {
            shell.Layer.Transparency = 0; // make the layer intransparent again
            base.OnRemoveAction();
        }
        private bool DistanceInput_SetLengthEvent(double length)
        {
            validResult = false;
            if (facesToMove.Count > 0 && facesToKeep.Count > 0)
            {
                currentOffset = originalOffset;
                currentOffset.Length = length - originalOffset.Length;
                offsetFeedBack.EndPoint = offsetFeedBack.StartPoint + originalOffset + currentOffset;
                Parametrics pm = new Parametrics(shell);
                switch (mode)
                {
                    case Mode.forward:
                        for (int i = 0; i < facesToMove.Count; i++)
                        {
                            pm.MoveFace(facesToMove[i], currentOffset);
                        }
                        break;
                    case Mode.symmetric:
                        for (int i = 0; i < facesToMove.Count; i++)
                        {
                            pm.MoveFace(facesToMove[i], 0.5 * currentOffset);
                        }
                        for (int i = 0; i < facesToKeep.Count; i++)
                        {
                            pm.MoveFace(facesToKeep[i], -0.5 * currentOffset);
                        }
                        break;
                    case Mode.backward:
                        for (int i = 0; i < facesToKeep.Count; i++)
                        {
                            pm.MoveFace(facesToKeep[i], -currentOffset);
                        }
                        break;
                }
                Shell sh = pm.Result(out HashSet<Face> involvedFaces);
                if (sh != null)
                {
                    ActiveObject = sh;
                    validResult = true;
                    return true;
                }
                else
                {
                    ActiveObject = shell.Clone();
                    return false;
                }

            }
            return false;
        }

        private double DistanceInput_GetLengthEvent()
        {
            return (originalOffset + currentOffset).Length;
        }

        private bool SetFacesAndOffset(bool testOnly, object distanceFromHere)
        {
            List<Face> fromHere = new List<Face>();
            List<Face> toHere = new List<Face>();
            Vertex vtx1 = distanceFromHere as Vertex;
            Vertex vtx2 = distanceToHere as Vertex;
            Edge edg1 = distanceFromHere as Edge;
            Edge edg2 = distanceToHere as Edge;
            Face fc1 = distanceFromHere as Face;
            Face fc2 = distanceToHere as Face;
            originalOffset = GeoVector.NullVector;
            if (vtx1 != null && vtx2 != null)
            {
                originalOffset = vtx1.Position - vtx2.Position;
            }
            else if ((vtx1 != null && edg2 != null) || (vtx2 != null && edg1 != null))
            {
                Vertex vtx;
                Edge edg;
                List<Face> lv, le;
                if (vtx1 != null) { vtx = vtx1; edg = edg2; lv = fromHere; le = toHere; }
                else { vtx = vtx2; edg = edg1; lv = toHere; le = fromHere; }
                double cpos = edg.Curve3D.PositionOf(vtx.Position);
                originalOffset = vtx.Position - edg.Curve3D.PointAt(cpos);
                if (vtx1 == null) originalOffset.Reverse();
            }
            else if ((vtx1 != null && fc2 != null) || (vtx2 != null && fc1 != null))
            {
                Vertex vtx;
                Face fc;
                List<Face> lv, le;
                if (vtx1 != null) { vtx = vtx1; fc = fc2; lv = fromHere; le = toHere; }
                else { vtx = vtx2; fc = fc1; lv = toHere; le = fromHere; }
                GeoPoint2D[] pfs = fc.Surface.PerpendicularFoot(vtx.Position);
                for (int i = 0; i < pfs.Length; i++)
                {
                    if (fc.Contains(ref pfs[i], true))
                    {
                        originalOffset = vtx.Position - fc.Surface.PointAt(pfs[i]);
                        if (vtx1 == null) originalOffset.Reverse();
                        break;
                    }
                }
            }
            else if ((edg1 != null && edg2 != null))
            {
                double pos1 = 0.5, pos2 = 0.5;
                if (Curves.NewtonMinDist(edg1.Curve3D, ref pos1, edg2.Curve3D, ref pos2))
                    originalOffset = edg1.Curve3D.PointAt(pos1) - edg2.Curve3D.PointAt(pos2);
                else if (edg1.Curve3D is Line l1 && edg2.Curve3D is Line l2 && Precision.SameDirection(l1.StartDirection, l2.StartDirection, false))
                {
                    double u = edg1.Curve3D.PositionOf(edg2.Curve3D.StartPoint);
                    originalOffset = edg1.Curve3D.PointAt(u) - edg2.Curve3D.StartPoint;
                }
                else if (edg1.Curve3D.GetPlanarState() == PlanarState.Planar && edg2.Curve3D.GetPlanarState() == PlanarState.Planar)
                {   // probably parallel planes, so newton could find anything 
                    GeoPoint foot = edg1.Curve3D.GetPlane().ToLocal(edg2.Curve3D.StartPoint);
                    originalOffset = foot - edg2.Curve3D.StartPoint;
                }
                // else no distance between two curves, maybe check some more cases, which make a usable offset
            }
            else if ((edg1 != null && fc2 != null) || (edg2 != null && fc1 != null))
            {
                Edge edg;
                Face fc;
                List<Face> lv, le;
                if (edg1 != null) { edg = edg1; fc = fc2; lv = fromHere; le = toHere; }
                else { edg = edg2; fc = fc1; lv = toHere; le = fromHere; }
                if (fc.Surface is PlaneSurface pls && edg.Curve3D is Line line)
                {
                    if (Precision.IsPerpendicular(pls.Normal, line.StartDirection, false))
                    {

                        GeoPoint pl = line.PointAt(0.5);
                        GeoPoint2D[] fps = pls.PerpendicularFoot(pl);
                        if (fps.Length == 1)
                        {
                            originalOffset = pl - pls.PointAt(fps[0]);
                        }
                    }
                }
                else if (fc.Surface.GetExtremePositions(fc.Domain, edg.Curve3D, out List<Tuple<double, double, double>> positions) > 0)
                {
                    originalOffset = edg.Curve3D.PointAt(positions[0].Item3) - fc.Surface.PointAt(new GeoPoint2D(positions[0].Item1, positions[0].Item2));
                }
                if (edg1 == null) originalOffset.Reverse();
            }
            else if (fc1 != null && fc2 != null)
            {
                if (fc1.Surface.GetExtremePositions(fc1.Domain, fc2.Surface, fc2.Domain, out List<Tuple<double, double, double, double>> extremePositions) > 0)
                {
                    double mindist = double.MaxValue;
                    for (int i = 0; i < extremePositions.Count; i++)
                    {
                        GeoPoint2D uv1 = new GeoPoint2D(extremePositions[i].Item1, extremePositions[i].Item2);
                        GeoPoint2D uv2 = new GeoPoint2D(extremePositions[i].Item3, extremePositions[i].Item4);
                        GeoVector diff = fc1.Surface.PointAt(uv1) - fc2.Surface.PointAt(uv2);
                        if (diff.Length < mindist)
                        {
                            mindist = diff.Length;
                            originalOffset = diff;
                        }
                    }
                }
            }

            if (originalOffset == GeoVector.NullVector)
            {
                if (!testOnly)
                {
                    facesToKeep.Clear();
                    facesToMove.Clear();
                }
                return false;
            }
            offsetFeedBack.EndPoint = offsetFeedBack.StartPoint + originalOffset;
            if (vtx1 != null)
            {
                foreach (Face fc in vtx1.InvolvedFaces)
                {
                    if (!fc.Surface.IsExtruded(originalOffset)) fromHere.Add(fc);
                }
            }
            if (vtx2 != null)
            {
                foreach (Face fc in vtx2.InvolvedFaces)
                {
                    if (!fc.Surface.IsExtruded(originalOffset)) toHere.Add(fc);
                }
            }
            if (edg1 != null)
            {
                if (!edg1.PrimaryFace.Surface.IsExtruded(originalOffset)) fromHere.Add(edg1.PrimaryFace);
                if (!edg1.SecondaryFace.Surface.IsExtruded(originalOffset)) fromHere.Add(edg1.SecondaryFace);
            }
            if (edg2 != null)
            {
                if (!edg2.PrimaryFace.Surface.IsExtruded(originalOffset)) toHere.Add(edg2.PrimaryFace);
                if (!edg2.SecondaryFace.Surface.IsExtruded(originalOffset)) toHere.Add(edg2.SecondaryFace);
            }
            if (fc1 != null && !fc1.Surface.IsExtruded(originalOffset)) fromHere.Add(fc1);
            if (fc2 != null && !fc2.Surface.IsExtruded(originalOffset)) toHere.Add(fc2);
            if (!testOnly)
            {
                facesToMove = fromHere;
                facesToKeep = toHere;
            }
            return fromHere.Count > 0 && toHere.Count > 0;
        }
        private void OtherObject_GeoObjectSelectionChangedEvent(GeoObjectInput sender, IGeoObject selectedGeoObject)
        {
            bool ok = SetFacesAndOffset(false, selectedGeoObject);
            if (ok)
            {
                distanceFromHere = selectedGeoObject;
                currentOffset = GeoVector.NullVector;
                distanceInput.ForceValue(originalOffset.Length);
            }
            //if (feedbackIndex >= 0) FeedBack.RemoveSelected(feedbackIndex);
            //feedbackIndex = FeedBack.AddSelected(otherObject);
        }

        private bool OtherObject_MouseOverGeoObjectsEvent(GeoObjectInput sender, IGeoObject[] geoObjects, bool up)
        {   // we need to implement more cases here, resulting in faceToMove, faceToKeep (maybe null) and a reference point from where to calculate footpoints for the offset vector
            
            Projection.PickArea pa = CurrentMouseView.Projection.GetPickSpace(new System.Drawing.Rectangle(sender.currentMousePoint.X - 5, sender.currentMousePoint.Y - 5, 10, 10));
            for (int i = 0; i < geoObjects.Length; i++)
            {
                double z = geoObjects[i].Position(pa.FrontCenter, pa.Direction, CurrentMouseView.Projection.Precision);
                object candidate = geoObjects[i];
                if (geoObjects[i].Owner is Edge edg) candidate = edg;
                bool ok = SetFacesAndOffset(!up, candidate);
                if (up && ok)
                {
                    distanceFromHere = candidate;
                    currentOffset = GeoVector.NullVector;
                    distanceInput.ForceValue(originalOffset.Length);
                    List<IGeoObject> currentTargets = new List<IGeoObject>();
                    IGeoObject[] ct = sender.GetGeoObjects();
                    if (ct != null) currentTargets.AddRange(ct);
                    currentTargets.Add(geoObjects[i]);
                    sender.SetGeoObject(currentTargets.ToArray(), geoObjects[i]);
                }
                if (ok)
                {
                    return true;
                }
            }
            return false;
        }
    }
}