using CADability.Attribute;
using CADability.GeoObject;
using netDxf;
using netDxf.Blocks;
using netDxf.Entities;
using netDxf.Tables;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace CADability.DXF
{
    public class Export
    {
        private DxfDocument doc;
        private Dictionary<CADability.Attribute.Layer, netDxf.Tables.Layer> createdLayers;
        Dictionary<CADability.Attribute.LinePattern, netDxf.Tables.Linetype> createdLinePatterns;
        int anonymousBlockCounter = 0;
        public Export(netDxf.Header.DxfVersion version)
        {
            doc = new DxfDocument(version);
            createdLayers = new Dictionary<Attribute.Layer, netDxf.Tables.Layer>();
            createdLinePatterns = new Dictionary<LinePattern, Linetype>();
        }
        public void WriteToFile(Project toExport, string filename)
        {
            Model modelSpace = null;
            if (toExport.GetModelCount() == 1) modelSpace = toExport.GetModel(0);
            else
            {
                modelSpace = toExport.FindModel("*Model_Space");
            }
            if (modelSpace == null) modelSpace = toExport.GetActiveModel();
            for (int i = 0; i < modelSpace.Count; i++)
            {
                EntityObject entity = GeoObjectToEntity(modelSpace[i]);
                if (entity != null) doc.AddEntity(entity);
            }
            doc.Save(filename);
        }

        private EntityObject GeoObjectToEntity(IGeoObject geoObject)
        {
            EntityObject entity = null;
            switch (geoObject)
            {
                case GeoObject.Point point: entity = ExportPoint(point); break;
                case GeoObject.Line line: entity = ExportLine(line); break;
                case GeoObject.Ellipse elli: entity = ExportEllipse(elli); break;
                case GeoObject.Polyline polyline: entity = ExportPolyline(polyline); break;
                case GeoObject.BSpline bspline: entity = ExportBSpline(bspline); break;
                case GeoObject.Path path: entity = ExportPath(path); break;
                case GeoObject.Text text: entity = ExportText(text); break;
                case GeoObject.Block block: entity = ExportBlock(block); break;
            }
            if (entity!=null) SetAttributes(entity, geoObject);
            return entity;
        }

        private EntityObject ExportText(GeoObject.Text text)
        {
            System.Drawing.FontStyle fs = System.Drawing.FontStyle.Regular;
            if (text.Bold) fs |= System.Drawing.FontStyle.Bold;
            if (text.Italic) fs |= System.Drawing.FontStyle.Italic;
            System.Drawing.Font font = new System.Drawing.Font(text.Font, 1000.0f, fs);
            netDxf.Entities.Text res = new netDxf.Entities.Text(text.TextString, Vector2.Zero, text.TextSize * 1000 / font.Height, new TextStyle(text.Font + ".ttf"));
            ModOp toText = ModOp.Fit(GeoPoint.Origin, new GeoVector[] { GeoVector.XAxis, GeoVector.YAxis, GeoVector.ZAxis }, text.Location, new GeoVector[] { text.LineDirection.Normalized, text.GlyphDirection.Normalized, text.LineDirection.Normalized ^ text.GlyphDirection.Normalized });
            res.TransformBy(Matrix4(toText)); // easier than setting normal and rotation
            return res;
        }

        private EntityObject ExportBlock(GeoObject.Block blk)
        {
            List<EntityObject> entities = new List<EntityObject>();
            for (int i = 0; i < blk.Children.Count; i++)
            {
                EntityObject entity = GeoObjectToEntity(blk.Child(i));
                if (entity != null) entities.Add(entity);
            }
            string name = blk.Name;
            if (name==null || doc.Blocks.Contains(name) || !TableObject.IsValidName(name)) name = GetNextAnonymousBlockName();
            netDxf.Blocks.Block block = new netDxf.Blocks.Block(name, entities);
            doc.Blocks.Add(block);
            return new netDxf.Entities.Insert(block);
        }
        private EntityObject ExportPath(Path path)
        {
            List<EntityObject> entities = new List<EntityObject>();
            for (int i = 0; i < path.Curves.Length; i++)
            {
                EntityObject curve = GeoObjectToEntity(path.Curves[i] as IGeoObject);
                if (curve != null) entities.Add(curve);
            }
            netDxf.Blocks.Block block = new netDxf.Blocks.Block(GetNextAnonymousBlockName(), entities);
            doc.Blocks.Add(block);
            return new netDxf.Entities.Insert(block);
        }

        private EntityObject ExportBSpline(BSpline bspline)
        {
            List<SplineVertex> vertices = new List<SplineVertex>();
            for (int i = 0; i < bspline.Poles.Length; i++)
            {
                if (bspline.HasWeights) vertices.Add(new SplineVertex(Vector3(bspline.Poles[i]), bspline.Weights[i]));
                else vertices.Add(new SplineVertex(Vector3(bspline.Poles[i])));
            }
            List<double> knots = new List<double>();
            for (int i = 0; i < bspline.Knots.Length; i++)
            {
                for (int j = 0; j < bspline.Multiplicities[i]; j++) knots.Add(bspline.Knots[i]);
            }
            return new netDxf.Entities.Spline(vertices, knots, (short)bspline.Degree);
        }

        private EntityObject ExportPolyline(GeoObject.Polyline polyline)
        {
            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < polyline.Vertices.Length; i++)
            {
                vertices.Add(Vector3(polyline.Vertices[i]));
            }
            if (polyline.IsClosed) vertices.Add(Vector3(polyline.Vertices[0]));
            return new netDxf.Entities.Polyline(vertices);
        }

        private EntityObject ExportPoint(GeoObject.Point point)
        {
            return new netDxf.Entities.Point(Vector3(point.Location));
        }
        private EntityObject ExportLine(GeoObject.Line line)
        {
            return new netDxf.Entities.Line(Vector3(line.StartPoint), Vector3(line.EndPoint));
        }
        private EntityObject ExportEllipse(GeoObject.Ellipse elli)
        {
            netDxf.Entities.EntityObject entity = null;
            if (elli.IsArc)
            {
                Plane dxfPlane;
                if (elli.CounterClockWise) dxfPlane = Import.Plane(Vector3(elli.Center), Vector3(elli.Plane.Normal));
                else dxfPlane = Import.Plane(Vector3(elli.Center), Vector3(-elli.Plane.Normal));
                if (elli.IsCircle)
                {
                    GeoObject.Ellipse aligned = GeoObject.Ellipse.Construct();
                    aligned.SetArcPlaneCenterStartEndPoint(dxfPlane, dxfPlane.Project(elli.Center), dxfPlane.Project(elli.StartPoint), dxfPlane.Project(elli.EndPoint), dxfPlane, true);
                    entity = new netDxf.Entities.Arc(Vector3(aligned.Center), aligned.Radius, aligned.StartParameter / Math.PI * 180, (aligned.StartParameter + aligned.SweepParameter) / Math.PI * 180);
                    entity.Normal = Vector3(dxfPlane.Normal);
                }
                else
                {
                    netDxf.Entities.Ellipse expelli = new netDxf.Entities.Ellipse(Vector3(elli.Center), 2 * elli.MajorRadius, 2 * elli.MinorRadius);
                    entity = expelli;
                    entity.Normal = Vector3(elli.Plane.Normal);
                    Plane cdbplane = elli.Plane;
                    GeoVector2D dir = dxfPlane.Project(cdbplane.DirectionX);
                    SweepAngle rot = new SweepAngle(GeoVector2D.XAxis, dir);
                    expelli.Rotation = rot.Degree;
                    SetEllipseParameters(expelli, elli.StartParameter, elli.StartParameter + elli.SweepParameter);
                }
            }
            else
            {
                if (elli.IsCircle)
                {
                    entity = new netDxf.Entities.Circle(Vector3(elli.Center), elli.Radius);
                    entity.Normal = Vector3(elli.Plane.Normal);
                }
                else
                {
                    netDxf.Entities.Ellipse expelli = new netDxf.Entities.Ellipse(Vector3(elli.Center), 2 * elli.MajorRadius, 2 * elli.MinorRadius);
                    entity = expelli;
                    entity.Normal = Vector3(elli.Plane.Normal);
                    Plane dxfplane = Import.Plane(expelli.Center, expelli.Normal); // this plane is not correct, it has to be rotated
                    Plane cdbplane = elli.Plane;
                    GeoVector2D dir = dxfplane.Project(cdbplane.DirectionX);
                    SweepAngle rot = new SweepAngle(GeoVector2D.XAxis, dir);
                    expelli.Rotation = rot.Degree;
                }
            }
            return entity;
        }
        private static void SetEllipseParameters(netDxf.Entities.Ellipse ellipse, double startparam, double endparam)
        {
            //CADability: also set the start and end parameter
            ellipse.StartParameter = startparam;
            ellipse.EndParameter = endparam;
            if (MathHelper.IsZero(startparam) && MathHelper.IsEqual(endparam, MathHelper.TwoPI))
            {
                ellipse.StartAngle = 0.0;
                ellipse.EndAngle = 0.0;
            }
            else
            {
                double a = ellipse.MajorAxis * 0.5;
                double b = ellipse.MinorAxis * 0.5;

                Vector2 startPoint = new Vector2(a * Math.Cos(startparam), b * Math.Sin(startparam));
                Vector2 endPoint = new Vector2(a * Math.Cos(endparam), b * Math.Sin(endparam));

                if (Vector2.Equals(startPoint, endPoint))
                {
                    ellipse.StartAngle = 0.0;
                    ellipse.EndAngle = 0.0;
                }
                else
                {
                    ellipse.StartAngle = Vector2.Angle(startPoint) * MathHelper.RadToDeg;
                    ellipse.EndAngle = Vector2.Angle(endPoint) * MathHelper.RadToDeg;
                }
            }
        }

        private void SetAttributes(EntityObject entity, IGeoObject go)
        {
            if (go is IColorDef cd && cd.ColorDef != null)
            {
                AciColor clr = AciColor.FromTrueColor(cd.ColorDef.Color.ToArgb());
                entity.Color = clr;
            }
            if (go.Layer != null)
            {
                if (!createdLayers.TryGetValue(go.Layer, out netDxf.Tables.Layer layer))
                {
                    layer = new netDxf.Tables.Layer(go.Layer.Name);
                    doc.Layers.Add(layer);
                    createdLayers[go.Layer] = layer;
                }
                entity.Layer = layer;
            }
            if (go is ILinePattern lp)
            {
                if (!createdLinePatterns.TryGetValue(lp.LinePattern, out Linetype linetype))
                {
                    List<LinetypeSegment> segments = new List<LinetypeSegment>();
                    if (lp.LinePattern.Pattern != null)
                    {
                        for (int i = 0; i < lp.LinePattern.Pattern.Length; i++)
                        {
                            LinetypeSegment ls;
                            if ((i & 0x01) == 0) ls = new LinetypeSimpleSegment(lp.LinePattern.Pattern[i]);
                            else ls = new LinetypeSimpleSegment(-lp.LinePattern.Pattern[i]);
                            segments.Add(ls);
                        }
                    }
                    linetype = new Linetype(lp.LinePattern.Name);
                    linetype.Segments.AddRange(segments);
                    doc.Linetypes.Add(linetype);
                    createdLinePatterns[lp.LinePattern] = linetype;
                }
                entity.Linetype = linetype;
            }
            if (go is ILineWidth lw)
            {
                double minError = double.MaxValue;
                Lineweight found = Lineweight.Default;
                foreach (Lineweight lwe in Enum.GetValues(typeof(Lineweight)))
                {
                    double err = Math.Abs(((int)lwe) / 100.0 - lw.LineWidth.Width);
                    if (err < minError)
                    {
                        minError = err;
                        found = lwe;
                    }
                }
                entity.Lineweight = found;
            }
        }
        private Vector3 Vector3(GeoPoint p)
        {
            return new Vector3(p.x, p.y, p.z);
        }
        private Vector3 Vector3(GeoVector v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
        private netDxf.Matrix4 Matrix4(ModOp toText)
        {
            return new netDxf.Matrix4(toText[0, 0], toText[0, 1], toText[0, 2], toText[0, 3], toText[1, 0], toText[1, 1], toText[1, 2], toText[1, 3], toText[2, 0], toText[2, 1], toText[2, 2], toText[2, 3], 0, 0, 0, 1);
        }
        private string GetNextAnonymousBlockName()
        {
            return "AnonymousBlock" + (++anonymousBlockCounter).ToString();
        }
    }
}
