using CADability.GeoObject;


namespace CADability.Actions
{
    /// <summary>
    /// Construction of an sphere. The construction uses centerpoint and radius. 
    /// </summary>
    internal class Constr3DSphere : ConstructAction
    {
        public Constr3DSphere()
        { }

        private Solid sphere;
        private GeoPoint sphereCenterPoint;
        private double sphereRadius;
        private GeoPointInput centerPointInput;
        private LengthInput radius;



        private void CenterPoint(GeoPoint p)
        {
            sphereCenterPoint = p;
            radius.SetDistanceFromPoint(p);
            sphere = Make3D.MakeSphere(sphereCenterPoint, sphereRadius);
            sphere.CopyAttributes(base.ActiveObject);
            base.ActiveObject = sphere;
        }



        private bool Radius(double length)
        {
            if (length > Precision.eps)
            {
                sphereRadius = length;
                sphere = Make3D.MakeSphere(sphereCenterPoint, sphereRadius);
                if (sphere == null) return false;
                sphere.CopyAttributes(base.ActiveObject);
                base.ActiveObject = sphere;
                return true;
            }
            return false;
        }



        public override void OnSetAction()
        {
            sphere = Solid.Construct();
            base.BasePoint = ConstrDefaults.DefaultStartPoint;
            sphereCenterPoint = base.BasePoint;
            sphereRadius = ConstrDefaults.DefaultArcRadius;
            sphere = Make3D.MakeSphere(sphereCenterPoint, sphereRadius);

            base.ActiveObject = sphere;
            base.TitleId = "Constr.Sphere";

            centerPointInput = new GeoPointInput("Constr.Sphere.Center");
            centerPointInput.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;
            centerPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(CenterPoint);

            radius = new LengthInput("Constr.Sphere.Radius");
            radius.DefaultLength = ConstrDefaults.DefaultArcRadius;
            radius.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(Radius);

            base.SetInput(centerPointInput, radius);
            base.ShowAttributes = true;

            base.OnSetAction();
        }


        public override void OnRemoveAction()
        {
            base.OnRemoveAction();
        }

        public override string GetID()
        {
            return "Constr.Sphere";
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


