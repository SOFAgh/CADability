using CADability.GeoObject;
using System.Collections.Generic;

namespace CADability.Actions
{
    internal class Constr3DFillet : ConstructAction
    {
        private GeoObjectInput geoObjectInput;
        private GeoObjectInput faceInput;
        private GeoObjectInput shellInput;
        private LengthInput radiusInput;
        private List<Edge> edges;
        private double radius;

        public Constr3DFillet()
        {
            edges = new List<Edge>();
        }

        public override void OnSetAction()
        {
            base.TitleId = "Constr.Fillet";

            geoObjectInput = new GeoObjectInput("Constr.Fillet.Edges");
            geoObjectInput.EdgesOnly = true;
            geoObjectInput.MultipleInput = true;
            geoObjectInput.MouseOverGeoObjectsEvent += new GeoObjectInput.MouseOverGeoObjectsDelegate(OnMouseOverEdges);
            // we would need a mass selection (rectangular selection) for GeoObjectInput

            faceInput = new GeoObjectInput("Constr.Fillet.Face");
            faceInput.FacesOnly = true;
            faceInput.Optional = true;
            faceInput.MultipleInput = false;
            faceInput.MouseOverGeoObjectsEvent += new GeoObjectInput.MouseOverGeoObjectsDelegate(OnMouseOverFace);

            shellInput = new GeoObjectInput("Constr.Fillet.Shell");
            shellInput.Optional = true;
            shellInput.MultipleInput = false;
            shellInput.MouseOverGeoObjectsEvent += new GeoObjectInput.MouseOverGeoObjectsDelegate(OnMouseOverShell);

            radiusInput = new LengthInput("Constr.Fillet.Radius");
            radiusInput.SetLengthEvent += new LengthInput.SetLengthDelegate(OnSetRadius);
            radiusInput.GetLengthEvent += new LengthInput.GetLengthDelegate(OnGetRadius);
            radiusInput.DefaultLength = ConstrDefaults.DefaultRoundRadius;
            radiusInput.ForwardMouseInputTo = geoObjectInput;

            base.SetInput(geoObjectInput, faceInput, shellInput, radiusInput);
            base.OnSetAction();
        }

        double OnGetRadius()
        {
            return radius;
        }

        bool OnSetRadius(double Length)
        {
            if (Length > 0.0)
            {
                radius = Length;
                return true;
            }
            return false;
        }

        bool OnMouseOverEdges(ConstructAction.GeoObjectInput sender, IGeoObject[] TheGeoObjects, bool up)
        {
            bool ok = false;
            Edge toSelect = null;
            for (int i = 0; i < TheGeoObjects.Length; ++i)
            {
                if (TheGeoObjects[i].Owner is Edge)
                {
                    ok = true;
                    if (up)
                    {
                        Edge edge = TheGeoObjects[i].Owner as Edge;
                        if (edges.Contains(edge))
                        {
                            edges.Remove(edge);
                            base.FeedBack.RemoveSelected(edge.Curve3D as IGeoObject);
                        }
                        else
                        {
                            toSelect = edge;
                            edges.Add(edge);
                            base.FeedBack.AddSelected(edge.Curve3D as IGeoObject);
                            geoObjectInput.Optional = true;
                        }
                    }
                }
            }
            if (up)
            {
                List<IGeoObject> sel = new List<IGeoObject>();
                for (int i = 0; i < edges.Count; ++i)
                {
                    sel.Add(edges[i].Curve3D as IGeoObject);
                }
                if (toSelect != null)
                    geoObjectInput.SetGeoObject(sel.ToArray(), toSelect.Curve3D as IGeoObject);
                else
                    geoObjectInput.SetGeoObject(sel.ToArray(), null);
            }
            return ok;
        }

        bool OnMouseOverFace(ConstructAction.GeoObjectInput sender, IGeoObject[] TheGeoObjects, bool up)
        {
            bool ok = false;
            for (int i = 0; i < TheGeoObjects.Length; ++i)
            {
                if (TheGeoObjects[i] is Face)
                {
                    ok = true;
                    if (up)
                    {
                        Face theFace = (TheGeoObjects[i] as Face);
                        List<Edge> edgesTemp = new List<Edge>(theFace.AllEdges);
                        List<IGeoObject> geoObjects = new List<IGeoObject>();
                        foreach (Edge edge in edgesTemp)
                        {
                            IGeoObject go = edge.Curve3D as IGeoObject;
                            if (go != null)
                            {
                                // base.FeedBack.AddSelected(go);
                                if (edges.Contains(edge))
                                {
                                    edges.Remove(edge);
                                    base.FeedBack.RemoveSelected(edge.Curve3D as IGeoObject);
                                }
                                else
                                {
                                    edges.Add(edge);
                                    base.FeedBack.AddSelected(edge.Curve3D as IGeoObject);
                                    geoObjectInput.Optional = true;
                                }
                                geoObjects.Add(go);
                            }
                        }
                        geoObjectInput.SetGeoObject(geoObjects.ToArray(), null);
                    }
                }
            }
            return ok;
        }

        bool OnMouseOverShell(ConstructAction.GeoObjectInput sender, IGeoObject[] TheGeoObjects, bool up)
        {
            bool ok = false;
            for (int i = 0; i < TheGeoObjects.Length; ++i)
            {
                if ((TheGeoObjects[i] is Solid) || (TheGeoObjects[i] is Shell))
                {
                    ok = true;
                    if (up)
                    {
                        List<Edge> edgesTemp;
                        if (TheGeoObjects[i] is Solid)
                        {
                            Solid theSolid = (TheGeoObjects[i] as Solid);
                            edgesTemp = new List<Edge>(theSolid.Edges);
                        }
                        else
                        {
                            Shell theShell = (TheGeoObjects[i] as Shell);
                            edgesTemp = new List<Edge>(theShell.Edges);

                        }
                        List<IGeoObject> geoObjects = new List<IGeoObject>();
                        foreach (Edge edge in edgesTemp)
                        {
                            IGeoObject go = edge.Curve3D as IGeoObject;
                            if (go != null)
                            {
                                // base.FeedBack.AddSelected(go);
                                if (edges.Contains(edge))
                                {
                                    edges.Remove(edge);
                                    base.FeedBack.RemoveSelected(edge.Curve3D as IGeoObject);
                                }
                                else
                                {
                                    if (edge.SecondaryFace != null)
                                    {
                                        edges.Add(edge);
                                        base.FeedBack.AddSelected(edge.Curve3D as IGeoObject);
                                        geoObjectInput.Optional = true;
                                    }
                                }
                                geoObjects.Add(go);
                            }
                        }
                        geoObjectInput.SetGeoObject(geoObjects.ToArray(), null);
                    }
                }
            }
            return ok;
        }

        public override string GetID()
        {
            return "Constr.Fillet";
        }
        public override void OnDone()
        {
            if (edges.Count > 0)
            {
                IGeoObject[] affected;
                IGeoObject[] modified = Make3D.MakeFillet(edges.ToArray(), radius, out affected);
                if (affected!=null && affected.Length > 0)
                {
                    using (Frame.Project.Undo.UndoFrame)
                    {
                        IGeoObjectOwner owner = null; // should be a solid if the affected shell is part of a solid, or owner should be the model
                        // only the edges of a single shell should be rounded!
                        for (int i = 0; i < affected.Length; ++i)
                        {
                            if (owner == null || affected[i].Owner is Model)
                            {
                                owner = affected[i].Owner;
                            }
                            affected[i].Owner.Remove(affected[i]);
                        }
                        if (owner is Model model)
                        {
                            model.Remove(affected);
                            model.Add(modified);
                        }
                        else if (owner is Solid sld)
                        {
                            Model m = sld.Owner as Model;
                            if (m!=null)    // not able to round edges of Solid which is part of a Block?
                            {
                                m.Remove(sld);
                                for (int i = 0; i < modified.Length; ++i)
                                {
                                    Solid rsld = Solid.Construct();
                                    rsld.SetShell(modified[i] as Shell);
                                    m.Add(rsld);
                                }
                            }
                        }
                    }
                }
            }
            base.OnDone();
        }
    }
}
