using CADability.GeoObject;
using System;
using System.Runtime.Serialization;

namespace CADability.Attribute
{
    using System.Collections.Generic;
    using UserInterface;

    /// <summary>
    /// Interface supported by all objects that have a Layer proerty
    /// </summary>

    public interface ILayer
    {
        /// <summary>
        /// Gets or sets the layer.
        /// </summary>
		Layer Layer { get; set; }
    }
    /*
	public class LayerListEntry :IShowPropertyImpl,ICommandHandler
	{
		private Layer layer;
		private LayerList layerList;
		private IShowProperty[] showProperties;
		private DisplayLayerCtrl control;
		public LayerListEntry(Layer l, LayerList ll)
		{
			layer = l;
			layerList = ll;
		}
		#region IShowProperty Members
		
		public override string LabelText
		{
			get
			{
				return layer.Name;
			}
		}

		public override ShowPropertyLabelFlags LabelType
		{
			get
			{
				ShowPropertyLabelFlags flags = ShowPropertyLabelFlags.Editable|ShowPropertyLabelFlags.Selectable|ShowPropertyLabelFlags.ContextMenu|ShowPropertyLabelFlags.ContextMenu ;
				if( layerList.Current==layer )
					flags |= ShowPropertyLabelFlags.Bold;
				return flags;
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
				
				return 4;
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
				if (showProperties==null)
				{
					ImageList LayerImageList = BitmapTable.GetImageList("Layer.png",16,16);
					showProperties = new IShowProperty[4];
					BooleanProperty prop0 = new BooleanProperty(layer,"Visible",
						"Layer.Visibility",
						"Layer.Visibility.Values");
					prop0.SetImages(LayerImageList,0,1);
					showProperties[0] = prop0;
					BooleanProperty prop1 = new BooleanProperty(layer,"Printable",
						"Layer.Printability",
						"Layer.Printability.Values");
					prop1.SetImages(LayerImageList,4,5);
					showProperties[1] = prop1;
					BooleanProperty prop2 = new BooleanProperty(layer,"Pickable",
						"Layer.Pickability",
						"Layer.Pickability.Values");
					prop2.SetImages(LayerImageList,2,3);
					showProperties[2] = prop2;
					ColorList ct = layerList.ColorList ;
					if(ct != null)
					{
						ct.DidModifyEvent +=new DidModifyDelegate(ct_DidModify);
						showProperties[3] = new ColorSelectionProperty(layer,"ColorDef","Layer.Color",ct,0);
					}
				}
				return showProperties;
			}
		}

		public override Control GetValueControl(int Width, int Height)
		{
			if (control==null)
			{	// erstmaliges Erzeugen des Controls
				control = new DisplayLayerCtrl(layer);
			}
			return control;
		}
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.GetContextMenu"/>, 
        /// returns the context menu with the id "MenuId.LayerListEntry".
        /// (see <see cref="MenuResource.LoadContextMenu"/>)
        /// </summary>
        public override ContextMenu GetContextMenu()
		{
			ContextMenu res = MenuResource.LoadContextMenu("MenuId.LayerListEntry",this);
			foreach( MenuItem mi in res.MenuItems)
			{
				MenuItemWithID miWid = mi as MenuItemWithID;
				switch( miWid.ID)
				{
					case "MenuId.LayerListEntry.Delete":
						if(layerList.Current == layer )
							miWid.Enabled=false;
						break;
					case "MenuId.LayerListEntry.Current":
						if(layerList.Current == layer )
						{
							miWid.Checked =true;
							miWid.Enabled=false;
						}
						else
						{
							miWid.Checked =false;
							miWid.Enabled=true;
						}break;
				}
			}
			return  res;
		}

		public override void LabelChanged(string NewText)
		{
			layer.Name = NewText;
		}

	
		public override void SetFocus(){control.Focus();}
		public override Control[] FocusableControls()
		{
			Control[] ctrls = new Control[1];
			ctrls[0] = control;
			return ctrls;
		}
		#endregion

		private void ct_DidModify(object sender, EventArgs args)
		{
			if (propertyTreeView!=null) propertyTreeView.Refresh(showProperties[3]);
		}
		#region ICommandHandler Members
        bool ICommandHandler.OnCommand(string MenuId)
		{
			switch( MenuId )
			{
				case "MenuId.LayerListEntry.Edit":
					if (propertyTreeView!=null) propertyTreeView.StartEditLabel(this);
					return true;
				case "MenuId.LayerListEntry.Delete":
					layerList.Remove(layer);
					if (propertyTreeView!=null) propertyTreeView.Refresh(layerList);
					return true;
				case "MenuId.LayerListEntry.Current":
					layerList.Current = layer;
					if (propertyTreeView!=null) propertyTreeView.Refresh(layerList);
					return true;
			}
			return false;
		}
        bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
		{
			// TODO: betreffende MenueIds behandeln
			return false;
		}
		#endregion
	}
*/
    public enum LayerUsage { UseAllLayer, OnlyVisible, OnlyPickable }


    public class LayerComparer : IEqualityComparer<Layer>
    {
        #region IEqualityComparer<Layer> Members

        bool IEqualityComparer<Layer>.Equals(Layer x, Layer y)
        {
            return (x.Name == y.Name);
        }

        int IEqualityComparer<Layer>.GetHashCode(Layer obj)
        {
            return obj.Name.GetHashCode();
        }

        #endregion
    }
    /// <summary>
    /// A Layer is an attribute to <see cref="IGeoObject"/> objects. 
    /// With several layers you can group the GeoObjects in a <see cref="Model"/>.
    /// In a <see cref="ModelView"/> you can select which layers are visible.
    /// In a <see cref="Filter"/> you can select which layers are be pickable
    /// The property <see cref="DisplayOrder"/> affects the order of display or printing
    /// if the according feature is enabled in a <see cref="ModelView"/> or <see cref="Layout"/>.
    /// Layer implements the <see cref="IShowProperty"/> interface to present ist properties.
    /// </summary>
    // created by MakeClassComVisible
    [Serializable()]
    public class Layer : IShowPropertyImpl, ISerializable, INamedAttribute,
            ICommandHandler
    {
        private string name; // der Name
        private int displayOrder;
        private int transparency;
        /// <summary>
        /// Nach längerem hin und her scheint es am sinnvollsten, dass ein Layer
        /// seine Liste kennt, in der er sich befindet. Alle Möglichkeiten mit events
        /// sind auch nicht sparsamer, im Gegenteil. Und es darf immer nur eine Liste geben,
        /// denn sonst ist auch z.B. die ColorList nicht dingfest zu machen.
        /// </summary>
        private LayerList parent; // die Liste, in der der Layer steckt.
        private void FireDidChange(string propertyName, object propertyOldValue)
        {   // Eine Eigenschaft des Layers hat sich geändert.
            // Wird an parent, also LayerListe weitergeleitet
            if (parent != null)
            {
                ReversibleChange change = new ReversibleChange(this, propertyName, propertyOldValue);
                (parent as IAttributeList).AttributeChanged(this, change);
            }
        }
        private IShowProperty[] showProperties;
        public Layer()
        {	// kein Name
            displayOrder = 0;
            transparency = 0;
        }
        public Layer(string Name) : this()
        {
            name = Name;
        }

        public static LayerComparer LayerComparer = new LayerComparer();

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
                if (parent != null) (parent as IAttributeList).NameChanged(this, OldName);
                FireDidChange("Name", OldName);
            }
        }
        public Layer Clone()
        {
            Layer res = new Layer();
            res.name = name;
            res.displayOrder = displayOrder;
            res.transparency = transparency;
            return res;
        }
        internal LayerList Parent
        {
            get { return parent; }
            set { parent = value; }
        }
        IAttributeList INamedAttribute.Parent
        {
            get { return parent; }
            set { parent = value as LayerList; }
        }
        IShowProperty INamedAttribute.GetSelectionProperty(string key, Project project, GeoObjectList geoObjectList)
        {
            return null;
        }

        #region IShowProperty Members
        public override string LabelText
        {
            get { return name; }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.LabelType"/>
        /// </summary>
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                ShowPropertyLabelFlags flags = ShowPropertyLabelFlags.Editable | ShowPropertyLabelFlags.Selectable | ShowPropertyLabelFlags.ContextMenu;
                if (parent.Current != null && parent.Current.Name == Name)
                    flags |= ShowPropertyLabelFlags.Bold;

                return flags;
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.EntryType"/>, 
        /// returns <see cref="ShowPropertyEntryType.SimpleEntry"/>.
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
            get { return 2; }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.SubEntries"/>, 
        /// returns the subentries in this property view.
        /// </summary>
        public override IShowProperty[] SubEntries
        {
            get
            {
                if (showProperties == null)
                {
                    showProperties = new IShowProperty[2];
                    IntegerProperty ip = new IntegerProperty(displayOrder, "Layer.DisplayOrder");
                    ip.GetIntEvent += new IntegerProperty.GetIntDelegate(GetDisplayOrder);
                    ip.SetIntEvent += new IntegerProperty.SetIntDelegate(SetDisplayOrder);
                    ip.SetMinMax(int.MinValue, int.MaxValue, true);
                    showProperties[0] = ip;
                    IntegerProperty ipt = new IntegerProperty(displayOrder, "Layer.Transparency");
                    ipt.GetIntEvent += new IntegerProperty.GetIntDelegate(GetTransparency);
                    ipt.SetIntEvent += new IntegerProperty.SetIntDelegate(SetTransparency);
                    ipt.IntegerValue = transparency;
                    ipt.SetMinMax(0, 255, true);
                    showProperties[1] = ipt;
                }
                return showProperties;
            }
        }
        void SetDisplayOrder(IntegerProperty sender, int newValue)
        {
            displayOrder = newValue;
        }
        int GetDisplayOrder(IntegerProperty sender)
        {
            return displayOrder;
        }
        void SetTransparency(IntegerProperty sender, int newValue)
        {
            Transparency = newValue;
        }
        int GetTransparency(IntegerProperty sender)
        {
            return transparency;
        }
        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.LabelChanged (string)"/>
        /// </summary>
        /// <param name="NewText"></param>
		public override void LabelChanged(string NewText)
        {
            Name = NewText;
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Added"/>
        /// </summary>
        /// <param name="propertyTreeView"></param>
        public override void Added(IPropertyTreeView propertyTreeView)
        {
            base.Added(propertyTreeView);
            base.resourceId = "LayerName";
        }
        /*	public override void Removed(IPropertyTreeView propertyTreeView)
            {
                ColorList ct = FindColorList();
                if(ct != null)
                {
                    ct.DidModify -= new DidModifyDelegate(ColorListDidModify);
                }
                base.Removed (propertyTreeView);
            }
    */
        public override MenuWithHandler[] ContextMenu
        {
            get
            {
                return MenuResource.LoadMenuDefinition("MenuId.LayerListEntry", false, this);
            }
        }
        public override void EndEdit(bool aborted, bool modified, string newValue)
        {
            if (!aborted && newValue != Name)
            try
            {
                Name = newValue;
            }
            catch (NameAlreadyExistsException)
            {
            }
        }
        #endregion

        /*		private void ColorListDidModify(object sender, EventArgs args)
                {	// wenn sich die ColorList ändert, dann muss sich das in der Liste der angezeigten Farben wiederspiegeln
                    propertyTreeView.Refresh(this);
                }
                */
        #region ISerializable Members
        /// <summary>
        /// Constructor required by deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected Layer(SerializationInfo info, StreamingContext context)
        {
            name = (string)info.GetValue("Name", typeof(string));
            try
            {
                displayOrder = info.GetInt32("DisplayOrder");
            }
            catch (SerializationException)
            {
                displayOrder = 0;
            }
            try
            {
                transparency = info.GetInt32("Transparency");
            }
            catch (SerializationException)
            {
                transparency = 0;
            }
        }
        /// <summary>
        /// Implements <see cref="ISerializable.GetObjectData"/>
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (<see cref="System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", name);
            info.AddValue("DisplayOrder", displayOrder);
            info.AddValue("Transparency", transparency);
        }

        #endregion

        public int DisplayOrder
        {
            get
            {
                return displayOrder;
            }
            set
            {
                displayOrder = value;
            }
        }
        /// <summary>
        /// Gets or sets the transparency of this layer: 0 is not transparent, 255 is totally transparent
        /// </summary>
        public int Transparency
        {
            get
            {
                return transparency;
            }
            set
            {
                int oldtransparence = transparency;
                transparency = value;
                FireDidChange("Transparency", oldtransparence);
            }
        }
        #region ICommandHandler Members
        bool ICommandHandler.OnCommand(string MenuId)
        {
            if (parent == null) return false;
            switch (MenuId)
            {
                case "MenuId.LayerListEntry.Edit":
                    if (propertyTreeView != null) propertyTreeView.StartEditLabel(this);
                    return true;
                case "MenuId.LayerListEntry.Delete":
                    parent.Remove(this);
                    if (propertyTreeView != null) propertyTreeView.Refresh(parent);
                    return true;
                case "MenuId.LayerListEntry.Current":
                    parent.Current = this;
                    if (propertyTreeView != null) propertyTreeView.Refresh(parent);
                    return true;
            }
            return false;
        }
        bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            switch (MenuId)
            {
                case "MenuId.LayerListEntry.Delete":
                    if (parent.Current == this)
                        CommandState.Enabled = false;
                    break;
                case "MenuId.LayerListEntry.Current":
                    if (parent.Current == this)
                    {
                        CommandState.Checked = true;
                        CommandState.Enabled = false;
                    }
                    else
                    {
                        CommandState.Checked = false;
                        CommandState.Enabled = true;
                    }
                    break;
            }
            return false;
        }
        #endregion
    }


    public class LayerSelectionProperty : MultipleChoiceProperty
    {
        private LayerList layerList;
        private ILayer objectWithLayer;
        private IGeoObject toWatch;

        public LayerSelectionProperty(string resourceId, LayerList layerList, Layer select)
        {
            this.layerList = layerList;
            base.resourceId = resourceId;
            choices = new string[layerList.Count];
            int i = 0;
            foreach (Layer lay in layerList)
            {
                choices[i] = lay.Name;
                i++;
            }
            if (select != null) base.selectedText = select.Name;
        }

        public LayerSelectionProperty(ILayer ObjectWithLayer, string resourceId, LayerList ll) :
            this(ObjectWithLayer, resourceId, ll, false)
        { }
        public LayerSelectionProperty(ILayer ObjectWithLayer, string resourceId, LayerList ll, bool includeUndefined)
        {
            layerList = ll;
            base.resourceId = resourceId;
            if (includeUndefined)
            {
                choices = new string[ll.Count + 1];
                int i = 0;
                foreach (Layer lay in ll)
                {
                    choices[i + 1] = lay.Name;
                    i++;
                }
                string undef = StringTable.GetString("Layer.Undefined");
                // sollte es den Namen schon geben, werden solange - davor und dahintergemacht, bis es den Namen mehr gibt
                while (layerList.Find(undef) != null) undef = "-" + undef + "-";
                choices[0] = undef;
                objectWithLayer = ObjectWithLayer;
                if (objectWithLayer != null && objectWithLayer.Layer != null)
                {
                    selectedText = objectWithLayer.Layer.Name;
                }
                else
                {
                    selectedText = undef;
                }
            }
            else
            {
                choices = new string[ll.Count];
                int i = 0;
                foreach (Layer lay in ll)
                {
                    choices[i] = lay.Name;
                    i++;
                }
                objectWithLayer = ObjectWithLayer;
                if (objectWithLayer != null && objectWithLayer.Layer != null)
                    selectedText = objectWithLayer.Layer.Name;
            }
            toWatch = objectWithLayer as IGeoObject;
        }

        public delegate void LayerChangedDelegate(Layer selected);
        public event LayerChangedDelegate LayerChangedEvent;
        protected override void OnSelectionChanged(string selected)
        {
            if (selected == null) return;
            Layer lay = layerList.Find(selected); // kann null sein
            if (objectWithLayer != null) objectWithLayer.Layer = lay;
            base.OnSelectionChanged(selected);
            if (LayerChangedEvent != null) LayerChangedEvent(lay);
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Added"/>
        /// </summary>
        /// <param name="propertyTreeView"></param>
        public override void Added(IPropertyPage propertyTreeView)
        {
            base.Added(propertyTreeView);
            if (toWatch != null) toWatch.DidChangeEvent += new ChangeDelegate(GeoObjectDidChange);
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Removed"/>
        /// </summary>
        /// <param name="propertyTreeView">the IPropertyTreeView from which it was removed</param>
        public override void Removed(IPropertyPage propertyTreeView)
        {
            base.Removed(propertyTreeView);
            if (toWatch != null) toWatch.DidChangeEvent -= new ChangeDelegate(GeoObjectDidChange);
        }
        private void GeoObjectDidChange(IGeoObject Sender, GeoObjectChange Change)
        {
            if (Sender == toWatch && Change.OnlyAttributeChanged && propertyTreeView != null)
            {
                if ((Change as GeoObjectChange).MethodOrPropertyName == "Layer" ||
                    (Change as GeoObjectChange).MethodOrPropertyName == "Style")
                {
                    if (toWatch.Layer != null) base.selectedText = toWatch.Layer.Name;
                    else base.selectedText = null;
                    propertyTreeView.Refresh(this);
                }
            }
        }
        public IGeoObject Connected
        {   // mit dieser Property kann man das kontrollierte Geoobjekt ändern
            get { return toWatch; }
            set
            {
                if (base.propertyTreeView != null)
                {   // dann ist diese Property schon Added und nicht removed
                    if (toWatch != null) toWatch.DidChangeEvent -= new ChangeDelegate(GeoObjectDidChange);
                }
                toWatch = value;
                objectWithLayer = value as ILayer;
                if (toWatch != null) toWatch.DidChangeEvent += new ChangeDelegate(GeoObjectDidChange);
            }
        }
    }
}

