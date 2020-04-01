using CADability.GeoObject;
using CADability.Shapes;
using System;
using System.Collections;
using System.Collections.Generic;



namespace CADability.Actions
{
    internal class Constr3DFaceRotate : ConstructAction
    {
        private GeoObjectInput geoObjectInput;
        private GeoPointInput innerPointInput;
        private CurveInput rotateLineInput;
        private GeoVectorInput axisVectorInput;
        private GeoPointInput axisPointInput;
        private GeoPoint objectPoint;
        //        private IGeoObject iGeoObjectSel;
        //        private IGeoObject iGeoObjectOrg;
        //        private IGeoObjectOwner owner;
        //        private Path pathCreatedFromModel;
        static private GeoVector axisVector;
        static private GeoPoint axisPoint;
        static private Boolean axisOrLine;
        private Boolean selectedMode;
        static private Angle angleRotation;
        static private Angle angleOffsetRotation;
        private GeoObjectList selectedObjectsList; // die Liste der selektierten Objekte
        // die folgenden Listen werden dazu synchron gefüllt mit Werten oder "null", bei OnDone zum Einfügen bzw Löschen ausgewertet.
        private List<IGeoObject> geoObjectOrgList; // Liste der beteiligten Originalobjekte, bei OnDone zum Löschen wichtig
        private List<IGeoObject> shapeList; // Die Liste der entstandenen Shapes, bei OnDone zum Einfügen gebraucht
        private List<Path> pathCreatedFromModelList; // Liste der evtl. syntetisch erzeugten Pfade zusammenhängender Objekte
        private List<IGeoObjectOwner> ownerList; // List der Owner der Objekte, wichti bei OnDone
        private Block blk; // da werden die Objekte drinn gesammelt
        private bool innerPointUp;

        public static bool faceTestRotate(GeoObjectList selectedObjects)
        {
            for (int i = 0; i < selectedObjects.Count; i++)
            {   // nur eins muss passen
                if ((selectedObjects[i] is Face) || (selectedObjects[i] is Shell) || ((selectedObjects[i] is ICurve) && (selectedObjects[i] as ICurve).IsClosed)
                     || ((selectedObjects[i] is Ellipse) && (selectedObjects[i] as Ellipse).IsArc) || (selectedObjects[i] is Path) || (selectedObjects[i] is Polyline)) return true;
            }
            return false;
        }



        private Constr3DFaceRotate()
        { // wird über ":this" von den beiden nächsten Constructoren aufgerufen, initialisiert die Listen
            selectedObjectsList = new GeoObjectList();
            geoObjectOrgList = new List<IGeoObject>();
            shapeList = new List<IGeoObject>();
            ownerList = new List<IGeoObjectOwner>();
            pathCreatedFromModelList = new List<Path>();
        }


        public Constr3DFaceRotate(GeoObjectList selectedObjectsList) : this()
        {
            selectedMode = (selectedObjectsList != null);
            if (selectedMode)
            {
                this.selectedObjectsList = selectedObjectsList.Clone();
                ListDefault(selectedObjectsList.Count); // setzt alle Listen auf gleiche Länge, Inhalte "null"
            };
        }

        public Constr3DFaceRotate(Constr3DFaceRotate autorepeat) : this()
        {
            if (autorepeat.selectedMode) throw new ApplicationException();
            selectedMode = false;
        }

        private void ListDefault(int number)
        { // setzt alle Listen auf gleiche Länge, Inhalte "null"
            geoObjectOrgList.Clear();
            shapeList.Clear();
            ownerList.Clear();
            pathCreatedFromModelList.Clear();
            for (int i = 0; i < number; i++)
            {
                geoObjectOrgList.Add(null);
                shapeList.Add(null);
                ownerList.Add(null);
                pathCreatedFromModelList.Add(null);
            }
        }


        void optionalOrg()
        {
            axisPointInput.Optional = !axisOrLine;
            axisVectorInput.Optional = !axisOrLine;
            rotateLineInput.Optional = axisOrLine;
        }


        private bool rotateOrg(bool openDraw)
        {
            if (selectedObjectsList == null) return false;
            IGeoObject iGeoObjectSel;
            //         IGeoObject iGeoObjectOrg;
            blk = Block.Construct(); // zur Darstellung
            if (base.ActiveObject != null) blk.CopyAttributes(base.ActiveObject);
            // der block wird das neue aktive Objekt, er muss die Attribute tragen, weil sie später
            // wieder von ihm verlangt werden
            Boolean success = false; // hat er wenigstens eins gefunden
            GeoPoint axisPointSav = axisPoint;
            GeoVector axisVectorSav = axisVector;
            optionalOrg(); // erstmal aufrufen, wird unten im offen-Fall überschrieben
            for (int i = 0; i < selectedObjectsList.Count; i++) // läuft über alle selektierten Objekte. Evtl nur eins bei Konstruktion
            {
                IGeoObject iGeoObjectTemp = null; // lokaler Merker
                iGeoObjectSel = selectedObjectsList[i]; // zur Vereinfachung
                geoObjectOrgList[i] = iGeoObjectSel; // zum Weglöschen des Originals in onDone
                ownerList[i] = iGeoObjectSel.Owner; // owner merken für löschen 
                pathCreatedFromModelList[i] = null;
                shapeList[i] = null;

                if ((iGeoObjectSel is Face) || (iGeoObjectSel is Shell))
                { // nur kopieren
                    iGeoObjectTemp = iGeoObjectSel.Clone();
                }
                else
                {
                    if (iGeoObjectSel is ICurve)
                    {
                        Path p = null;
                        p = CADability.GeoObject.Path.CreateFromModel(iGeoObjectSel as ICurve, Frame.ActiveView.Model, Frame.ActiveView.Projection, true);
                        if (p == null)
                        { // also nur Einzelelement
                            if (iGeoObjectSel is Path)
                            { // schon fertig
                                p = iGeoObjectSel.Clone() as Path;
                            }
                            else
                            {  // Pfad aus Einzelobjekt machen:
                                p = Path.Construct();
                                p.Set(new ICurve[] { iGeoObjectSel.Clone() as ICurve });
                            }
                        }
                        else
                        { // CreateFromModel hat was zusammengesetzt
                          //                            if (p.IsClosed)
                            {
                                geoObjectOrgList[i] = null; // zum nicht Weglöschen des Originals in onDone
                                pathCreatedFromModelList[i] = p; // kopie merken für onDone
                                p = p.Clone() as Path;
                                p.Flatten();
                            }
                        }
                        if (p.IsClosed)
                        { // jetzt face machen
                            iGeoObjectTemp = Make3D.MakeFace(p, Frame.Project, true);
                        }
                        else
                        { // Pfad ist offen!
                            Line l = Line.Construct();
                            if (rotateLineInput.Fixed || axisVectorInput.Fixed || axisPointInput.Fixed || openDraw)
                            { // die Achse ist auf irgendeine Art schon bestimmt (.Fixed) oder wird es gerade (openDraw = true)
                                l.SetTwoPoints(axisPoint, axisPoint + axisVector);
                            }
                            else
                            { // Achse noch nicht bestimmt: Hier festlegen
                                l.SetTwoPoints(p.EndPoint, p.StartPoint);
                            }
                            if ((p.GetPlanarState() == PlanarState.Planar) && (Precision.IsPointOnPlane(l.StartPoint, p.GetPlane())) && (Precision.IsPointOnPlane(l.EndPoint, p.GetPlane())))
                            { // pfad eben und in selber Ebene wie Drehachse
                                if (rotateLineInput.Fixed || axisVectorInput.Fixed || axisPointInput.Fixed || openDraw)
                                { // die Achse ist auf irgendeine Art schon bestimmt (.Fixed) oder wird es gerade (openDraw = true)
                                    GeoPoint lotEnd = Geometry.DropPL(p.EndPoint, axisPoint, axisVector); // Lotpunkt Endpunkt auf Drehachse
                                    GeoPoint lotStart = Geometry.DropPL(p.StartPoint, axisPoint, axisVector); // Lotpunkt Startpunkt auf Drehachse
                                    l.SetTwoPoints(p.EndPoint, lotEnd); // Linie generieren und zufügen
                                    if (!Precision.IsNull(l.Length)) p.Add(l.Clone() as ICurve);
                                    l.SetTwoPoints(lotEnd, lotStart);
                                    if (!Precision.IsNull(l.Length)) p.Add(l.Clone() as ICurve);
                                    l.SetTwoPoints(lotStart, p.StartPoint);
                                    if (!Precision.IsNull(l.Length)) p.Add(l.Clone() as ICurve);
                                }
                                else
                                { // alles optional
                                    axisPointInput.Optional = true;
                                    axisVectorInput.Optional = true;
                                    rotateLineInput.Optional = true;
                                    axisPoint = p.StartPoint; // Drehachse bestimmen
                                    axisVector = new GeoVector(p.StartPoint, p.EndPoint);
                                    l.SetTwoPoints(p.EndPoint, p.StartPoint); // Pfad schliessen
                                    p.Add(l);
                                }
                                if (p.IsClosed) iGeoObjectTemp = Make3D.MakeFace(p, Frame.Project, true);
                            }
                        }
                    }
                }
                if (iGeoObjectTemp != null)
                { // also was geeignetes dabei
                    //if (angleOffsetRotation != 0.0)
                    //{
                    //    ModOp m = ModOp.Rotate(axisPoint, axisVector, new SweepAngle(angleOffsetRotation));
                    //    iGeoObjectTemp.Modify(m);
                    //}
                    double sw = angleRotation;
                    if (sw == 0.0) sw = Math.PI * 2.0;
                    // IGeoObject shape = Make3D.MakeRevolution(iGeoObjectTemp, axisPoint, axisVector, sw, Frame.Project);
                    IGeoObject shape = Make3D.Rotate(iGeoObjectTemp,new Axis(axisPoint, axisVector), sw, angleOffsetRotation, Frame.Project);
                    if (shape != null)
                    {
                        //                        shape.CopyAttributes(iGeoObjectSel as IGeoObject);
                        shape.CopyAttributes(blk);
                        // die Attribute müssen vom Block übernommen werden
                        shapeList[i] = shape; // fertiger Körper in shapeList
                        blk.Add(shape); // zum Darstellen
                        base.FeedBack.AddSelected(iGeoObjectTemp); // zum Markieren des Ursprungsobjekts
                        success = true;
                    }
                }
            }
            axisPoint = axisPointSav; // im Fall nicht geschlossener Pfad und nichts bestimmt, werden diese Parameter oben verstellt
            axisVector = axisVectorSav;
            if (success)
            {
                base.ActiveObject = blk; // darstellen
                base.ShowActiveObject = true;
                return true;
            }
            else
            {
                optionalOrg();
                base.ShowActiveObject = false;
                base.FeedBack.ClearSelected();
                return false;
            }
        }


        bool geoObjectInputFace(ConstructAction.GeoObjectInput sender, IGeoObject[] TheGeoObjects, bool up)
        {	// ... nur die sinnvollen Kurven verwenden
            objectPoint = base.CurrentMousePosition;
            if (up)
                if (TheGeoObjects.Length == 0) sender.SetGeoObject(TheGeoObjects, null); // ...die werden jetzt im ControlCenter dargestellt (nur bei up)
                else sender.SetGeoObject(TheGeoObjects, TheGeoObjects[0]);
            if (TheGeoObjects.Length > 0)
            {	// er hat was gewählt
                selectedObjectsList.Clear();
                base.FeedBack.ClearSelected();
                selectedObjectsList.Add(TheGeoObjects[0]); // das eine in die Liste
                ListDefault(1); // die Listen löschen und mit "1" vorbesetzen
                if (rotateOrg(false)) return true;
            }
            base.ShowActiveObject = false;
            base.FeedBack.ClearSelected();
            return false;
        }

        void geoObjectInputFaceChanged(ConstructAction.GeoObjectInput sender, IGeoObject SelectedGeoObject)
        {
            selectedObjectsList.Clear();
            base.FeedBack.ClearSelected();
            selectedObjectsList.Add(SelectedGeoObject); // das eine in die Liste
            ListDefault(1); // die Listen löschen und mit "1" vorbesetzen
            rotateOrg(false);
        }

        void SetInnerPointInput(GeoPoint p)
        {
            if (innerPointUp)
            {
                CompoundShape shape = FindShape(p, base.ActiveDrawingPlane);
                if (!(shape == null))
                {
                    Face[] faces = shape.MakeFaces(base.ActiveDrawingPlane);
                    if (faces.Length > 0)
                    {
                        selectedObjectsList.Clear();
                        base.FeedBack.ClearSelected();
                        base.Frame.Project.SetDefaults(faces[0]);
                        selectedObjectsList.Add(faces[0]); // das eine in die Liste
                        ListDefault(1); // die Listen löschen und mit "1" vorbesetzen
                        if (rotateOrg(false))
                        {
                            geoObjectInput.Fixed = true;
                            geoObjectInput.Optional = true;
                        }
                        else
                        {
                            geoObjectInput.Fixed = false;
                            geoObjectInput.Optional = false;
                        }
                    }
                }
            }
            innerPointUp = false;
        }

        void innerPointInputMouseClickEvent(bool up, GeoPoint MousePosition, IView View)
        {
            if (up)
            {
                innerPointUp = true;
            }
        }


        private bool RotateLine(CurveInput sender, ICurve[] Curves, bool up)
        {	// ... nur die sinnvolen Kurven verwenden
            ArrayList usableCurves = new ArrayList();
            for (int i = 0; i < Curves.Length; ++i)
            {
                Line l = Curves[i] as Line;
                if (l != null)
                {
                    usableCurves.Add(Curves[i]);
                }
            }
            // ...hier wird der ursprüngliche Parameter überschrieben. Hat ja keine Auswirkung nach außen.
            Curves = (ICurve[])usableCurves.ToArray(typeof(ICurve));
            if (up)
                if (Curves.Length == 0) sender.SetCurves(Curves, null); // ...die werden jetzt im ControlCenter dargestellt (nur bei up)
                else sender.SetCurves(Curves, Curves[0]);
            // erstmal den Urprungszustand herstellen, "block" ist ja schon gespiegelt 
            if (Curves.Length > 0)
            {	// einfach die erste Kurve nehmen
                ICurve iCurve = Curves[0];
                axisPoint = iCurve.StartPoint;
                axisVector = iCurve.StartDirection;
                axisOrLine = false;
                optionalOrg();
                return rotateOrg(true);
            }
            base.FeedBack.ClearSelected();
            base.ShowActiveObject = false;
            return false;
        }

        private void RotateLineChanged(CurveInput sender, ICurve SelectedCurve)
        {
            axisPoint = SelectedCurve.StartPoint;
            axisVector = SelectedCurve.StartDirection;
            rotateOrg(false);
        }


        bool SetAxisVector(GeoVector vec)
        {
            if (Precision.IsNullVector(vec)) return false;
            axisVector = vec;
            axisOrLine = true;
            optionalOrg();
            return rotateOrg(true);
        }

        GeoVector GetAxisVector()
        {
            return axisVector;
        }

        void SetAxisPoint(GeoPoint p)
        {
            axisPoint = p;
            axisOrLine = true;
            optionalOrg();
            rotateOrg(true);
        }

        GeoPoint GetAxisPoint()
        {
            return axisPoint;
        }


        bool SetAngleInput(Angle angle)
        {
            //            if (angle == 0.0) return false; sonst geht 360° nicht, da 360° als 0.0 ankommt
            angleRotation = angle;
            rotateOrg(false);
            return true;
        }

        Angle GetAngleInput()
        {
            return angleRotation;
        }

        bool SetAngleOffsetInput(Angle angle)
        {
            angleOffsetRotation = angle;
            rotateOrg(false);
            return true;
        }

        Angle GetAngleOffsetInput()
        {
            return angleOffsetRotation;
        }

        private void SetInsertMode(int val)
        {
            Frame.SolidModellingMode = (Make3D.SolidModellingMode)val;
        }

        int GetInsertMode()
        {
            return (int)(Frame.SolidModellingMode);
        }


        public override void OnSetAction()
        {
            base.ActiveObject = Face.Construct();
            if (axisVector.IsNullVector()) axisVector = GeoVector.YAxis;
            if (angleRotation == 0.0) angleRotation = Math.PI;

            base.TitleId = "Constr.Solid.FaceRotate";

            geoObjectInput = new GeoObjectInput("Constr.Solid.FaceRotate.Path");
            geoObjectInput.MouseOverGeoObjectsEvent += new GeoObjectInput.MouseOverGeoObjectsDelegate(geoObjectInputFace);
            geoObjectInput.GeoObjectSelectionChangedEvent += new GeoObjectInput.GeoObjectSelectionChangedDelegate(geoObjectInputFaceChanged);
            if (selectedMode) geoObjectInput.Fixed = true;

            innerPointInput = new GeoPointInput("Constr.Solid.FaceRotate.InnerPoint");
            innerPointInput.SetGeoPointEvent += new GeoPointInput.SetGeoPointDelegate(SetInnerPointInput);
            innerPointInput.MouseClickEvent += new MouseClickDelegate(innerPointInputMouseClickEvent);
            innerPointInput.Optional = true;


            rotateLineInput = new CurveInput("Constr.Face.PathRotate.AxisLine");
            rotateLineInput.Decomposed = true; // nur Einzelelemente, auch bei Polyline und Pfad
            rotateLineInput.MouseOverCurvesEvent += new CurveInput.MouseOverCurvesDelegate(RotateLine);
            rotateLineInput.CurveSelectionChangedEvent += new CurveInput.CurveSelectionChangedDelegate(RotateLineChanged);

            axisPointInput = new GeoPointInput("Constr.Face.PathRotate.AxisPoint", axisPoint);
            axisPointInput.SetGeoPointEvent += new GeoPointInput.SetGeoPointDelegate(SetAxisPoint);
            axisPointInput.GetGeoPointEvent += new GeoPointInput.GetGeoPointDelegate(GetAxisPoint);
            axisPointInput.ForwardMouseInputTo = geoObjectInput;
            axisPointInput.DefinesBasePoint = true;

            axisVectorInput = new GeoVectorInput("Constr.Face.PathRotate.AxisVector", axisVector);
            axisVectorInput.SetGeoVectorEvent += new GeoVectorInput.SetGeoVectorDelegate(SetAxisVector);
            axisVectorInput.GetGeoVectorEvent += new GeoVectorInput.GetGeoVectorDelegate(GetAxisVector);
            //            vectorInput.DefaultGeoVector = ConstrDefaults.DefaultExtrudeDirection;
            axisVectorInput.ForwardMouseInputTo = geoObjectInput;
            optionalOrg();

            AngleInput angleInput = new AngleInput("Constr.Face.PathRotate.Angle", angleRotation);
            angleInput.SetAngleEvent += new AngleInput.SetAngleDelegate(SetAngleInput);
            angleInput.GetAngleEvent += new AngleInput.GetAngleDelegate(GetAngleInput);

            AngleInput angleOffsetInput = new AngleInput("Constr.Face.PathRotate.AngleOffset", angleOffsetRotation);
            angleOffsetInput.SetAngleEvent += new AngleInput.SetAngleDelegate(SetAngleOffsetInput);
            angleOffsetInput.GetAngleEvent += new AngleInput.GetAngleDelegate(GetAngleOffsetInput);
            angleOffsetInput.Optional = true;

            MultipleChoiceInput insertMode = new MultipleChoiceInput("Constr.Solid.SolidInsertMode", "Constr.Solid.SolidInsertMode.Values");
            insertMode.GetChoiceEvent += new MultipleChoiceInput.GetChoiceDelegate(GetInsertMode);
            insertMode.SetChoiceEvent += new CADability.Actions.ConstructAction.MultipleChoiceInput.SetChoiceDelegate(SetInsertMode);

            base.SetInput(geoObjectInput, innerPointInput, rotateLineInput, axisPointInput, axisVectorInput, angleInput, angleOffsetInput, insertMode);
            base.ShowAttributes = true;
            base.ShowActiveObject = false;
            base.OnSetAction();
            if (selectedMode)
                rotateOrg(false); // zum Zeichnen und organisieren

        }



        public override string GetID()
        { return "Constr.Solid.FaceRotate"; }

        public override void OnDone()
        {
            if (base.ShowActiveObject == true)
            {	// es soll mindestens ein shape eingefügt werden
                using (base.Frame.Project.Undo.UndoFrame)
                {
                    for (int i = 0; i < selectedObjectsList.Count; i++) // geht durch die komplette Liste
                    {
                        bool insert = true;
                        if (shapeList[i] != null) // nur hier was machen!
                        {
                            KeepAsLastStyle(ActiveObject);
                            if (Frame.SolidModellingMode == Make3D.SolidModellingMode.unite)
                            { // vereinigen, wenn möglich
                                base.Frame.Project.GetActiveModel().UniteSolid(shapeList[i] as Solid, false);
                                base.ActiveObject = null;
                            }
                            if (Frame.SolidModellingMode == Make3D.SolidModellingMode.subtract)
                            { // abziehen, wenn möglich, sonst: nichts tun
                                insert = base.Frame.Project.GetActiveModel().RemoveSolid(shapeList[i] as Solid, false);
                                base.ActiveObject = null;
                            }
                            if (Precision.IsNull(angleOffsetRotation) && insert && Frame.GetBooleanSetting("Construct.3D_Delete2DBase", false)) // beim Abziehen kann sein, dass nichts passieren soll
                            {
                                if (geoObjectOrgList[i] != null) // evtl. Einzelobjekt (Object oder Path) als Original rauslöschen
                                    ownerList[i].Remove(geoObjectOrgList[i] as IGeoObject);
                                else
                                { // die Einzelelemente des CreateFromModel identifizieren
                                    for (int j = 0; j < pathCreatedFromModelList[i].Count; ++j)
                                    {
                                        IGeoObject obj = null;
                                        if ((pathCreatedFromModelList[i].Curve(j) as IGeoObject).UserData.ContainsData("CADability.Path.Original"))
                                            obj = (pathCreatedFromModelList[i].Curve(j) as IGeoObject).UserData.GetData("CADability.Path.Original") as IGeoObject;
                                        if (obj != null && obj.Owner != null) obj.Owner.Remove(obj); // löschen
                                    }
                                }
                            }
                        }
                    }
                    base.DisassembleBlock = true; // da das ActiveObject aus einem Block besteht: Nur die Einzelteile einfügen!
                    base.OnDone();
                }
            }
            base.OnDone();
        }

    }
}




