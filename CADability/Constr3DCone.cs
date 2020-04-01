
using CADability.GeoObject;
using System;


namespace CADability.Actions
{
    /// <summary>
    /// Construction of an cone. the base construction uses startpoint, endpoint and radii. Startpoint and endpoint define the cone middleaxis (direction in space and height).
    /// You can specify the height before as distance from startpoint.
    /// If you specify radius instead of endpoint, you get a cone standing upright on active drawing plane.
    /// </summary>
    internal class Constr3DCone : ConstructAction
    {
        public Constr3DCone()
        { }

        private Solid cone;
        private GeoPoint coneStartPoint;
        private GeoVector coneDirX;
        private GeoVector coneDirZ;
        private double coneRadius1;
        private double coneRadius2;
        private double coneHeight;
        private GeoPointInput startPointInput;
        private GeoPointInput endPointInput;
        private LengthInput radius1;
        private LengthInput radius2;
        private LengthInput height;



        private void StartPoint(GeoPoint p)
        {
            coneStartPoint = p;
            // die Höhe berechnet sich als Abstand von diesem Punkt
            height.SetDistanceFromPoint(coneStartPoint);
            cone = Make3D.MakeCone(coneStartPoint, coneDirX, coneDirZ, coneRadius1, coneRadius2);
            cone.CopyAttributes(base.ActiveObject);
            base.ActiveObject = cone;
        }

        private void EndPoint(GeoPoint p)
        {
            if (!Precision.IsEqual(p, coneStartPoint))
            {
                coneDirZ = new GeoVector(coneStartPoint, p);
                if (height.Fixed) // die schon bestimmte Höhe benutzen!
                    coneDirZ = coneHeight * coneDirZ.Normalized;
                else coneHeight = coneDirZ.Length;
                // coneDirX muss irgendwie senkrecht auf coneDirZ stehen. Hier: Hilfsvektor definieren mit der kleinsten Komponente von coneDirZ
                GeoVector vT = new GeoVector(1, 0, 0); // x am kleinsten
                if (Math.Abs(coneDirZ.y) < Math.Abs(coneDirZ.x))
                    vT = new GeoVector(0, 1, 0); // y am kleinsten
                if ((Math.Abs(coneDirZ.x) > Math.Abs(coneDirZ.z)) && (Math.Abs(coneDirZ.y) > Math.Abs(coneDirZ.z)))
                    vT = new GeoVector(0, 0, 1); // z am kleinsten
                coneDirX = coneRadius1 * (vT ^ coneDirZ).Normalized;
                cone = Make3D.MakeCone(coneStartPoint, coneDirX, coneDirZ, coneRadius1, coneRadius2);
                cone.CopyAttributes(base.ActiveObject);
                base.ActiveObject = cone;
            }
        }



        private bool Radius1(double length)
        {
            if (length > Precision.eps)
            {
                coneRadius1 = length;
                coneDirX = coneRadius1 * coneDirX.Normalized;
                cone = Make3D.MakeCone(coneStartPoint, coneDirX, coneDirZ, coneRadius1, coneRadius2);
                if (cone == null) return false;
                cone.CopyAttributes(base.ActiveObject);
                if (startPointInput.Fixed && !endPointInput.Fixed)
                { // er will also einen Kegel senkrecht auf der drawingplane
                    endPointInput.Optional = true;
                    height.Optional = false;
                    height.ForwardMouseInputTo = new object[0]; // den forward abschalten
                    coneDirX = coneRadius1 * base.ActiveDrawingPlane.DirectionX;
                    coneDirZ = coneHeight * (base.ActiveDrawingPlane.DirectionX ^ base.ActiveDrawingPlane.DirectionY);
                }
                base.ActiveObject = cone;
                return true;
            }
            return false;
        }

        double GetRadius1()
        {
            return (coneRadius1);
        }


        private double Radius1Calculate(GeoPoint MousePosition)
        {  // falls die Höhe über einen Punkt im Raum über der jetzigen Cone bestimmt wird:
            // komplizierte Berechnung des Radius. Zunächst der Spezialfall Draufsicht auf Kegel: 
            double l;
            if (Precision.SameDirection(CurrentMouseView.Projection.Direction, coneDirZ, false))
                l = Geometry.DistPL(MousePosition, coneStartPoint, coneDirZ);
            else
            {  // Ebene aus Kegelachse und der Senkrechten dieser Achse mit der Projektionsrichtung (Auge-Zeichnung)
                Plane pl = new Plane(coneStartPoint, coneDirZ, CurrentMouseView.Projection.Direction ^ coneDirZ);
                // Projektion des Mousepunkts in diese Ebene
                GeoPoint2D tP = pl.Project(MousePosition);
                // gemäß Ebenendefinition ist der y-Wert der gewünschte Radius
                l = Math.Abs(tP.y);
            }
            if (l > Precision.eps)
            {
                // nun die Länge zurückliefern
                return l;
            }
            return 0;
        }

        private bool Radius2(double length)
        {
            if (length >= 0.0)
            {
                if (length <= Precision.eps) length = 0.0;
                coneRadius2 = length;
                cone = Make3D.MakeCone(coneStartPoint, coneDirX, coneDirZ, coneRadius1, coneRadius2);
                if (cone == null) return false;
                cone.CopyAttributes(base.ActiveObject);
                if (startPointInput.Fixed && !endPointInput.Fixed)
                { // er will also einen Kegel senkrecht auf der drawingplane
                    endPointInput.Optional = true;
                    height.Optional = false;
                    height.ForwardMouseInputTo = new object[0];  // den forward abschalten
                    coneDirX = coneRadius1 * base.ActiveDrawingPlane.DirectionX;
                    coneDirZ = coneHeight * (base.ActiveDrawingPlane.DirectionX ^ base.ActiveDrawingPlane.DirectionY);
                }
                base.ActiveObject = cone;
                return true;
            }
            return false;
        }

        double GetRadius2()
        {
            return (coneRadius2);
        }


        private double Radius2Calculate(GeoPoint MousePosition)
        {  // falls die Höhe über einen Punkt im Raum über der jetzigen Cone bestimmt wird:
            // komplizierte Berechnung des Radius. Zunächst der Spezialfall Draufsicht auf Kegel: 
            double l;
            if (Precision.SameDirection(CurrentMouseView.Projection.Direction, coneDirZ, false))
                l = Geometry.DistPL(MousePosition, coneStartPoint, coneDirZ);
            else
            {  // ebene aus Kegelachse und der Senkrechten dieser Achse mit der Projektionsrichtung (Auge-Zeichnung)
                Plane pl = new Plane(coneStartPoint, coneDirZ, CurrentMouseView.Projection.Direction ^ coneDirZ);
                // Projektion des Mousepunkts in diese Ebene
                GeoPoint2D tP = pl.Project(MousePosition);
                // gemäß Ebenendefinition ist der y-Wert der gewünschte Radius
                l = Math.Abs(tP.y);
            }
            if (l >= 0.0)
            {
                if (l <= Precision.eps) l = 0.0;
                // nun die Länge zurückliefern
                return l;
            }
            return 0;
        }


        private bool Height(double length)
        {
            if (length > Precision.eps)
            {
                coneHeight = length;
                coneDirZ = coneHeight * coneDirZ.Normalized;
                cone = Make3D.MakeCone(coneStartPoint, coneDirX, coneDirZ, coneRadius1, coneRadius2);
                if (cone == null) return false;
                cone.CopyAttributes(base.ActiveObject);
                base.ActiveObject = cone;
                return true;
            }
            return false;
        }

        double GetHeight()
        {
            return (coneHeight);
        }


        public override void OnSetAction()
        {
            cone = Solid.Construct();
            base.BasePoint = ConstrDefaults.DefaultStartPoint;
            coneStartPoint = base.BasePoint;
            coneRadius1 = ConstrDefaults.DefaultArcRadius;
            coneRadius2 = ConstrDefaults.DefaultArcRadius / 2;
            coneHeight = ConstrDefaults.DefaultBoxHeight;
            coneDirX = coneRadius1 * base.ActiveDrawingPlane.DirectionX;
            coneDirZ = coneHeight * (base.ActiveDrawingPlane.DirectionX ^ base.ActiveDrawingPlane.DirectionY);
            cone = Make3D.MakeCone(coneStartPoint, coneDirX, coneDirZ, coneRadius1, coneRadius2);

            base.ActiveObject = cone;
            base.TitleId = "Constr.Cone";

            startPointInput = new GeoPointInput("Constr.Cone.StartPoint");
            startPointInput.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;
            startPointInput.DefinesBasePoint = true;
            startPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(StartPoint);

            endPointInput = new GeoPointInput("Constr.Cone.EndPoint");
            //			endPointInput.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;
            endPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(EndPoint);

            radius1 = new LengthInput("Constr.Cone.Radius1");
            radius1.DefaultLength = ConstrDefaults.DefaultArcRadius;
            radius1.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Radius1);
            radius1.GetLengthEvent += new LengthInput.GetLengthDelegate(GetRadius1);
            radius1.CalculateLengthEvent += new CADability.Actions.ConstructAction.LengthInput.CalculateLengthDelegate(Radius1Calculate);


            radius2 = new LengthInput("Constr.Cone.Radius2");
            radius2.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Radius2);
            radius2.GetLengthEvent += new LengthInput.GetLengthDelegate(GetRadius2);
            radius2.CalculateLengthEvent += new CADability.Actions.ConstructAction.LengthInput.CalculateLengthDelegate(Radius2Calculate);

            height = new LengthInput("Constr.Cone.Height");
            height.DefaultLength = ConstrDefaults.DefaultBoxHeight;
            height.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Height);
            height.GetLengthEvent += new LengthInput.GetLengthDelegate(GetHeight);
            height.Optional = true;
            height.ForwardMouseInputTo = endPointInput;

            base.SetInput(startPointInput, endPointInput, radius1, radius2, height);
            base.ShowAttributes = true;

            base.OnSetAction();
        }


        public override void OnRemoveAction()
        {
            base.OnRemoveAction();
        }

        public override string GetID()
        {
            return "Constr.Cone";
        }

        public override void OnDone()
        {
            ConstrDefaults.DefaultStartPoint.Point = coneStartPoint + coneDirZ;
            // wird auf den oberen Mittelpunkt gesetzt
            if (base.ActiveObject != null)
            {
                KeepAsLastStyle(ActiveObject);
                using (base.Frame.Project.Undo.UndoFrame)
                {
                    if (Frame.SolidModellingMode == Make3D.SolidModellingMode.unite)
                    { // vereinigen, wenn möglich
                        base.Frame.Project.GetActiveModel().UniteSolid(base.ActiveObject as Solid, false);
                        base.ActiveObject = null;
                    }
                    if (Frame.SolidModellingMode == Make3D.SolidModellingMode.subtract)
                    { // abziehen, wenn möglich, sonst: nichts tun
                        base.Frame.Project.GetActiveModel().RemoveSolid(base.ActiveObject as Solid, false);
                        base.ActiveObject = null;
                    }
                }
            }
            base.OnDone();
        }

    }
}

