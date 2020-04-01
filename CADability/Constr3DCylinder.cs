using CADability.GeoObject;
using System;


namespace CADability.Actions
{
    /// <summary>
    /// Construction of an cylinder. the base construction uses startpoint, endpoint and radius. Startpoint and endpoint define the cylinder middleaxis (direction in space and height).
    /// You can specify the height before as distance from startpoint.
    /// If you specify radius instead of endpoint, you get a cylinder standing on active drawing plane.
    /// </summary>
    internal class Constr3DCylinder : ConstructAction
    {
        public Constr3DCylinder()
        { }

        private Solid cylinder;
        private GeoPoint cylinderStartPoint;
        private GeoVector cylinderDirX;
        private GeoVector cylinderDirZ;
        private double cylinderRadius;
        private double cylinderHeight;
        private GeoPointInput startPointInput;
        private GeoPointInput endPointInput;
        private LengthInput radius;
        private LengthInput height;



        private void StartPoint(GeoPoint p)
        {
            cylinderStartPoint = p;
            // die Höhe berechnet sich als Abstand von diesem Punkt
            height.SetDistanceFromPoint(cylinderStartPoint);
            cylinder = Make3D.MakeCylinder(cylinderStartPoint, cylinderDirX, cylinderDirZ);
            cylinder.CopyAttributes(base.ActiveObject);
            base.ActiveObject = cylinder;
        }

        private void EndPoint(GeoPoint p)
        {
            if (!Precision.IsEqual(p, cylinderStartPoint))
            {
                cylinderDirZ = new GeoVector(cylinderStartPoint, p);
                if (height.Fixed) // die schon bestimmt Höhe benutzen!
                    cylinderDirZ = cylinderHeight * cylinderDirZ.Normalized;
                else cylinderHeight = cylinderDirZ.Length;
                // cylinderDirX muss irgendwie senkrecht auf cylinderDirZ stehen. Hier: Hilfsvektor definieren mit der kleinsten Komponente von cylinderDirZ
                GeoVector vT = new GeoVector(1, 0, 0); // x am kleinsten
                if (Math.Abs(cylinderDirZ.y) < Math.Abs(cylinderDirZ.x))
                    vT = new GeoVector(0, 1, 0); // y am kleinsten
                if ((Math.Abs(cylinderDirZ.x) > Math.Abs(cylinderDirZ.z)) && (Math.Abs(cylinderDirZ.y) > Math.Abs(cylinderDirZ.z)))
                    vT = new GeoVector(0, 0, 1); // z am kleinsten
                cylinderDirX = cylinderRadius * (vT ^ cylinderDirZ).Normalized;
                cylinder = Make3D.MakeCylinder(cylinderStartPoint, cylinderDirX, cylinderDirZ);
                cylinder.CopyAttributes(base.ActiveObject);
                base.ActiveObject = cylinder;
            }
        }

        private bool Radius(double length)
        {
            if (length > Precision.eps)
            {
                cylinderRadius = length;
                cylinderDirX = cylinderRadius * cylinderDirX.Normalized;
                cylinder = Make3D.MakeCylinder(cylinderStartPoint, cylinderDirX, cylinderDirZ);
                cylinder.CopyAttributes(base.ActiveObject);
                if (startPointInput.Fixed && !endPointInput.Fixed)
                { // er will also einen Zylinder senkrecht auf der drawingplane
                    endPointInput.Optional = true;
                    height.Optional = false;
                    height.ForwardMouseInputTo = new object[0]; // den forward abschalten
                    cylinderDirX = cylinderRadius * base.ActiveDrawingPlane.DirectionX;
                    cylinderDirZ = cylinderHeight * (base.ActiveDrawingPlane.DirectionX ^ base.ActiveDrawingPlane.DirectionY);
                }
                base.ActiveObject = cylinder;
                return true;
            }
            return false;
        }

        double GetRadius()
        {
            return (cylinderRadius);
        }

        private double RadiusCalculate(GeoPoint MousePosition)
        {  // falls die Höhe über einen Punkt im Raum über der jetzigen Cylinder bestimmt wird:
            // komplizierte Berechnung des Radius. Zunächst der Spezialfall Draufsicht auf Zylinder: 
            double l;
            if (Precision.SameDirection(CurrentMouseView.Projection.Direction, cylinderDirZ, false))
                l = Geometry.DistPL(MousePosition, cylinderStartPoint, cylinderDirZ);
            else
            {  // ebene aus Zylinderachse und der Senkrechten dieser Achse mit der Projektionsrichtung (Auge-Zeichnung)
                Plane pl = new Plane(cylinderStartPoint, cylinderDirZ, CurrentMouseView.Projection.Direction ^ cylinderDirZ);
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



        private bool Height(double length)
        {
            if (length > Precision.eps)
            {
                cylinderHeight = length;
                cylinderDirZ = cylinderHeight * cylinderDirZ.Normalized;
                cylinder = Make3D.MakeCylinder(cylinderStartPoint, cylinderDirX, cylinderDirZ);
                cylinder.CopyAttributes(base.ActiveObject);
                base.ActiveObject = cylinder;
                return true;
            }
            return false;
        }

        double GetHeight()
        {
            return (cylinderHeight);
        }


        public override void OnSetAction()
        {
            cylinder = Solid.Construct();
            base.BasePoint = ConstrDefaults.DefaultStartPoint;
            cylinderStartPoint = base.BasePoint;
            cylinderRadius = ConstrDefaults.DefaultArcRadius;
            cylinderHeight = ConstrDefaults.DefaultBoxHeight;
            cylinderDirX = cylinderRadius * base.ActiveDrawingPlane.DirectionX;
            cylinderDirZ = cylinderHeight * (base.ActiveDrawingPlane.DirectionX ^ base.ActiveDrawingPlane.DirectionY);
            cylinder = Make3D.MakeCylinder(ConstrDefaults.DefaultStartPoint, cylinderDirX, cylinderDirZ);

            base.ActiveObject = cylinder;
            base.TitleId = "Constr.Cylinder";

            startPointInput = new GeoPointInput("Constr.Cylinder.StartPoint");
            startPointInput.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;
            startPointInput.DefinesBasePoint = true;
            startPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(StartPoint);

            endPointInput = new GeoPointInput("Constr.Cylinder.EndPoint");
            endPointInput.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;
            endPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(EndPoint);

            radius = new LengthInput("Constr.Cylinder.Radius");
            radius.DefaultLength = ConstrDefaults.DefaultArcRadius;
            radius.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Radius);
            radius.GetLengthEvent += new LengthInput.GetLengthDelegate(GetRadius);
            radius.CalculateLengthEvent += new CADability.Actions.ConstructAction.LengthInput.CalculateLengthDelegate(RadiusCalculate);

            height = new LengthInput("Constr.Cylinder.Height");
            height.DefaultLength = ConstrDefaults.DefaultBoxHeight;
            height.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Height);
            height.GetLengthEvent += new LengthInput.GetLengthDelegate(GetHeight);
            height.ForwardMouseInputTo = endPointInput;
            height.Optional = true;

            base.SetInput(startPointInput, endPointInput, radius, height);
            base.ShowAttributes = true;

            base.OnSetAction();
        }


        public override void OnRemoveAction()
        {
            base.OnRemoveAction();
        }

        public override string GetID()
        {
            return "Constr.Cylinder";
        }

        public override void OnDone()
        {
            ConstrDefaults.DefaultStartPoint.Point = cylinderStartPoint + cylinderDirZ;
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

