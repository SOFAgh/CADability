using CADability.Actions;
using CADability.Attribute;
using CADability.Curve2D;
using CADability.GeoObject;
using System;
using System.Collections;
using System.Collections.Generic;
using Wintellect.PowerCollections;

namespace CADability.Shapes
{
    /// <summary>
    /// Ein Verbindungspunkt. Die Kurve curve startet oder endet hier. Findet Verwendung
    /// im Cluster, der i.A. aus mehreren solchen Verbindungspunkten besteht.
    /// </summary>
    internal class Joint : IComparable
    {
        public ICurve2D curve; // die Kurve
        public Cluster StartCluster; // von hier
        public Cluster EndCluster; // nach da (isStartPoint wieder entfernen)
        public double tmpAngle; // ein Winkel für das sortieren
        public bool forwardUsed; // diese Kante wurde 
        public bool reverseUsed;
        public Joint() { }
        public override string ToString()
        {
            return "Joint: " + curve.ToString();
        }
#if DEBUG
        GeoObjectList Debug
        {
            get
            {
                GeoObjectList res = new GeoObjectList();
                IGeoObject go = curve.MakeGeoObject(Plane.XYPlane);
                (go as IColorDef).ColorDef = new ColorDef("Main", System.Drawing.Color.Black);
                res.Add(go);
                if (StartCluster != null)
                {
                    for (int i = 0; i < StartCluster.Joints.Count; ++i)
                    {
                        go = StartCluster.Joints[i].curve.MakeGeoObject(Plane.XYPlane);
                        (go as IColorDef).ColorDef = new ColorDef("Start", System.Drawing.Color.Green);
                        res.Add(go);
                    }
                }
                if (EndCluster != null)
                {
                    for (int i = 0; i < EndCluster.Joints.Count; ++i)
                    {
                        go = EndCluster.Joints[i].curve.MakeGeoObject(Plane.XYPlane);
                        (go as IColorDef).ColorDef = new ColorDef("Start", System.Drawing.Color.Red);
                        res.Add(go);
                    }
                }
                return res;
            }
        }
#endif
        #region IComparable Members
        // zum Sortieren nach tmpAngle
        public int CompareTo(object obj)
        {
            Joint other = obj as Joint;
            return tmpAngle.CompareTo(other.tmpAngle);
        }

        #endregion
    }

    /// <summary>
    /// Ein oder mehrere Joints, die sehr end beisammenliegen und als identische Punkte
    /// betrachtet werden.
    /// </summary>
    internal class Cluster : IQuadTreeInsertable
    {
        public GeoPoint2D center; // der Mittelpunkt aller zugehörigen Joints
        public List<Joint> Joints; // Liste von Joint[]
        public Cluster()
        {
            Joints = new List<Joint>();
        }
#if DEBUG
        GeoObjectList Debug
        {
            get
            {
                GeoObjectList res = new GeoObjectList();
                for (int i = 0; i < Joints.Count; ++i)
                {
                    IGeoObject go = Joints[i].curve.MakeGeoObject(Plane.XYPlane);
                    (go as IColorDef).ColorDef = new ColorDef("Start", System.Drawing.Color.Green);
                    res.Add(go);
                }
                return res;
            }
        }
#endif
        #region IQuadTreeInsertable Members

        public BoundingRect GetExtent()
        {
            BoundingRect res = BoundingRect.EmptyBoundingRect;
            foreach (Joint lp in Joints)
            {
                GeoPoint2D p;
                if (lp.StartCluster == this) p = lp.curve.StartPoint;
                else p = lp.curve.EndPoint;
                res.MinMax(p);
            }
            return res;
        }

        public bool HitTest(ref BoundingRect Rect, bool IncludeControlPoints)
        {
            foreach (Joint lp in Joints)
            {
                GeoPoint2D p;
                if (lp.StartCluster == this) p = lp.curve.StartPoint;
                else p = lp.curve.EndPoint;
                if (p > Rect) return false;
            }
            return true;
        }
        public object ReferencedObject
        {
            get
            {
                return this;
            }
        }

        #endregion
        public override string ToString()
        {
            string res = "Cluster:\n";
            for (int i = 0; i < Joints.Count; ++i) res += Joints[i].ToString() + "\n";
            return res;
        }
    }

    internal class CurveGraphException : ApplicationException
    {
        public CurveGraphException(string msg) : base(msg)
        {
        }
    }
    /// <summary>
    /// INTERN:
    /// Dient zum Erzeugen von Border und SimpleShape/CompoundShape aus einer Liste von
    /// ICurve2D. Es werden keine Schnittpunkte betrachtet, die müssten zuvor erzeugt und
    /// die ICurve2D Objekte gesplittet werden.
    /// </summary>
    internal class CurveGraph
    {
        private double clusterSize; // maximale Cluster Größe, abhängig von der Ausdehnung alle Objekte
        private double maxGap; // maximale zu schließende Lücke
        private QuadTree clusterTree; // QuadTree aller Cluster
        private UntypedSet clusterSet; // Menge aller Cluster (parallel zum QuadTree)
        /// <summary>
        /// Liste an unbrauchbaren Objekten die bei erstellen eines CompoundShape angefallen sind
        /// </summary>
        public List<IGeoObject> DeadObjects { get; } = new List<IGeoObject>();

        static public CurveGraph CrackCurves(GeoObjectList l, Plane plane, double maxGap)
        {   // alle Kurven in l werden in die Ebene plane projiziert. Das ist mal ein erster Ansatz
            // Man könnte auch gemeinsame Ebenen finden u.s.w.
            ArrayList curves = new ArrayList();
            BoundingRect ext = BoundingRect.EmptyBoundingRect;
            foreach (IGeoObject go in l)
            {
                ICurve cv = go as ICurve;
                if (cv != null)
                {
                    ICurve2D cv2 = cv.GetProjectedCurve(plane);
                    if (cv2 != null)
                    {
                        // "3d" wird nur verwendet um hinterher aus den Originalkurven die Ebene zu bestimmen
                        // in die alles zurücktranformiert wird. Besser würde man vermutlich mit "plane" arbeiten
                        // so wie es hier reinkommt.
                        if (cv2 is Path2D && (cv2 as Path2D).GetSelfIntersections().Length > 0)
                        {   // ein sich selbst überschneidender Pfad muss aufgelöst werden
                            ICurve2D[] sub = (cv2 as Path2D).SubCurves;
                            curves.AddRange(sub);
                            for (int i = 0; i < sub.Length; ++i)
                            {
                                sub[i].UserData.Add("3d", cv);
                            }
                            ext.MinMax(cv2.GetExtent());
                        }
                        else
                        {
                            cv2.UserData.Add("3d", cv);
                            curves.Add(cv2);
                            ext.MinMax(cv2.GetExtent());
                        }
                    }
                }
            }
            if (curves.Count == 0) return null;
            QuadTree qt = new QuadTree(ext);
            qt.MaxDeepth = 8;
            qt.MaxListLen = 3;
            for (int i = 0; i < curves.Count; ++i)
            {
                qt.AddObject(curves[i] as ICurve2D);
            }
            // jetzt alle mit allen schneiden und die Schnipsel in eine weitere Liste stecken
            ArrayList snippet = new ArrayList();
            for (int i = 0; i < curves.Count; ++i)
            {
                ICurve2D cv1 = curves[i] as ICurve2D;
                ArrayList intersectionPoints = new ArrayList(); // double
                ICollection closecurves = qt.GetObjectsCloseTo(cv1);
                foreach (ICurve2D cv2 in closecurves)
                {
                    if (cv2 != cv1)
                    {
                        //if ((cv1 is Line2D && (cv1 as Line2D).Length > 10 && (cv1 as Line2D).Length < 15) ||
                        //    (cv2 is Line2D && (cv2 as Line2D).Length > 10 && (cv2 as Line2D).Length < 15))
                        //{
                        //}
                        GeoPoint2DWithParameter[] isp = cv1.Intersect(cv2);
                        for (int k = 0; k < isp.Length; ++k)
                        {
                            if (cv2.IsParameterOnCurve(isp[k].par2) && 0.0 < isp[k].par1 && isp[k].par1 < 1.0)
                            {
                                intersectionPoints.Add(isp[k].par1);
                            }
                        }
                    }
                }
                if (intersectionPoints.Count == 0)
                {
                    snippet.Add(cv1);
                }
                else
                {
                    intersectionPoints.Add(0.0);
                    intersectionPoints.Add(1.0); // damit sinds mindesten 3
                    double[] pps = (double[])intersectionPoints.ToArray(typeof(double));
                    Array.Sort(pps);
                    for (int ii = 1; ii < pps.Length; ++ii)
                    {
                        if (pps[ii - 1] < pps[ii])
                        {
                            ICurve2D cv3 = cv1.Trim(pps[ii - 1], pps[ii]);
                            if (cv3 != null)
                            {
#if DEBUG
                                GeoPoint2D dbg1 = cv1.PointAt(pps[ii - 1]);
                                GeoPoint2D dbg2 = cv1.PointAt(pps[ii]);
                                GeoPoint2D dbg3 = cv3.StartPoint;
                                GeoPoint2D dbg4 = cv3.EndPoint;
                                double d1 = dbg1 | dbg3;
                                double d2 = dbg2 | dbg4;
#endif
                                cv3.UserData.Add("3d", cv1.UserData.GetData("3d"));
                                snippet.Add(cv3);
                            }
                        }
                    }
                }
            }
            // snippet ist jetzt die Liste aller Schnipsel
            return new CurveGraph((ICurve2D[])snippet.ToArray(typeof(ICurve2D)), maxGap);
        }

        public CurveGraph(ICurve2D[] curves, double maxGap)
        {   // aus den ICurve2D wird eine Clusterliste erzeugt (Start- und Endpunkte)
            this.maxGap = maxGap;
            BoundingRect ext = BoundingRect.EmptyBoundingRect;
            for (int i = 0; i < curves.Length; ++i)
            {
                ext.MinMax(curves[i].GetExtent());
            }
            // clusterSize = (ext.Width + ext.Height) * 1e-8;
            clusterSize = maxGap;
            clusterTree = new QuadTree(ext);
            clusterTree.MaxDeepth = 8;
            clusterTree.MaxListLen = 3;
            clusterSet = new UntypedSet();
            for (int i = 0; i < curves.Length; ++i)
            {
                if (curves[i].Length > clusterSize)
                {
                    Insert(curves[i]);
                }
            }
        }

        private void Insert(ICurve2D curve)
        {   // der Start- bzw. Endpunkt einer Kurve kommt in die Cluster Liste
            Joint lp = new Joint();
            lp.curve = curve;

            GeoPoint2D p = curve.StartPoint;
            BoundingRect CheckSp = new BoundingRect(p, clusterSize, clusterSize);
            ICollection StartCluster = clusterTree.GetObjectsFromRect(CheckSp);
            Cluster InsertInto = null;
            foreach (Cluster cl in StartCluster)
            {
                if (Geometry.Dist(cl.center, p) < clusterSize)
                {
                    InsertInto = cl;
                    clusterTree.RemoveObject(cl); // rausnehmen, da er u.U. größer wird und unten wieder eingefügt wird
                    break;
                }
            }
            if (InsertInto == null)
            {
                InsertInto = new Cluster();
                clusterSet.Add(InsertInto);
            }
            InsertInto.Joints.Add(lp);
            lp.StartCluster = InsertInto;
            double x = 0.0;
            double y = 0.0;
            for (int i = 0; i < InsertInto.Joints.Count; ++i)
            {
                GeoPoint2D pp;
                if ((InsertInto.Joints[i]).StartCluster == InsertInto)
                {
                    pp = (InsertInto.Joints[i]).curve.StartPoint;
                }
                else
                {
                    pp = (InsertInto.Joints[i]).curve.EndPoint;
                }
                x += pp.x;
                y += pp.y;
            }
            InsertInto.center = new GeoPoint2D(x / InsertInto.Joints.Count, y / InsertInto.Joints.Count);
            clusterTree.AddObject(InsertInto);

            // desgleichen mit dem Endpunkt:
            p = curve.EndPoint;
            CheckSp = new BoundingRect(p, clusterSize, clusterSize);
            StartCluster = clusterTree.GetObjectsFromRect(CheckSp);
            InsertInto = null;
            foreach (Cluster cl in StartCluster)
            {
                if (Geometry.Dist(cl.center, p) < clusterSize)
                {
                    InsertInto = cl;
                    clusterTree.RemoveObject(cl); // rausnehmen, da er u.U. größer wird und unten wieder eingefügt wird
                    break;
                }
            }
            if (InsertInto == null)
            {
                InsertInto = new Cluster();
                clusterSet.Add(InsertInto);
            }
            InsertInto.Joints.Add(lp);
            lp.EndCluster = InsertInto;
            x = 0.0;
            y = 0.0;
            for (int i = 0; i < InsertInto.Joints.Count; ++i)
            {
                GeoPoint2D pp;
                if ((InsertInto.Joints[i]).StartCluster == InsertInto)
                {
                    pp = (InsertInto.Joints[i]).curve.StartPoint;
                }
                else
                {
                    pp = (InsertInto.Joints[i]).curve.EndPoint;
                }
                x += pp.x;
                y += pp.y;
            }
            InsertInto.center = new GeoPoint2D(x / InsertInto.Joints.Count, y / InsertInto.Joints.Count);
            clusterTree.AddObject(InsertInto);

        }

        private Cluster FindCluster(ICurve2D curve, GeoPoint2D p, bool RemovePoint)
        {   // Findet einen Cluster, der den Punkt p und die Kurve curve enthält
            BoundingRect clip = new BoundingRect(p, clusterSize, clusterSize);
            ICollection col = clusterTree.GetObjectsFromRect(clip);
            foreach (Cluster cl in col)
            {
                if (cl.HitTest(ref clip, false))
                {
                    for (int i = 0; i < cl.Joints.Count; ++i)
                    {
                        if ((cl.Joints[i]).curve == curve)
                        {
                            if (RemovePoint) cl.Joints.RemoveAt(i);
                            return cl;
                        }
                    }
                }
            }
            return null;
        }
        private void RemoveAllDeadEnds()
        {   // Entfernt alle Sackgassen
            ArrayList ClusterToRemove = new ArrayList(); // damit die Schleife über clusterSet laufen kann
            foreach (Cluster cl in clusterSet)
            {
                if (cl.Joints.Count < 2)
                {
                    ClusterToRemove.Add(cl);
                }
            }
            foreach (Cluster cl in ClusterToRemove) RemoveDeadEnd(cl);
        }
        private void RemoveDeadEnd(Cluster cl)
        {
            Cluster NextCluster = cl;
            while (NextCluster != null && NextCluster.Joints.Count < 2)
            {
                clusterTree.RemoveObject(NextCluster);
                clusterSet.Remove(NextCluster);
                if (NextCluster.Joints.Count > 0)
                {
                    DeadObjects.Add(NextCluster.Joints[0].curve.MakeGeoObject(Plane.XYPlane));

                    Joint lp = NextCluster.Joints[0]; // es gibt ja genau einen
                    if (lp.StartCluster == NextCluster) NextCluster = lp.EndCluster;
                    else NextCluster = lp.StartCluster;
                    if (NextCluster != null)
                    {
                        for (int i = 0; i < NextCluster.Joints.Count; ++i)
                        {
                            if ((NextCluster.Joints[i]) == lp)
                            {
                                NextCluster.Joints.RemoveAt(i);
                                break; // diesen Pfad rausoperiert
                            }
                        }
                    }
                }
                else break;
            }
        }
        //		private void FindCurve(Cluster StartCluster, int PointIndex, Cluster EndCluster, Set UsedClusters, ArrayList result)
        //		{
        //			// der Cluster StartHere hat mehr als zwei Anschlüsse. Gesucht sind sämtliche Pfade, die von StartHere
        //			// nach EndHere führen. Das Ergebnis ist in dem Parameter result zu finden. Dieser
        //			// enthält ein oder mehrerer ArrayLists, wovon jede einzelne eine Abfolge von Cluster und Index ist
        //			ArrayList SingleCurve = new ArrayList();
        //			Cluster LastCluster = StartCluster;
        //			while (PointIndex>=0)
        //			{
        //				SingleCurve.Add(LastCluster);
        //				SingleCurve.Add(PointIndex);
        //				UsedClusters.Add(LastCluster);
        //				Joint lp = (Joint)LastCluster.Points[PointIndex];
        //				Cluster cl;
        //				if (lp.isStartPoint) cl = FindCluster(lp.curve,lp.curve.EndPoint,false);
        //				else cl = FindCluster(lp.curve,lp.curve.StartPoint,false);
        //				if (cl==null) break; // nichts gefunden, kann eigentlich nicht vorkommen
        //				if (cl==EndCluster)
        //				{	// fertig
        //					result.Add(SingleCurve);
        //					break;
        //				}
        //				if (UsedClusters.Contains(cl)) break; // innerer Kurzschluss, führt nicht zum Anfang
        //				if (cl.Points.Count==2)
        //				{	// es geht eindeutig weiter
        //					if (((Joint)cl.Points[0]).curve==lp.curve) PointIndex = 1;
        //					else PointIndex = 0;
        //					LastCluster = cl;
        //					// und weiter gehts
        //				} 
        //				else
        //				{	// es gibt mehr als eine Fortsetzung, denn Cluster mit einem Punkt dürfen nicht vorkommen
        //					// hier werden also verschiedene Fortsetzungen gesucht
        //					for (int i=0; i<cl.Points.Count; ++i)
        //					{
        //						if (((Joint)cl.Points[i]).curve!=lp.curve)
        //						{
        //							ArrayList NextSegment = new ArrayList(); // das neue Ergebnis
        //							FindCurve(cl,i,EndCluster,UsedClusters.Clone(),NextSegment);
        //							for (int j=0; j<NextSegment.Count; ++j)
        //							{
        //								ArrayList NextCurve = new ArrayList(SingleCurve); // immer der selbe Anfang
        //								NextCurve.AddRange((ArrayList)NextSegment[j]); // und verschiedene Enden
        //								result.Add(NextCurve);
        //							}
        //						}
        //					}
        //					break; // fertig
        //				}
        //			}
        //		}
        private void RemoveJoint(Joint lp, Cluster cl)
        {   // entfernt einen Joint aus einem Cluster. Wird der Cluster leer, so wird er ganz entfernt
            clusterTree.RemoveObject(cl);
            cl.Joints.Remove(lp);
            if (cl.Joints.Count > 0) clusterTree.AddObject(cl);
            else clusterSet.Remove(cl);
        }
        private void RemoveCurve(ICurve2D ToRemove)
        {
            ExtractCurve(ToRemove, true);
            ExtractCurve(ToRemove, false);
        }
        private Cluster ExtractCurve(ICurve2D ToRemove, bool RemoveStartPoint)
        {
            GeoPoint2D p;
            if (RemoveStartPoint) p = ToRemove.StartPoint;
            else p = ToRemove.EndPoint;
            BoundingRect tst = new BoundingRect(p, clusterSize, clusterSize);
            ICollection cllist = clusterTree.GetObjectsFromRect(tst);
            Cluster foundCluster = null;
            Joint foundJoint = null;
            foreach (Cluster cl in cllist)
            {
                if (cl.HitTest(ref tst, false))
                {
                    foreach (Joint lp in cl.Joints)
                    {
                        if (lp.curve == ToRemove)
                        {
                            foundCluster = cl;
                            foundJoint = lp;
                            break;
                        }
                    }
                    if (foundCluster != null) break;
                }
            }
            if (foundCluster == null) return null; // sollte nicht vorkommen
            RemoveJoint(foundJoint, foundCluster);
            return foundCluster;
        }
        //		private Border FindNextBorder()
        //		{
        //			// Alle Cluster enthalten zwei Punkte. Suche einen Joint, dessen angeschlossener
        //			// ICurve2D länger als maxGap ist (warum eigentlich?)
        //			// Gehe solange durch die Cluster, bis wieder der erste Punkt erreicht ist
        //			Joint StartWith = null;
        //			foreach (Cluster cl in clusterSet)
        //			{
        //				foreach (Joint lp in cl.Points)
        //				{
        //					if (lp.curve.Length>maxGap)
        //					{
        //						StartWith = lp;
        //						RemoveJoint(lp,cl);
        //						break;
        //					}
        //				}
        //				if (StartWith!=null) break;
        //			}
        //			if (StartWith==null) return null; // keinen Anfang gefunden
        //			Joint LastPoint = StartWith; 
        //			Cluster goon = null;
        //			BorderBuilder makeBorder = new BorderBuilder();
        //			makeBorder.Precision = clusterSize;
        //			while ((goon = ExtractCurve(LastPoint.curve,!LastPoint.isStartPoint))!=null)
        //			{
        //				makeBorder.AddSegment(LastPoint.curve.CloneReverse(!LastPoint.isStartPoint));
        //				if (goon.Points.Count==0) break; // auf den letzten und ersten Punkt gestoßen
        //				LastPoint = (Joint)goon.Points[0]; // es sollte ja nur diesen einen geben
        //				RemoveJoint(LastPoint,goon); // damit müsste dieser Cluster verschwinden
        //			}
        //			return makeBorder.BuildBorder();
        //		}
        //		private bool FindSimpleBorder(Set clusterSet, ArrayList AllBorders, Set UsedJoints, ICurve2D startWith, bool forward)
        //		{
        //			Set tmpUsedJoints = new Set();
        //			tmpUsedJoints.Add(new UsedJoint(startWith,forward));
        //			// Anfangskante gefunden, wie gehts weiter
        //			BorderBuilder bb = new BorderBuilder();
        //			bb.Precision = clusterSize;
        //			if (forward) bb.AddSegment(startWith.Clone());
        //			else bb.AddSegment(startWith.CloneReverse(true));
        //			
        //			while (!bb.IsClosed)
        //			{
        //				Cluster cl = FindCluster(startWith, bb.EndPoint, false);
        //				if (cl==null) return false; // eine angefangene Border geht nicht weiter, sollte nicht passieren, da keine Sackgassen
        //				int ind = -1;
        //				double sa = -1.0;
        //				for (int i=0; i<cl.Points.Count; ++i)
        //				{
        //					Joint j = cl.Points[i] as Joint;
        //					if (j.curve==startWith) continue; // nicht auf der Stelle rückwärts weiter
        //					UsedJoint uj = new UsedJoint(j.curve,j.isStartPoint);
        //					if (!UsedJoints.Contains(uj) && !tmpUsedJoints.Contains(uj))
        //					{
        //						SweepAngle d;
        //						if (j.isStartPoint) d = new SweepAngle(bb.EndDirection,j.curve.StartDirection);
        //						else d = new SweepAngle(bb.EndDirection,j.curve.EndDirection.Opposite());
        //						// d zwischen -PI und +PI
        //						if (d+Math.PI > sa)
        //						{	// je mehr nach links umso größer is d
        //							sa = d+Math.PI;
        //							ind = i;
        //						}
        //					}
        //				}
        //				if (ind>=0)
        //				{
        //					Joint j = cl.Points[ind] as Joint;
        //					if (j.isStartPoint) bb.AddSegment(j.curve.Clone());
        //					else bb.AddSegment(j.curve.CloneReverse(true));
        //					tmpUsedJoints.Add(new UsedJoint(j.curve,j.isStartPoint));
        //					startWith = j.curve;
        //				}
        //				else
        //				{
        //					return false; // kein weitergehen möglich
        //				}
        //			}
        //			if (bb.IsOriented)
        //			{
        //				Border bdr = bb.BuildBorder();
        //				AllBorders.Add(bdr);
        //				foreach (UsedJoint uj in tmpUsedJoints) 
        //				{
        //					if (!UsedJoints.Contains(uj))
        //					{
        //						UsedJoints.Add(uj);
        //					}
        //					else
        //					{
        //						int dbg = 0;
        //					}
        //				}
        //				return true;
        //			}
        //			return false;
        //		}
        //		private bool FindSimpleBorder(Set clusterSet, ArrayList AllBorders, Set UsedJoints)
        //		{
        //			// es wird eine minimale Border gesucht: von irgend einem Cluster ausgehend immer
        //			// linksrum bis man wieder am Anfang ist. 
        //			// UsedJoints enthält UsedJoint objekte, damit man feststellen kann, ob eine Kante bereits
        //			// benutzt ist oder nicht
        //			ICurve2D startWith = null; 
        //			bool forward = false;
        //			foreach (Cluster cl in clusterSet)
        //			{
        //				for (int i=0; i<cl.Points.Count; ++i)
        //				{
        //					UsedJoint uj = new UsedJoint();
        //					Joint j = cl.Points[i] as Joint;
        //					uj.curve = j.curve;
        //					uj.forward = true;
        //					if (!UsedJoints.Contains(uj))
        //					{
        //						forward = j.isStartPoint;
        //						startWith = j.curve;
        //						if (FindSimpleBorder(clusterSet,AllBorders,UsedJoints,startWith,forward))
        //							return true;
        //					}
        //					uj.forward = false;
        //					if (!UsedJoints.Contains(uj))
        //					{
        //						forward = !j.isStartPoint;
        //						startWith = j.curve;
        //						if (FindSimpleBorder(clusterSet,AllBorders,UsedJoints,startWith,forward))
        //							return true;
        //					}
        //				}
        //			}
        //			return false;
        //		}
        private Joint[] SortCluster()
        {   // sortiert die Kanten (Joints) in einem Cluster im Gegenuhrzeigersinn
            // liefert alle Kanten

            // Verwerfen von identischen Kanten:
            // Zwei Kanten in einem Cluster, die das selbe "Gegencluster" haben
            // stehen im Verdacht identisch zu sein. Ihre Mittelpunkte werden auf
            // identität überprüft und die Kanten werden ggf. entfernt.
            foreach (Cluster cl in clusterSet)
            {
                for (int i = 0; i < cl.Joints.Count - 1; ++i)
                {
                    int duplicate = -1;
                    for (int j = i + 1; j < cl.Joints.Count; ++j)
                    {
                        Cluster cl1;
                        Cluster cl2;
                        Joint j1 = cl.Joints[i] as Joint;
                        Joint j2 = cl.Joints[j] as Joint;
                        if (j1.StartCluster == cl) cl1 = j1.EndCluster;
                        else cl1 = j1.StartCluster;
                        if (j2.StartCluster == cl) cl2 = j2.EndCluster;
                        else cl2 = j2.StartCluster;
                        if (cl1 == cl2)
                        {   // zwei Kanten verbinden dieselben Cluster. Sie könnten identisch sein
                            ICurve2D curve1 = j1.curve.CloneReverse(j1.StartCluster != cl);
                            ICurve2D curve2 = j2.curve.CloneReverse(j2.StartCluster != cl);
                            // curve1 und curve2 haben jetzt die selbe Richtung
                            GeoPoint2D p1 = curve1.PointAt(0.5);
                            GeoPoint2D p2 = curve2.PointAt(0.5);
                            if (Geometry.Dist(p1, p2) < clusterSize)
                            {
                                duplicate = j;
                                break;
                            }
                        }
                    }
                    if (duplicate > 0)
                    {
                        cl.Joints.RemoveAt(duplicate);
                    }
                }
            }
            // zu kurze Joints werden entfern
            foreach (Cluster cl in clusterSet)
            {
                for (int i = cl.Joints.Count - 1; i >= 0; --i)
                {
                    Joint j1 = cl.Joints[i] as Joint;
                    if (j1.curve.Length < this.clusterSize)
                    {
                        cl.Joints.RemoveAt(i);
                    }
                }
            }

            UntypedSet allJoints = new UntypedSet();
            foreach (Cluster cl in clusterSet)
            {
                if (cl.Joints.Count < 3)
                {
                    foreach (Joint j in cl.Joints)
                    {
                        if (!allJoints.Contains(j)) allJoints.Add(j);
                    }
                    continue;
                }
                // zwei Punkte im cluster muss man nicht sortieren
                double minDist = double.MaxValue;
                foreach (Joint j in cl.Joints)
                {
                    if (!allJoints.Contains(j)) allJoints.Add(j);
                    GeoPoint2D p;
                    if (j.StartCluster == cl) p = j.EndCluster.center;
                    else
                    {
                        if (j.EndCluster != cl) throw new CurveGraphException("SortCluster");
                        p = j.StartCluster.center;
                    }
                    double d = Geometry.Dist(cl.center, p);
                    if (d == 0.0)
                    {
                        if (j.StartCluster == j.EndCluster)
                        {
                            continue;
                        }
                        throw new CurveGraphException("SortCluster");
                    }
                    if (d < minDist) minDist = d;
                }
                // Kreis um cl mit halber Entfernung zum nächsten Knoten als Radius 
                Circle2D c2d = new Circle2D(cl.center, minDist / 2.0);
                foreach (Joint j in cl.Joints)
                {
                    GeoPoint2DWithParameter[] ip = c2d.Intersect(j.curve);
                    if (ip.Length > 0)
                    {
                        for (int i = 0; i < ip.Length; ++i)
                        {
                            if (j.curve.IsParameterOnCurve(ip[i].par2))
                            {
                                Angle a = new Angle(ip[i].p, cl.center);
                                j.tmpAngle = a.Radian;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // darf nicht vorkommen, eine Kante schneidet nicht den Kreis um
                        // den Knoten mit halbem Radius zum nächsten knoten
                        // der Sortierwert bleibt halt 0.0, aber man sollte solche Kanten 
                        // entfernen ...
                        // kommt vor, das Problem liegt bei regle4!!!
                        if (j.StartCluster == cl)
                        {   // curve startet hier
                            j.tmpAngle = j.curve.StartDirection.Angle;
                        }
                        else
                        {
                            j.tmpAngle = j.curve.EndDirection.Opposite().Angle;
                        }
                    }
                }
                cl.Joints.Sort(); // es wird nach tmpAngle sortiert
            }
            Joint[] res = new Joint[allJoints.Count];
            int ii = 0;
            foreach (Joint j in allJoints)
            {   // die Kurve exakt ausrichten
                try
                {
                    j.curve.StartPoint = j.StartCluster.center;
                    j.curve.EndPoint = j.EndCluster.center;
                }
                catch (Curve2DException) { } // z.B. Kreise endpunkt setzen
                res[ii] = j;
                ++ii;
            }
            return res;
        }
        private BorderBuilder FindBorder(Joint startWith, bool forward)
        {
            BorderBuilder bb = new BorderBuilder();
            bb.Precision = clusterSize;
            if (startWith.curve.Length == 0.0) return null; // sollte nicht vorkommen, kommt aber vor
            bb.AddSegment(startWith.curve.CloneReverse(!forward));
            Cluster cl;
            Cluster startCluster;
            if (forward)
            {
                cl = startWith.EndCluster;
                startCluster = startWith.StartCluster;
                // hier sollte eine Exception geworfen werden wenn schon benutzt!!
                if (startWith.forwardUsed) return null; // schon benutzt, sollte nicht vorkommen
                startWith.forwardUsed = true;
            }
            else
            {
                cl = startWith.StartCluster;
                startCluster = startWith.EndCluster;
                if (startWith.reverseUsed) return null; // schon benutzt, sollte nicht vorkommen
                startWith.reverseUsed = true;
            }
            while (cl != startCluster)
            {
                int ind = -1;
                for (int i = 0; i < cl.Joints.Count; ++i)
                {
                    if (cl.Joints[i] == startWith)
                    {
                        ind = i - 1;
                        if (ind < 0) ind = cl.Joints.Count - 1;
                        break;
                    }
                }
                startWith = cl.Joints[ind] as Joint;
                forward = (startWith.StartCluster == cl);
                if (startWith.curve.Length == 0.0) return null; // sollte nicht vorkommen, kommt aber vor
                bb.AddSegment(startWith.curve.CloneReverse(!forward));
                if (forward)
                {
                    cl = startWith.EndCluster;
                    if (startWith.forwardUsed) return null; // schon benutzt, innere Schleife
                    startWith.forwardUsed = true;
                }
                else
                {
                    cl = startWith.StartCluster;
                    if (startWith.reverseUsed) return null; // schon benutzt, innere Schleife
                    startWith.reverseUsed = true; // auch hier Exception, wenn es schon benutzt war!!
                }
            }
            return bb;
        }
        private Border[] FindAllBorders(Joint[] AllJoints, bool inner)
        {
            ArrayList res = new ArrayList();
            for (int i = 0; i < AllJoints.Length; ++i)
            {
                if (!AllJoints[i].forwardUsed)
                {
                    BorderBuilder bb = FindBorder(AllJoints[i], true);
                    if (bb != null && bb.IsOriented == inner && bb.IsClosed) res.Add(bb.BuildBorder(true));
                }
                if (!AllJoints[i].reverseUsed)
                {
                    BorderBuilder bb = FindBorder(AllJoints[i], false);
                    if (bb != null && bb.IsOriented == inner && bb.IsClosed) res.Add(bb.BuildBorder(true));
                }
            }
            return (Border[])res.ToArray(typeof(Border));
        }
#if DEBUG
        GeoObjectList Debug
        {
            get
            {
                GeoObjectList res = new GeoObjectList();
                Set<ICurve2D> c2d = new Set<ICurve2D>();
                foreach (Cluster cl in clusterSet)
                {
                    GeoPoint p = new GeoPoint(cl.center);
                    Point pnt = Point.Construct();
                    pnt.Location = p;
                    pnt.Symbol = PointSymbol.Circle;
                    if (cl.Joints.Count > 1) pnt.Symbol = PointSymbol.Square;
                    res.Add(pnt);
                    foreach (Joint j in cl.Joints)
                    {
                        c2d.Add(j.curve);
                    }
                }
                foreach (ICurve2D c in c2d)
                {
                    res.Add(c.MakeGeoObject(Plane.XYPlane));
                }
                return res;
            }
        }
        Joint[] DebugJoints;
        GeoObjectList Joints
        {
            get
            {
                GeoObjectList res = new GeoObjectList();
                for (int i = 0; i < DebugJoints.Length; ++i)
                {
                    IGeoObject go = DebugJoints[i].curve.MakeGeoObject(Plane.XYPlane);
                    if (DebugJoints[i].forwardUsed && DebugJoints[i].reverseUsed)
                    {
                        (go as IColorDef).ColorDef = new ColorDef("bothUsed", System.Drawing.Color.Black);
                    }
                    else if (DebugJoints[i].forwardUsed)
                    {
                        (go as IColorDef).ColorDef = new ColorDef("forwardUsed", System.Drawing.Color.Red);
                    }
                    else if (DebugJoints[i].reverseUsed)
                    {
                        (go as IColorDef).ColorDef = new ColorDef("reverseUsed", System.Drawing.Color.Green);
                    }
                    else
                    {
                        (go as IColorDef).ColorDef = new ColorDef("unUsed", System.Drawing.Color.Blue);
                    }
                    res.Add(go);
                }
                return res;
            }

        }
#endif
        public CompoundShape CreateCompoundShape(bool useInnerPoint, GeoPoint2D innerPoint, ConstrHatchInside.HatchMode mode, bool partInPart)
        {
            // 1. Die offenen Enden mit anderen offenen Enden verbinden, wenn Abstand kleiner maxGap.
            // 2. Alle verbleibenden Sackgassen entfernen (alle einzelpunkte sammeln und von dort aus "aufessen".)
            // 3. Es entstehen jetzt zusammenhängende Punktmengen.
            // 4. die Kanten bestimmen und Verweise auf die Cluster setzen (das lässt sich vielleicht
            //    in einen der vorhergehenden Schritte integrieren)
            // 5. In den Clustern die Kanten linksherum sortieren
            //		jetzt haben wir einen (oder mehrere) überschneidungsfreien Graphen, in denen man leicht
            //		durch linksrumgehen die Border findet. Zusätzlich findet man auch noch für jeden
            //		Graphen die Hülle. Die erkennt man daran, dass sie rechtrum geht.
            // 6. Wenn useInnerPoint == false && partInPart == true dann werden auch verschachtelte Teile gesucht und zurückgegeben

            // Lückenschließer einfügen, und zwar die kürzestmöglichen
            // und nur an offenen Enden
            ArrayList CurvesToInsert = new ArrayList();
            foreach (Cluster cl in clusterSet)
            {
                if (cl.Joints.Count < 2)
                {
                    double minDist = maxGap;
                    ICurve2D BestCurve = null;
                    ICollection found = clusterTree.GetObjectsInsideRect(new BoundingRect(cl.center, maxGap, maxGap));
                    foreach (Cluster clnear in found)
                    {
                        if (clnear != cl &&
                            clnear.Joints.Count < 2 &&
                            Geometry.Dist(cl.center, clnear.center) < minDist)
                        {
                            minDist = Geometry.Dist(cl.center, clnear.center);
                            BestCurve = new Line2D(cl.center, clnear.center);
                        }
                    }
                    if (BestCurve != null) CurvesToInsert.Add(BestCurve);
                }
            }
            foreach (ICurve2D curve in CurvesToInsert)
            {
                Insert(curve);
            }

            // hier könnten nun noch auf Wunsch mit ICurve2D.MinDistance zusätzliche Joints
            // eingefügt werden, die als echte Lückenschließer dienen könnten und z.B.
            // auch einen Flaschenhals aus zwei Kreisbögen schließen würden

            // alle Sackgassen entfernen
            RemoveAllDeadEnds();

            Joint[] AllJoints = SortCluster();
#if DEBUG
            DebugJoints = AllJoints;
#endif

            bool inner = (mode != ConstrHatchInside.HatchMode.hull);
            Border[] AllBorders = FindAllBorders(AllJoints, inner);
            Array.Sort(AllBorders, new BorderAreaComparer());
            // der größe nach sortieren, zuerst kommt das kleinste

            if (useInnerPoint)
            {
                int bestBorder = -1;
                if (inner)
                {	// suche die kleinste umgebende Umrandung:
                    for (int i = 0; i < AllBorders.Length; ++i)
                    {
                        if (AllBorders[i].GetPosition(innerPoint) == Border.Position.Inside)
                        {
                            bestBorder = i;
                            break;
                        }
                    }
                }
                else
                {	// suche die größte umgebende Umrandung:
                    // also rückwärts durch das array
                    for (int i = AllBorders.Length - 1; i >= 0; --i)
                    {
                        if (AllBorders[i].GetPosition(innerPoint) == Border.Position.Inside)
                        {
                            bestBorder = i;
                            break;
                        }
                    }
                }
                if (bestBorder >= 0)
                {
                    if (mode == ConstrHatchInside.HatchMode.excludeHoles)
                    {
                        // nur die kleineren Borders betrachten, die größeren können ja keine Löcher sein
                        SimpleShape ss = new SimpleShape(AllBorders[bestBorder]);
                        CompoundShape cs = new CompoundShape(ss);
                        for (int j = 0; j < bestBorder; ++j)
                        {
                            cs.Subtract(new SimpleShape(AllBorders[j]));
                            //BorderOperation bo = new BorderOperation(AllBorders[bestBorder], AllBorders[j]);
                            //if (bo.Position == BorderOperation.BorderPosition.b1coversb2)
                            //{
                            //    holes.Add(AllBorders[j]);
                            //}
                        }
                        return cs;
                    }
                    else
                    {
                        SimpleShape ss = new SimpleShape(AllBorders[bestBorder]);
                        CompoundShape cs = new CompoundShape(ss);
                        return cs;
                    }
                }
            }
            else
            {
                // wenn nicht "useInnerPoint", dann die erste (größte) Border liefern

                if (AllBorders.Length == 0)
                    return null;
                
                if (AllBorders.Length == 1)                    
                    return new CompoundShape(new SimpleShape(AllBorders[0]));                    
                
                //Bei mehr als einer Border
                Array.Reverse(AllBorders); // das größte zuerst
                List<Border> toIterate = new List<Border>(AllBorders);

                if (!partInPart) //Hier werden Teile die in Teilen liegen entfernt
                {
                    CompoundShape res = new CompoundShape();
                    while (toIterate.Count > 0)
                    {
                        SimpleShape ss = new SimpleShape(toIterate[0]);
                        CompoundShape cs = new CompoundShape(ss);
                        // das erste ist der Rand, die folgenden die Löcher
                        for (int i = toIterate.Count - 1; i > 0; --i)
                        {
                            SimpleShape ss1 = new SimpleShape(toIterate[i]);
                            if (SimpleShape.GetPosition(ss, ss1) == SimpleShape.Position.firstcontainscecond)
                            {
                                cs.Subtract(ss1);
                                toIterate.RemoveAt(i);
                            }
                        }
                        toIterate.RemoveAt(0);
                        res = CompoundShape.Union(res, cs);
                    }
                    return res;
                }
                else //Hier werden Teile in Teilen als neues SimpleShape zurückgegeben
                {
                    CompoundShape cs = new CompoundShape(new SimpleShape(toIterate[0]));
                    //Von groß nach klein
                    for (int i = 1; i < toIterate.Count; i++)
                    {
                        SimpleShape innerShape = new SimpleShape(toIterate[i]);

                        //Position des innerShape bestimmen
                        var shapePos = SimpleShape.GetPosition(cs.SimpleShapes[0], innerShape);

                        switch (shapePos)
                        {
                            case SimpleShape.Position.firstcontainscecond:
                                //innerShape aus outerShape ausschneiden weil dieses vollständig innerhalb liegt
                                cs.Subtract(innerShape); 
                                break;
                            case SimpleShape.Position.intersecting:
                                //sollten sich Teile überschneiden werden diese zusammengefügt
                                cs = CompoundShape.Union(cs, new CompoundShape(innerShape)); 
                                break;                                
                            case SimpleShape.Position.disjunct:
                                //die Teile liegen vollständig unabhängig
                                
                                bool shapeHandled = false;
                                //aber vielleicht liegt das Teil innerhalb eines der anderen SimpleShapes?
                                for (int j = 1; j < cs.SimpleShapes.Length; j++)
                                {
                                    var shapePos2 = SimpleShape.GetPosition(cs.SimpleShapes[j], innerShape);
                                    if (shapePos2 == SimpleShape.Position.firstcontainscecond)
                                    {
                                        cs.Subtract(innerShape);
                                        shapeHandled = true;
                                        break;
                                    }
                                    else if (shapePos2 == SimpleShape.Position.intersecting)
                                    {
                                        cs = CompoundShape.Union(cs, new CompoundShape(innerShape));
                                        shapeHandled = true;
                                        break;
                                    }
                                }
                                if (!shapeHandled)
                                    cs.UniteDisjunct(innerShape);
                                break;
                        }
                    }
                    return cs;
                }
            }
            // was sollen wir liefern, wenn nicht useInnerPoint gegeben ist und mehrere Borders gefunden wurden?
            return null;
        }
    }
}
