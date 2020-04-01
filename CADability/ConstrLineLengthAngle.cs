using CADability.GeoObject;


namespace CADability.Actions
{
    /// <summary>
    /// Construction of a line, specifying a poit, a length and a direction of the 
    /// line. All construct actions are internal, but the base class "ConstructAction"
    /// is public.
    /// </summary>
    internal class ConstrLineLengthAngle : ConstructAction
    {
        /// <summary>
        /// This is the line. It will be manipulated during the construction and finally
        /// added to the model
        /// </summary>
        private Line line;

        /// <summary>
        /// Constructuor, no need to do anything
        /// </summary>
        public ConstrLineLengthAngle()
        { }

        /// <summary>
        /// Overrides OnSetAction of ConstructAction. Here we create the new line
        /// and define the input for the construction
        /// </summary>
        public override void OnSetAction()
        {
            // Create the line and set it some default properties, so that it will
            // appear on the screen
            line = Line.Construct();
            line.StartPoint = ConstrDefaults.DefaultStartPoint;
            GeoPoint p = ConstrDefaults.DefaultStartPoint;
            p.x = p.x + ConstrDefaults.DefaultLineLength;
            line.EndPoint = p;

            // The line will be the active object during the construction. At the end
            // it will be added to the model.
            base.ActiveObject = line;
            // The title appears in the control center
            base.TitleId = "Constr.Line.PointLengthAngle";

            // The first input is a point, which defines the startpoint of the line
            // There is a default value for the startpoint (DefaultStartPoint) which
            // is updated at the end of the construction
            GeoPointInput startPointInput = new GeoPointInput("Line.StartPoint");
            startPointInput.DefaultGeoPoint = ConstrDefaults.DefaultStartPoint;
            // BasePoint is for ortho modus and direction input
            startPointInput.DefinesBasePoint = true;
            // registering a handler to react on the point input
            startPointInput.SetGeoPointEvent += new ConstructAction.GeoPointInput.SetGeoPointDelegate(SetStartPoint);

            // the second input is a length.
            LengthInput len = new LengthInput("Line.Length");
            // the default length
            len.DefaultLength = ConstrDefaults.DefaultLineLength;
            // registering a handler for the change of the length
            len.SetLengthEvent += new ConstructAction.LengthInput.SetLengthDelegate(SetLength);
            // during length-input the mouseinput goes to startPointInput, if it isn´t fixed
            len.ForwardMouseInputTo = startPointInput;

            // the third input is the direction
            GeoVectorInput dir = new GeoVectorInput("Line.Direction");
            dir.DefaultGeoVector = ConstrDefaults.DefaultLineDirection;
            dir.SetGeoVectorEvent += new CADability.Actions.ConstructAction.GeoVectorInput.SetGeoVectorDelegate(SetGeoVector);
            dir.IsAngle = true;
            // during angle-input the mouseinput goes to startPointInput, if it isn´t fixed
            dir.ForwardMouseInputTo = startPointInput;

            // tell the ConstructAction which inputs there are
            base.SetInput(startPointInput, len, dir);


            // the new line gets the default attributes and shows them in the control center
            base.ShowAttributes = true;


            // be sure to call the default implementation
            base.OnSetAction();
        }

        // called by the startPointInput when the user changes the startpoint
        // with the mouse, keyboard or a special construction (like midpoint)
        private void SetStartPoint(GeoPoint p)
        {
            GeoVector v = new GeoVector(line.StartPoint, line.EndPoint);
            line.SetTwoPoints(p, p + v);
        }

        // called by the length input, when the user changes the length
        private bool SetLength(double length)
        {
            if (length > Precision.eps)
            {
                line.Length = length;
                return true;
            }
            return false;
        }

        // called by the VectorInput, when the user changes the direction
        private bool SetGeoVector(GeoVector vector)
        {
            if (Precision.IsNullVector(vector)) return false;
            vector.Norm();
            line.EndPoint = line.StartPoint + line.Length * vector;
            return true;
        }


        /// <summary>
        /// Overrices OnRemoveAction of ConstructAction, when the action is removed.
        /// There is actually no need to override it
        /// </summary>
        public override void OnRemoveAction()
        {
            base.OnRemoveAction();
        }

        /// <summary>
        /// Overrides GetID of Action. Each action mus have an ID. It is usually the
        /// MenuID that starts the action. It must be different from any other action ID.
        /// </summary>
        /// <returns></returns>
        public override string GetID()
        {
            return "Constr.Line.PointLengthAngle";
        }

        /// <summary>
        /// Overrides OnDone of ConstructAction. the defaulStartPoint is updated with
        /// the value of the endpoint of the line (so the next line will start here.
        /// There is no need to update the other default values, this is done
        /// automatically. Make sure to call the bas implementation, because there
        /// the line will be added to the model.
        /// </summary>
        public override void OnDone()
        {
            ConstrDefaults.DefaultStartPoint.Point = line.EndPoint; // wird auf den letzten Endpunkt gesetzt
            base.OnDone();
        }

    }
}
