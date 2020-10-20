using CADability.GeoObject;
using CADability.Shapes;
using CADability.UserInterface;
using System;
using System.Runtime.Serialization;


namespace CADability.Attribute
{

    public interface IHatchStyle
    {
        HatchStyle HatchStyle { get; set; }
    }
    /// <summary>
    /// Abstract base class for all hatchstyles. A hatchstyle is used to define the
    /// interior of a <see cref="Hatch"/> object.
    /// </summary>
    // created by MakeClassComVisible
    [Serializable()]
    public abstract class HatchStyle : IShowPropertyImpl, ISerializable, INamedAttribute, ICommandHandler
    {
        private string name;
        private HatchStyleList parent;
        public IAttributeList Parent
        {
            set { parent = value as HatchStyleList; }
            get { return parent; }
        }
        protected void FireDidChange(string propertyName, object propertyOldValue)
        {
            if (parent != null)
            {
                ReversibleChange change = new ReversibleChange(this, propertyName, propertyOldValue);
                (parent as IAttributeList).AttributeChanged(this, change);
            }
        }
        internal virtual void Init(Project pr)
        {   // ein neuer Hatchstyle soll initialisiert werden, damit vernünftige Werte drinstehen
            // leere default implementierung
        }
        public HatchStyle()
        {
            // 
            // TODO: Add constructor logic here
            //
        }
        /// <summary>
        /// Gets or sets the name of the HatchStyle
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (parent != null && !(parent as IAttributeList).MayChangeName(this, value))
                {
                    throw new NameAlreadyExistsException(parent, this, value, name);
                }
                string OldName = name;
                name = value;
                FireDidChange("Name", OldName);
                if (parent != null) (parent as IAttributeList).NameChanged(this, OldName);
            }
        }
        IShowProperty INamedAttribute.GetSelectionProperty(string key, Project project, GeoObjectList geoObjectList)
        {
            return null;
        }
        /// <summary>
		/// Generates the lines or other GeoObjects that fill out the given shape 
		/// in the given plane. If a HatchStyle returns null, it must implement the
		/// <see cref="Paint"/> and the <see cref="HitTest"/> methods.
		/// Default implementation returns null.
		/// </summary>
		/// <param name="shape">The shape to be filled</param>
		/// <param name="plane">The plane of the shape</param>
		/// <returns>The resulting GeoObjects or null</returns>
		public virtual GeoObjectList GenerateContent(CompoundShape shape, Plane plane)
        {
            return null;
        }
        /// <summary>
        /// Returns an object to display the properties of the HatchStyle
        /// </summary>
        /// <returns></returns>
        public abstract IShowProperty GetShowProperty();
        public abstract HatchStyle Clone();
        internal abstract void Update(bool AddMissingToList);
        #region IShowPropertyImpl overrides
        public override string LabelText
        {
            get
            {
                return Name;
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Added"/>
        /// </summary>
        /// <param name="propertyTreeView"></param>
        public override void Added(IPropertyPage propertyTreeView)
        {
            base.Added(propertyTreeView);
            //base.resourceId = "HatchStyleName";
        }
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                // ist dieser der aktuelle? Wenn ja, zusätzlich ShowPropertyLabelFlags.Checked setzen
                // Parent ist gewöhnlich die HatchStyleList. Eine andere Situation gibt es derzeit nicht.
                ShowPropertyLabelFlags flags = ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.Selectable | ShowPropertyLabelFlags.Editable;
                if ((parent != null) && (parent.Current == this))
                    flags |= ShowPropertyLabelFlags.Bold;
                return flags;
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
        public override MenuWithHandler[] ContextMenu
        {
            get
            {
                return MenuResource.LoadMenuDefinition("MenuId.HatchStyleEntry", false, this);
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.LabelChanged (string)"/>
        /// </summary>
        /// <param name="NewText"></param>
		public override void LabelChanged(string NewText)
        {
            try
            {
                this.Name = NewText;
                if (propertyTreeView != null) propertyTreeView.Refresh(this);
            }
            catch (NameAlreadyExistsException)
            { }
        }
        #endregion
        #region ISerializable Members
        /// <summary>
        /// Constructor required by deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected HatchStyle(SerializationInfo info, StreamingContext context)
        {
            name = (string)info.GetValue("Name", typeof(string));
        }
        /// <summary>
        /// Implements <see cref="ISerializable.GetObjectData"/>
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (<see cref="System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", name);
        }
        #endregion
        #region ICommandHandler Members
        bool ICommandHandler.OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.HatchStyleListEntry.Delete":
                    {
                        HatchStyleList hsl = propertyTreeView.GetParent(this) as HatchStyleList;
                        if (hsl != null)
                        {
                            hsl.Remove(this);
                            if (propertyTreeView != null) propertyTreeView.Refresh(hsl);
                        }
                        return true;
                    }
                case "MenuId.HatchStyleListEntry.Edit":
                    propertyTreeView.StartEditLabel(this);
                    return true;
                case "MenuId.HatchStyleListEntry.Current":
                    {
                        HatchStyleList hsl = propertyTreeView.GetParent(this) as HatchStyleList;
                        if (hsl != null)
                        {
                            hsl.Current = this;
                            propertyTreeView.Refresh(hsl);
                        }
                        return true;
                    }
            }
            return false;
        }
        bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            // TODO:  Add HatchStyle.OnUpdateCommand implementation
            return false;
        }
        void ICommandHandler.OnSelected(string MenuId, bool selected) { }
        #endregion
    }
}
