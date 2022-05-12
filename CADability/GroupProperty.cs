namespace CADability.UserInterface
{
    /// <summary>
    /// A simple entry for the showproperty tree, wich is represented by a GruopTitle
    /// eintry which contains some subentries. A folder in the treeview of the controlcenter.
    /// The subentries mus be specified in the constructur.
    /// </summary>
    public class GroupProperty : IShowPropertyImpl
    {
        private IPropertyEntry[] subEntries;
        public GroupProperty(string resourceId, IPropertyEntry[] subEntries)
        {
            this.resourceId = resourceId;
            this.subEntries = subEntries;
        }
        public void SetSubEntries(IPropertyEntry[] subEntries)
        {
            this.subEntries = subEntries;
            propertyTreeView?.Refresh(this);
        }
        #region IShowProperty Members

        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                return ShowPropertyLabelFlags.Selectable;
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.EntryType"/>, 
        /// returns <see cref="ShowPropertyEntryType.GroupTitle"/>.
        /// </summary>
        public override ShowPropertyEntryType EntryType
        {
            get
            {
                return ShowPropertyEntryType.GroupTitle;
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.SubEntriesCount"/>, 
        /// returns the number of subentries in this property view.
        /// </summary>
        public override int SubEntriesCount
        {
            get
            {
                if (subEntries == null) return 0;
                return subEntries.Length;
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.SubEntries"/>, 
        /// returns the subentries in this property view.
        /// </summary>
        public override IPropertyEntry[] SubItems
        {
            get
            {
                return subEntries;
            }
        }
        #endregion
    }
}
