using System.Collections;

namespace CADability.UserInterface
{
    /// <summary>
    /// A simple container for several <see cref="IShowProperty"/> entries in the
    /// treeview of the controlcenter. Add subentries to this group before the group
    /// ist displayed in the treeview. If you add or remove subentries while the
    /// group is displayed you will have to call <see cref="IPropertyTreeView.Refresh"/>.
    /// </summary>

    public class SimplePropertyGroup : IShowPropertyImpl
    {
        private ArrayList subentries;
        public SimplePropertyGroup(string resourceId)
        {
            this.resourceId = resourceId;
            subentries = new ArrayList();
        }
        public void Add(IShowProperty subEntry)
        {
            subentries.Add(subEntry);
        }
        public void Add(IShowProperty[] subEntries)
        {
            this.subentries.AddRange(subEntries);
        }
        public void Remove(IShowProperty subEntry)
        {
            subentries.Remove(subEntry);
        }
        public void RemoveAll()
        {
            subentries.Clear();
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
        /// Overrides <see cref="IShowPropertyImpl.SubEntries"/>, 
        /// returns the subentries in this property view.
        /// </summary>
        public override IShowProperty[] SubEntries
        {
            get
            {
                return (IShowProperty[])subentries.ToArray(typeof(IShowProperty));
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
                return subentries.Count;
            }
        }

        #endregion
    }
}
