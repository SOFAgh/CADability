using CADability.GeoObject;
using System;


namespace CADability.Actions
{
    /// <summary>
    /// Construction of an torus. The construction uses centerpoint and radii with torus in drawingplane. 
    /// If optional endpoint is fixed, centerpoint and endpoint define the torus middleaxis (direction in space).
    /// </summary>
    internal class Constr3DTorus : ConstructAction
    {
        public Constr3DTorus()
        { }

        private Solid torus;
        private GeoPoint torusCenterPoint;
        private GeoVector torusNormal;
        private double torusRadius1;
        private double torusRadius2;
        private GeoPointInput centerPointInput;
        private GeoPointInput endPointInput;
        private LengthInput radius1;
        private LengthInput radius2;



        private void CenterPoint(GeoPoint p)
        {
            torusCenterPoint = p;
            torus = Make3D.MakeTorus(torusCenterPoint, torusNormal, torusRadius1, torusRadius2);
            torus.CopyAttributes(base.ActiveObject);
            base.ActiveObject = torus;
        }

        private void EndPoint(GeoPoint p)
        { // definiert die Achse des Torus
            if (!Precision.IsEqual(p, torusCenterPoint))
            {
                torusNormal = new GeoVector(torusCenterPoint, p);
                torus = Make3D.MakeTorus(torusCenterPoint, torusNormal, torusRadius1, torusRadius2);
                torus.CopyAttributes(base.ActiveObject);
                base.ActiveObject = torus;
            }
        }



        private bool Radius1(double length)
        {
            if (length > Precision.eps)
            {  // Ringradius=torusRadius2 angleichen
                if (!radius2.Fixed) torusRadius2 = length / 5.0;
                else
                { // wenn torusRadius2 schon bestimmt: torusRadius1 muss immer kleiner sein!
                    if (torusRadius1 <= torusRadius2) return false;
                }
                torusRadius1 = length;
                torus = Make3D.MakeTorus(torusCenterPoint, torusNormal, torusRadius1, torusRadius2);
                if (torus == null) return false;
                torus.CopyAttributes(base.ActiveObject);
                base.ActiveObject = torus;
                return true;
            }
            return false;
        }

        double GetRadius1()
        {
            return (torusRadius1);
        }


        private double Radius1Calculate(GeoPoint MousePosition)
        {  // falls die Höhe über einen Punkt im Raum über der jetzigen Torus bestimmt wird:
            // komplizierte Berechnung des Radius. Zunächst der Spezialfall Draufsicht auf Torus: 
            double l;
            if (Precision.SameDirection(CurrentMouseView.Projection.Direction, torusNormal, false))
                l = Geometry.DistPL(MousePosition, torusCenterPoint, torusNormal);
            else
            {  // Ebene aus Torusachse und der Senkrechten dieser Achse mit der Projektionsrichtung (Auge-Zeichnung)
                Plane pl = new Plane(torusCenterPoint, torusNormal, CurrentMouseView.Projection.Direction ^ torusNormal);
                // Projektion des Mousepunkts in diese Ebene
                GeoPoint2D tP = pl.Project(MousePosition);
                // gemäß Ebenendefinition ist der y-Wert der gewünschte Radius
                l = Math.Abs(tP.y);
            }
            if (l > Precision.eps)
            {	//  Ringradius=torusRadius2 angleichen
                if (!radius2.Fixed) torusRadius2 = l / 5.0;
                else
                { // wenn torusRadius2 schon bestimmt: torusRadius1 muss immer kleiner sein!
                    if (torusRadius1 <= torusRadius2) return 0;
                }
                // nun die Länge zurückliefern
                return l;
            }
            return 0;
        }

        private bool Radius2(double length)
        {
            if ((length > Precision.eps) && (length < torusRadius1))
            {	// Neue Torus 
                torusRadius2 = length;
                torus = Make3D.MakeTorus(torusCenterPoint, torusNormal, torusRadius1, torusRadius2);
                if (torus == null) return false;
                torus.CopyAttributes(base.ActiveObject);
                base.ActiveObject = torus;
                return true;
            }
            return false;
        }

        double GetRadius2()
        {
            return (torusRadius2);
        }


        private double Radius2Calculate(GeoPoint MousePosition)
        {  // falls die Höhe über einen Punkt im Raum über der jetzigen Torus bestimmt wird:
            // komplizierte Berechnung des Radius. Zunächst der Spezialfall Draufsicht auf Torus: 
            double l;
            if (Precision.SameDirection(CurrentMouseView.Projection.Direction, torusNormal, false))
                l = Geometry.DistPL(MousePosition, torusCenterPoint, torusNormal);
            else
            {  // ebene aus Torusachse und der Senkrechten dieser Achse mit der Projektionsrichtung (Auge-Zeichnung)
                Plane pl = new Plane(torusCenterPoint, torusNormal, CurrentMouseView.Projection.Direction ^ torusNormal);
                // Projektion des Mousepunkts in diese Ebene
                GeoPoint2D tP = pl.Project(MousePosition);
                // gemäß Ebenendefinition ist der y-Wert der gewünschte Radius
                l = Math.Abs(tP.y);
            }
            if ((l > Precision.eps) && (Math.Abs(l - torusRadius1) < torusRadius1))
            {
                // nun die Länge zurückliefern
                return Math.Abs(l - torusRadius1);
            }
            return 0;
        }

        public override void OnSetAction()
        {
            torus = Solid.Construct();
            base.BasePoint = ConstrDefaults.DefaultStartPoint;
            torusCenterPoint = base.BasePoint;
            torusRadius1 = ConstrDefaults.DefaultArcRadius;
            torusRadius2 = ConstrDefaults.DefaultArcRadius / 5;
            torusNormal = base.ActiveDrawingPlane.Normal;
            torus = Make3D.MakeTorus(torusCenterPoint, torusNormal, torusRadius1, torusRadius2);

            base.ActiveObject = torus;
            base.TitleId = "Constr.Torus";

            centerPointInput = new GeoPointInput("Constr.Torus.Center");
            centerPointInput.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;
            centerPointInput.DefinesBasePoint = true;
            centerPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(CenterPoint);

            endPointInput = new GeoPointInput("Constr.Torus.EndPoint");
            endPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(EndPoint);
            endPointInput.Optional = true;

            radius1 = new LengthInput("Constr.Torus.Radius1");
            radius1.DefaultLength = ConstrDefaults.DefaultArcRadius;
            radius1.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Radius1);
            radius1.GetLengthEvent += new LengthInput.GetLengthDelegate(GetRadius1);
            radius1.CalculateLengthEvent += new CADability.Actions.ConstructAction.LengthInput.CalculateLengthDelegate(Radius1Calculate);


            radius2 = new LengthInput("Constr.Torus.Radius2");
            radius2.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Radius2);
            radius2.GetLengthEvent += new LengthInput.GetLengthDelegate(GetRadius2);
            radius2.CalculateLengthEvent += new CADability.Actions.ConstructAction.LengthInput.CalculateLengthDelegate(Radius2Calculate);

            base.SetInput(centerPointInput, endPointInput, radius1, radius2);
            base.ShowAttributes = true;

            base.OnSetAction();
        }


        public override void OnRemoveAction()
        {
            base.OnRemoveAction();
        }

        public override string GetID()
        {
            return "Constr.Torus";
        }

        public override void OnDone()
        {
            if (base.ActiveObject != null)
            {
                using (base.Frame.Project.Undo.UndoFrame)
                {
                    KeepAsLastStyle(ActiveObject);
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


