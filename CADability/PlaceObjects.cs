using CADability.GeoObject;


namespace CADability.Actions
{
    /// <summary>
    /// 
    /// </summary>
    internal class PlaceObjects : ConstructAction
    {
        private Block block;
        private GeoObjectList originals;
        private GeoPointInput positionPointInput;
        GeoPoint orgRefPoint;

        public PlaceObjects(GeoObjectList list, GeoPoint refPoint)
        {
            block = Block.Construct();
            foreach (IGeoObject go in list)
            {
                block.Add(go.Clone());
            }
            block.RefPoint = refPoint;
            orgRefPoint = refPoint;
            originals = new GeoObjectList(list);
        }

        private void SetPositionPoint(GeoPoint p)
        {
            GeoVector move = p - block.RefPoint;
            ModOp m = ModOp.Translate(move);
            block.Modify(m);
        }

        private GeoPoint GetPositionPoint()
        {
            return block.RefPoint;
        }


        public override void OnSetAction()
        {
            //			base.ActiveObject = block;
            base.TitleId = "PlaceObjects";

            positionPointInput = new GeoPointInput("Objects.Position");
            positionPointInput.SetGeoPointEvent += new CADability.Actions.ConstructAction.GeoPointInput.SetGeoPointDelegate(SetPositionPoint);
            positionPointInput.GetGeoPointEvent += new CADability.Actions.ConstructAction.GeoPointInput.GetGeoPointDelegate(GetPositionPoint);

            base.SetInput(positionPointInput);

            base.ActiveObject = block;

            base.OnSetAction();
        }


        public override string GetID()
        {
            return "PlaceObjects";
        }

        public override void OnDone()
        {
            using (Frame.Project.Undo.UndoFrame)
            {
                ModOp m = ModOp.Translate(block.RefPoint - orgRefPoint);
                originals.Modify(m);
                Frame.Project.GetActiveModel().Add(originals);
                for (int i = 0; i < originals.Count; i++)
                {
                    originals[i].UpdateAttributes(Frame.Project);
                }
            }
            base.ActiveObject = null; // damit es nicht gleich eingefügt wird
            base.OnDone();
        }

    }
}
