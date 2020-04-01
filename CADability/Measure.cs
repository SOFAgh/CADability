namespace CADability.Actions
{
    /// <summary>
    /// 
    /// </summary>

    public class Measure : ConstructAction
    {
        public Measure()
        {
            // 
            // TODO: Add constructor logic here
            //
        }
        /// <summary>
        /// Implements <see cref="Action.OnSetAction"/>. If you override this method
        /// don't forget to call the bas implementation.
        /// </summary>
        public override void OnSetAction()
        {
            base.TitleId = "Measure";
            base.SetInput();

            base.OnSetAction();
        }
        /// <summary>
        /// Implements <see cref="Action.GetID"/>.
        /// </summary>
        public override string GetID()
        { return "Measure"; }

        public override void OnDone()
        { base.OnDone(); }
    }
}
