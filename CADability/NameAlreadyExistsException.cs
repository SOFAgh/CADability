namespace CADability
{
    /// <summary>
    /// Bei der Namensänderung eines Objektes (z.B. Layer, Farbe u.s.w.) tritt diese
    /// Exception auf, wenn eine nach Namen sortierte Liste (z.B. LayerList) 
    /// ein Objekt mit diesen Namen bereits enthält. Diese Exception wird in CONDOR
    /// bei Namensänderungen abgefangen und der alte Name wird wieder gesetzt.
    /// 
    /// </summary>

    public class NameAlreadyExistsException : System.Exception
    {
        /// <summary>
        /// Der neue Name, der versucht wurde zu setzen
        /// </summary>
        public string NewName;
        /// <summary>
        /// Der alte Name, so wie er in Ordnung war
        /// </summary>
        public string OldName;
        /// <summary>
        /// Die Liste, die das Objekt enthält, und die bereits ein objekt mit neuem Namen enthält
        /// </summary>
        public object ContainingList;
        /// <summary>
        /// Das Objekt (z.B. Layer), welches den Verstoß ausgelöst hat
        /// </summary>
        public object OffendingObject;
        public NameAlreadyExistsException(object ContainingList, object OffendingObject, string NewName, string OldName) : this(ContainingList, OffendingObject, NewName)
        {
            this.OldName = OldName;
        }
        public NameAlreadyExistsException(object ContainingList, object OffendingObject, string NewName) : base("Name '" + NewName + "' already exists in List")
        {
            this.NewName = NewName;
            this.ContainingList = ContainingList;
            this.OffendingObject = OffendingObject;
        }
    }
}
