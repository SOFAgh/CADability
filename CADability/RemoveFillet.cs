using CADability.GeoObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CADability
{
    public class RemoveFillet
    {
        Shell shell;
        HashSet<Face> fillets;
        public RemoveFillet(Shell shell, HashSet<Face> fillets)
        {   // make a clone of the shell to work on and use the fillets of the cloned shell
            Dictionary<Edge, Edge> clonedEdges = new Dictionary<Edge, Edge>();
            Dictionary<Vertex, Vertex> clonedVertices = new Dictionary<Vertex, Vertex>();
            Dictionary<Face, Face> clonedFaces = new Dictionary<Face, Face>();
            this.shell = shell.Clone(clonedEdges, clonedVertices, clonedFaces);
            this.fillets = new HashSet<Face>();
            foreach (Face face in fillets)
            {
                this.fillets.Add(clonedFaces[face]);
            }
        }

        public Shell Result()
        {
            // the fillets edges fall into one of three categories: 
            // - lengthwayTangential: edges that are tangential to other faces, i.e. they round the other faces
            // - crossway: edges that connect fillets
            // - unrelated: here the fillets end or are otherwise broken up
            HashSet<Face> lengthwayTangential = new HashSet<Face>(); // the two faces that this fillet rounds
            HashSet<Edge> crossway = new HashSet<Edge>(); // connection to the following or previous fillet
            HashSet<Edge> lengthway = new HashSet<Edge>(); // connection between fillet and rounded face
            HashSet<Edge> filletEnd = new HashSet<Edge>(); // an edge which is the end of a fillet
            Dictionary<Edge, Face> forking = new Dictionary<Edge, Face>(); // a spherical face, where the fillets fork
            foreach (Face face in fillets)
            {
                foreach (Edge edge in face.AllEdgesIterated())
                {
                    if (face.Surface is ISurfaceOfArcExtrusion extrusion)
                    {
                        Face otherFace = edge.OtherFace(face);
                        if (edge.IsTangentialEdge() && edge.Curve2D(face).DirectionAt(0.5).IsMoreHorizontal != extrusion.ExtrusionDirectionIsV)
                        {
                            lengthwayTangential.Add(otherFace);
                            lengthway.Add(edge);
                        }
                        else if (fillets.Contains(otherFace)) crossway.Add(edge);
                        else filletEnd.Add(edge);
                    }
                }
            }
            Dictionary<Edge, Vertex> crosswayToVertex = new Dictionary<Edge, Vertex>();
            foreach (Edge edg in Extensions.Combine<Edge>(crossway, filletEnd))
            {
                HashSet<Face> involved = edg.Vertex1.InvolvedFaces;
                involved.UnionWith(edg.Vertex2.InvolvedFaces);
                involved.ExceptWith(fillets);
                if (involved.Count >= 3) // which should be the case 
                {
                    Face[] ia = involved.ToArray();
                    GeoPoint ip = edg.Curve3D.PointAt(0.5);
                    if (Surfaces.NewtonIntersect(ia[0].Surface, ia[0].Domain, ia[1].Surface, ia[1].Domain, ia[2].Surface, ia[2].Domain, ref ip, out GeoPoint2D uv0, out GeoPoint2D uv1, out GeoPoint2D uv2))
                    {   // there should be an intersection point close to the middle of the crossway edge
                        crosswayToVertex[edg] = new Vertex(ip);
                    }
                }
            }
            // replace all length-way edges
            foreach (Face face in fillets)
            {
                if (face.Surface is ISurfaceOfArcExtrusion extrusion)
                {
                    HashSet<Edge> all = new HashSet<Edge>(face.AllEdges);
                    List<Edge> lw = new List<Edge>(all.Intersect(lengthway)); // this should be two edges
                    List<Edge> cw = new List<Edge>(all.Intersect(crossway)); // this should also be two edges
                    if (lw.Count == 2 && cw.Count == 2)
                    {
                        Face[] toReplaceEdge = new Face[2];
                        for (int i = 0; i < 2; i++)
                        {
                            if (lw[i].PrimaryFace == face) toReplaceEdge[i] = lw[i].SecondaryFace;
                            else toReplaceEdge[i] = lw[i].PrimaryFace;
                        }
                        Vertex v0 = crosswayToVertex[cw[0]];
                        Vertex v1 = crosswayToVertex[cw[1]];
                        BoundingRect domain0 = toReplaceEdge[0].Domain;
                        domain0.MinMax(toReplaceEdge[0].Surface.PositionOf(v0.Position));
                        domain0.MinMax(toReplaceEdge[0].Surface.PositionOf(v1.Position));
                        BoundingRect domain1 = toReplaceEdge[1].Domain;
                        domain1.MinMax(toReplaceEdge[1].Surface.PositionOf(v0.Position));
                        domain1.MinMax(toReplaceEdge[1].Surface.PositionOf(v1.Position));
                        ICurve[] newCurves = toReplaceEdge[0].Surface.Intersect(domain0, toReplaceEdge[1].Surface, domain1);
                        ICurve newCurve = Hlp.GetClosest(newCurves, crv => crv.DistanceTo(v0.Position) + crv.DistanceTo(v1.Position));
                        if (newCurve != null)
                        {
                            double pos0 = newCurve.PositionOf(v0.Position);
                            double pos1 = newCurve.PositionOf(v1.Position);
                            if (pos1 < pos0)
                            {
                                newCurve.Reverse();
                                pos0 = 1 - pos0;
                                pos1 = 1 - pos1;
                            }
                            newCurve.Trim(pos0, pos1);
                            Edge newEdge = new Edge(newCurve, v0, v1);
                            newEdge.SetPrimary(toReplaceEdge[0], lw[0].Curve3D.DirectionAt(0.5) * newCurve.DirectionAt(0.5) > 0);
                            newEdge.SetSecondary(toReplaceEdge[1], lw[1].Curve3D.DirectionAt(0.5) * newCurve.DirectionAt(0.5) > 0);
                            //toReplaceEdge[0].ReplaceEdge(lw[0], newEdge);
                            //toReplaceEdge[1].ReplaceEdge(lw[1], newEdge);
                        }
                    }
                }
            }
            return null;
        }
    }
}
