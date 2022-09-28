using System;
using System.Collections.Generic;
using System.Text;
using CADability.GeoObject;
using CADability.UserInterface;

namespace CADability
{
    internal abstract class ParametricProperty : IJsonSerialize
    {
        public string Name { get; set; }
        public abstract double Value { get; set; } // the value of the property is kept here
        public ParametricProperty(string name)
        {
            Name = name;
        }
        /// <summary>
        /// When the shell is beeing modified, we have the chance here to update the parametric property if necessary
        /// </summary>
        /// <param name="m"></param>
        public abstract void Modify(ModOp m);
        /// <summary>
        /// When the shell is cloned, we need to replace the faces, edges and vertices with their clones
        /// </summary>
        /// <param name="clonedFaces"></param>
        /// <param name="clonedEdges"></param>
        /// <param name="clonedVertices"></param>
        /// <returns></returns>
        public abstract ParametricProperty Clone(Dictionary<Face, Face> clonedFaces, Dictionary<Edge, Edge> clonedEdges, Dictionary<Vertex, Vertex> clonedVertices);
        /// <summary>
        /// Execute the parametrics. This will change the shell and the resulting shell may be inconsistent. But maybe further applications of other parametrics to the shell
        /// make it consistent again
        /// </summary>
        /// <param name="parametrics"></param>
        public abstract void Execute(Parametric parametrics);
        /// <summary>
        /// Returns the object on which serves as a anchorpoint for the operation
        /// </summary>
        /// <returns></returns>
        public abstract object GetAnchor();
        /// <summary>
        /// Returns the faces, which will be modified by this parametric property
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<object> GetAffectedObjects();
        public Shell GetResult(Parametric parametrics)
        {
            return parametrics.Result();
        }
        /// <summary>
        /// Returns a list of objects that can be used as a feedback for this property (usually an arrow)
        /// </summary>
        /// <param name="projection"></param>
        /// <returns></returns>
        public abstract GeoObjectList GetFeedback(Projection projection);
        public IPropertyEntry PropertyEntry { get; set; }
        public virtual void GetObjectData(IJsonWriteData data)
        {
            data.AddProperty("Name", Name);
        }
        public virtual void SetObjectData(IJsonReadData data)
        {
            Name = data.GetStringProperty("Name");
        }
    }
    internal class ParametricDistanceProperty : ParametricProperty, IJsonSerialize, IJsonSerializeDone
    {
        private List<Face> facesToKeep, facesToMove; // if mode is symmetric, both faces will be moved
        private List<object> affectedObjects; // faces which are affected by this parametric
        private object fromHere, toHere; // vertices, edges or faces. they specify the direction, in which to move and the position of the arrow
        [Flags]
        public enum Mode
        {
            connected = 1, // call parametric.MoveFaces with moveConnected==true
            symmetric = 2, // use facesToKeep with -0.5 and facesToMove with +0.5 factor to the direction
            fromAxis = 4, // fromHere is a face of which the axis is used
            toAxis = 8, // toHere is a face of which the axis is used
            fromFurthest = 16, // fromHere is a face or edge (curve). Normally we use the closest connection to toHere, with this flag set we use the furthest
            toFurthest = 32,// toHere is a face or edge (curve). Normally we use the closest connection to toHere, with this flag set we use the furthest
        }
        private Mode mode;

        public ParametricDistanceProperty(string name, IEnumerable<Face> facesToKeep, IEnumerable<Face> facesToMove, IEnumerable<object> affectedObjects, Mode mode, object fromHere, object toHere) : base(name)
        {
            this.facesToMove = new List<Face>(facesToMove);
            this.facesToKeep = new List<Face>(facesToKeep);
            this.affectedObjects = new List<object>(affectedObjects);
            this.mode = mode;
            this.fromHere = fromHere;
            this.toHere = toHere;
            GetDistanceVector(out GeoPoint startPoint, out GeoPoint endPoint);
            Value = startPoint | endPoint;
        }

        public override ParametricProperty Clone(Dictionary<Face, Face> clonedFaces, Dictionary<Edge, Edge> clonedEdges, Dictionary<Vertex, Vertex> clonedVertices)
        {
            List<Face> facesToKeepCloned = new List<Face>();
            List<Face> facesToMoveCloned = new List<Face>();
            List<object> affectedObjectsCloned = new List<object>();
            object fromHereCloned, toHereCloned;
            for (int i = 0; i < facesToKeep.Count; i++)
            {
                if (clonedFaces.TryGetValue(facesToKeep[i], out Face clone)) facesToKeepCloned.Add(clone);
            }
            for (int i = 0; i < facesToMove.Count; i++)
            {
                if (clonedFaces.TryGetValue(facesToMove[i], out Face clone)) facesToMoveCloned.Add(clone);
            }
            for (int i = 0; i < affectedObjects.Count; i++)
            {
                if (affectedObjects[i] is Face face && clonedFaces.TryGetValue(face, out Face clonedFace)) affectedObjectsCloned.Add(clonedFace);
                if (affectedObjects[i] is Edge edge && clonedEdges.TryGetValue(edge, out Edge clonedEdge)) affectedObjectsCloned.Add(clonedEdge);
                if (affectedObjects[i] is Vertex vtx && clonedVertices.TryGetValue(vtx, out Vertex clonedVertex)) affectedObjectsCloned.Add(clonedVertex);
            }
            {
                if (fromHere is GeoPoint pnt) fromHereCloned = pnt;
                else if (fromHere is Vertex vtx) fromHereCloned = clonedVertices[vtx];
                else if (fromHere is Edge edg) fromHereCloned = clonedEdges[edg];
                else if (fromHere is Face face) fromHereCloned = clonedFaces[face];
                else fromHereCloned = null; // should not happen
            }
            {
                if (toHere is GeoPoint pnt) toHereCloned = pnt;
                else if (toHere is Vertex vtx) toHereCloned = clonedVertices[vtx];
                else if (toHere is Edge edg) toHereCloned = clonedEdges[edg];
                else if (toHere is Face face) toHereCloned = clonedFaces[face];
                else toHereCloned = null; // should not happen
            }
            return new ParametricDistanceProperty(Name, facesToKeepCloned, facesToMoveCloned, affectedObjectsCloned, mode, fromHereCloned, toHereCloned);
        }
        public override void Modify(ModOp m)
        {   // there is nothing to do, all referred objects like facesToMove or fromHere are part of the shell and already modified
            // with fromHere and toHere as GeoPoints we need to modify these points along with the modification of the Shell containing this parametric
            if (fromHere is GeoPoint point1 && toHere is GeoPoint point2)
            {
                fromHere = m * point1;
                toHere = m * point2;
            }
        }
        private double currentValue;
        public override double Value
        {
            get
            {
                GetDistanceVector(out GeoPoint startPoint, out GeoPoint endPoint);
                return startPoint | endPoint;
            }
            set
            {
                currentValue = value;
            }
        }
        public override void Execute(Parametric parametric)
        {
            GetDistanceVector(out GeoPoint startPoint, out GeoPoint endPoint);
            GeoVector delta = (endPoint - startPoint);
            double diff = currentValue - delta.Length;
            delta = diff * delta.Normalized;
            Dictionary<Face, GeoVector> facesToAffect = new Dictionary<Face, GeoVector>();
            if (mode.HasFlag(Mode.symmetric))
            {
                foreach (Face face in facesToKeep)
                {
                    facesToAffect[face] = -0.5 * delta;
                }
                foreach (Face face in facesToMove)
                {
                    facesToAffect[face] = 0.5 * delta;
                }
            }
            else
            {
                foreach (Face face in facesToMove)
                {
                    facesToAffect[face] = delta;
                }
            }
            parametric.MoveFaces(facesToAffect, endPoint - startPoint, mode.HasFlag(Mode.connected));
            parametric.Apply(); // the result may be inconsistent, but maybe further parametric operations make it consistent again
        }
        private void GetDistanceVector(out GeoPoint startPoint, out GeoPoint endPoint)
        {
            startPoint = endPoint = GeoPoint.Invalid;
            if (fromHere is GeoPoint point1 && toHere is GeoPoint point2)
            {   // I think, all other cases should be eliminated
                startPoint = point1;
                endPoint = point2;
                return;
            }
            if (fromHere is Vertex vtx)
            {
                if (toHere is Vertex vtx2)
                {
                    startPoint = vtx.Position;
                    endPoint = vtx2.Position;
                    return;
                }
                else if (toHere is Edge edge2)
                {
                    double pos = edge2.Curve3D.PositionOf(vtx.Position);
                    if (pos >= -1e-6 && pos <= 1 + 1e-6)
                    {
                        startPoint = vtx.Position;
                        endPoint = edge2.Curve3D.PointAt(pos);
                        return;
                    }
                }
                else if (toHere is Face fc2)
                {
                    GeoPoint2D[] fps = fc2.Surface.PerpendicularFoot(vtx.Position);
                    if (fps != null && fps.Length > 0)
                    {
                        endPoint = startPoint = vtx.Position;
                        double minDist = double.MaxValue;
                        for (int i = 0; i < fps.Length; i++)
                        {
                            GeoPoint ep = fc2.Surface.PointAt(fps[i]);
                            double d = ep | startPoint;
                            if (d < minDist)
                            {
                                minDist = d;
                                endPoint = ep;
                            }
                        }
                        return;
                    }
                }
            }
            else if (fromHere is Edge edge)
            {
                if (toHere is Vertex vtx2)
                {
                    double pos = edge.Curve3D.PositionOf(vtx2.Position);
                    if (pos >= -1e-6 && pos <= 1 + 1e-6)
                    {
                        endPoint = vtx2.Position;
                        startPoint = edge.Curve3D.PointAt(pos);
                        return;
                    }
                }
                else if (toHere is Edge edge2)
                {
                    if (edge.Curve3D is Line l1 && edge2.Curve3D is Line l2)
                    {
                        if (Precision.SameDirection(l1.StartDirection, l2.StartDirection, false))
                        {   // two parallel lines
                            double spos = Geometry.LinePar(l1.StartPoint, l1.EndPoint, l2.StartPoint);
                            double epos = Geometry.LinePar(l1.StartPoint, l1.EndPoint, l2.EndPoint);
                            if (epos < spos) (spos, epos) = (epos, spos);
                            if (epos < 0)
                            {
                                spos = (epos + 0.0) / 2.0;
                                epos = 0.0;
                            }
                            else if (spos > 1)
                            {
                                epos = (spos + 1.0) / 2.0;
                                spos = 1.0;
                            }
                            else
                            {
                                spos = Math.Max(0.0, spos);
                                epos = Math.Min(epos, 1.0);
                            }
                            startPoint = Geometry.LinePos(l1.StartPoint, l1.EndPoint, (spos + epos) / 2.0);
                            endPoint = Geometry.DropPL(startPoint, l2.StartPoint, l2.EndPoint);
                            return;
                        }
                        else
                        {   // two skewed lines
                            Geometry.ConnectLines(l1.StartPoint, l1.StartDirection, l2.StartPoint, l2.StartDirection, out double pos1, out double pos2);
                            startPoint = Geometry.LinePos(l1.StartPoint, l1.EndPoint, pos1);
                            endPoint = Geometry.LinePos(l2.StartPoint, l2.EndPoint, pos2);
                            return;
                        }
                    }
                    // more cases ...
                }
                else if (toHere is Face fc2)
                {
                    // to fill out
                }
            }
            else if (fromHere is Face face)
            {
                // to fill out
            }

        }
        public override GeoObjectList GetFeedback(Projection projection)
        {
            GetDistanceVector(out GeoPoint startPoint, out GeoPoint endPoint);
            if (mode.HasFlag(Mode.symmetric))
            {
                GeoObjectList res = new GeoObjectList();
                GeoPoint middlePoint = new GeoPoint(startPoint, endPoint);
                res.AddRange(projection.MakeArrow(middlePoint, startPoint, projection.ProjectionPlane, Projection.ArrowMode.circleArrow));
                res.AddRange(projection.MakeArrow(middlePoint, endPoint, projection.ProjectionPlane, Projection.ArrowMode.circleArrow));
                return res;
            }
            else return projection.MakeArrow(startPoint, endPoint, projection.ProjectionPlane, Projection.ArrowMode.circleArrow);
        }
        protected ParametricDistanceProperty() : base("") { } // empty constructor for Json
        public override void GetObjectData(IJsonWriteData data)
        {
            base.GetObjectData(data);

            data.AddProperty("FacesToMove", facesToMove);
            data.AddProperty("FacesToKeep", facesToKeep);
            data.AddProperty("AffectedObjects", affectedObjects);
            data.AddProperty("Mode", mode);
            data.AddProperty("FromHere", fromHere);
            data.AddProperty("ToHere", toHere);
        }

        public override void SetObjectData(IJsonReadData data)
        {
            base.SetObjectData(data);
            facesToMove = data.GetProperty<List<Face>>("FacesToMove");
            facesToKeep = data.GetProperty<List<Face>>("FacesToKeep");
            affectedObjects = data.GetPropertyOrDefault<List<object>>("AffectedObjects");
            mode = data.GetProperty<Mode>("Mode");
            fromHere = data.GetProperty("FromHere");
            toHere = data.GetProperty("ToHere");
            // there is a problem with objects been serialized as "JsonVersion.serializeAsStruct". fromHere and toHere should always be GeoPoints in future versions
            if (fromHere is double[]) fromHere = data.GetProperty<GeoPoint>("FromHere");
            if (toHere is double[]) toHere = data.GetProperty<GeoPoint>("ToHere");
            data.RegisterForSerializationDoneCallback(this);
        }

        public override object GetAnchor()
        {
            return fromHere;
        }

        public override IEnumerable<object> GetAffectedObjects()
        {
            return affectedObjects;
        }

        void IJsonSerializeDone.SerializationDone()
        {
            GetDistanceVector(out GeoPoint startPoint, out GeoPoint endPoint);
            Value = endPoint | startPoint;
        }

#if DEBUG
        public DebuggerContainer DebugAffected
        {
            get
            {

                DebuggerContainer dc = new DebuggerContainer();
                if (affectedObjects != null)
                {
                    foreach (var obj in affectedObjects)
                    {
                        if (obj is Edge edge) dc.Add(edge.Curve3D as IGeoObject, edge.GetHashCode());
                        if (obj is Vertex vtx) dc.Add(vtx.Position, System.Drawing.Color.Red, vtx.GetHashCode());
                        if (obj is Face face) dc.Add(face, face.GetHashCode());
                    }
                }
                return dc;
            }
        }
#endif
    }
}
