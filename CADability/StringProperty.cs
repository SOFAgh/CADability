using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace CADability.UserInterface
{
    // derive from EditableProperty<string>
    // [Serializable()]
    //public class StringPropertyOld : IShowPropertyImpl, ISerializable, ISettingChanged
    //{
    //    private string valString; // das gleiche wie in Textbox, nur damit es beim Speichern noch da ist
    //    private bool IsSetting;
    //    private object ObjectWithProperty;
    //    private PropertyInfo TheProperty;
    //    private bool highlight;
    //    public UserData UserData; // kann man sich zusätzliche Infos mit merken (z.B. einen Index)
    //    public delegate void StringChangedDelegate(object sender, EventArgs e);
    //    public delegate void SetStringDelegate(StringProperty sender, string newValue);
    //    public delegate string GetStringDelegate(StringProperty sender);
    //    public event StringChangedDelegate StringChangedEvent; // zugunsten des identischen "OnSetString" entfernen!
    //    public event GetStringDelegate GetStringEvent;
    //    public event SetStringDelegate SetStringEvent;
    //    string menuResource;
    //    ICommandHandler commandHandler;

    //    public StringPropertyOld(object ObjectWithProperty, string PropertyName, string resourceId)
    //    {
    //        this.ObjectWithProperty = ObjectWithProperty;
    //        TheProperty = ObjectWithProperty.GetType().GetProperty(PropertyName);
    //        if (TheProperty == null)
    //        {   // speziell für ERSACAD: dort haben wg. der flexiblen Namensvergabe die Properties manchmal
    //            // einen prefix, der nicht entsprechend geändert wird. Hier wird, wenn eine passende Property
    //            // nicht gefunden wird, eine solche gesucht, die mit dem namen endet. Ggf könnte man auch den Typ sicherstellen
    //            PropertyInfo[] props = ObjectWithProperty.GetType().GetProperties();
    //            for (int i = 0; i < props.Length; i++)
    //            {
    //                if (props[i].Name.EndsWith(PropertyName, true, CultureInfo.InvariantCulture))
    //                {
    //                    TheProperty = props[i];
    //                    break;
    //                }
    //            }
    //        }
    //        base.resourceId = resourceId;
    //        UserData = new UserData();
    //        IsSetting = false;
    //    }

    //    public StringPropertyOld(string stringValue, string resourceId)
    //    {
    //        base.resourceId = resourceId;
    //        valString = stringValue;
    //        UserData = new UserData();
    //    }
    //    public StringPropertyOld(string stringValue, string resourceId, bool ReadOnly)
    //        : this(stringValue, resourceId)
    //    {
    //    }
    //    public bool Highlight
    //    {
    //        get
    //        {
    //            return highlight;
    //        }
    //        set
    //        {
    //            highlight = value;
    //            if (propertyTreeView != null) propertyTreeView.Refresh(this);
    //        }
    //    }
    //    public void SetContextMenu(string menuResource, ICommandHandler commandHandler)
    //    {
    //        this.menuResource = menuResource;
    //        this.commandHandler = commandHandler;
    //    }
    //    /// <summary>
    //    /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.Refresh ()"/>
    //    /// </summary>
    //    public override void Refresh()
    //    {
    //    }

    //    public bool NotifyOnLostFocusOnly { get; set; }

    //    #region IShowPropertyImpl Overrides
    //    public override MenuWithHandler[] ContextMenu
    //    {
    //        get
    //        {
    //            if (menuResource != null)
    //            {
    //                return MenuResource.LoadMenuDefinition(menuResource, false, commandHandler);
    //            }
    //            return null;
    //        }
    //    }
    //    /// <summary>
    //    /// Overrides <see cref="IShowPropertyImpl.LabelType"/>
    //    /// </summary>
    //    public override ShowPropertyLabelFlags LabelType
    //    {
    //        get
    //        {
    //            ShowPropertyLabelFlags res = ShowPropertyLabelFlags.Selectable;
    //            if (menuResource != null) res |= ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.ContextMenu;
    //            if (highlight) res |= ShowPropertyLabelFlags.Highlight;
    //            return res;
    //        }
    //    }
    //    public override void Removed(IPropertyPage propertyTreeView)
    //    {
    //        base.Removed(propertyTreeView);
    //        if (Frame != null && Frame.Project != null)
    //        {
    //            Frame.Project.Undo.ClearContext();
    //        }
    //    }
    //    #endregion

    //    public string GetString()
    //    {
    //        if (GetStringEvent != null) return GetStringEvent(this);
    //        if (TheProperty != null)
    //        {
    //            MethodInfo mi = TheProperty.GetGetMethod();
    //            object[] prm = new Object[0];
    //            return (string)mi.Invoke(ObjectWithProperty, prm);
    //        }
    //        return null;
    //    }
    //    public void SetString(string newValue)
    //    {
    //        using (Frame.Project.Undo.ContextFrame(this))
    //        {
    //            valString = newValue;
    //            if (StringChangedEvent != null) StringChangedEvent(this, new EventArgs());
    //            if (SettingChangedEvent != null)
    //            {
    //                SettingChangedEvent(resourceId, newValue);
    //            }
    //            if (SetStringEvent != null) SetStringEvent(this, newValue);
    //            else
    //            {
    //                if (TheProperty != null)
    //                {
    //                    MethodInfo mi = TheProperty.GetSetMethod();
    //                    object[] prm = new object[1];
    //                    prm[0] = newValue;
    //                    mi.Invoke(ObjectWithProperty, prm);
    //                }
    //            }
    //        }
    //    }


    //    #region ISerializable Members
    //    protected StringPropertyOld(SerializationInfo info, StreamingContext context)
    //    {   // wird z.Z. nur für DebuggerVisualizer gebraucht
    //        base.resourceId = (string)info.GetValue("ResourceId", typeof(string));
    //        UserData = new UserData();
    //    }
    //    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    //    {
    //        info.AddValue("InternalValue", valString, typeof(string));
    //        info.AddValue("ResourceId", resourceId, typeof(string));
    //    }
    //    #endregion

    //    #region ISettingChanged Members
    //    public event CADability.SettingChangedDelegate SettingChangedEvent;
    //    #endregion
    //}

    [Serializable()]
    public class StringProperty : EditableProperty<string>, ISerializable, ISettingChanged
    {
        public delegate void SetStringDelegate(StringProperty sender, string newValue);
        public delegate string GetStringDelegate(StringProperty sender);

        public StringProperty(object ObjectWithProperty, string PropertyName, string resourceId) : base(ObjectWithProperty, PropertyName, resourceId)
        {
        }

        public StringProperty(string stringValue, string resourceId)
        {
            base.resourceId = resourceId;
        }

        protected override bool TextToValue(string text, out string val)
        {
            val = text;
            return true;
        }

        protected override string ValueToText(string val)
        {
            return val;
        }

        #region deprecated adaption to old implementation of StringProperty
        [Obsolete("use delegate StringProperty.OnGetValue instead")]
        public event GetStringDelegate GetStringEvent
        {
            add
            {
                base.OnGetValue = delegate ()
                {
                    return value(this);
                };
            }
            remove
            {
                base.OnGetValue = null;
            }
        }
        [Obsolete("use delegate StringProperty.OnSetValue instead")]
        public event SetStringDelegate SetStringEvent
        {
            add
            {
                base.OnSetValue = delegate (string l) { value(this, l); };
            }
            remove
            {
                base.OnSetValue = null;
            }
        }
        [Obsolete("use DeferUpdate instead")]
        public bool NotifyOnLostFocusOnly
        {
            set
            {
                DeferUpdate = value;
            }
        }
        [Obsolete("use SetValue instead")]
        public void SetString(string val)
        {
            SetValue(val, false);
        }
        [Obsolete("use GetValue instead")]
        public string GetString()
        {
            return GetValue();
        }
        [Obsolete("use delegate StringProperty.OnSetValue instead")]
        public delegate void StringChangedDelegate(object sender, EventArgs e);
        [Obsolete("use delegate StringProperty.OnSetValue instead")]
        public event StringChangedDelegate StringChangedEvent
        {
            add
            {
                base.OnSetValue = delegate (string l) { value(this, new EventArgs()); };
            }
            remove
            {
                base.OnSetValue = null;
            }
        }
        #endregion

        #region ISerializable Members
        protected StringProperty(SerializationInfo info, StreamingContext context)
        {   // wird z.Z. nur für DebuggerVisualizer gebraucht
            base.resourceId = (string)info.GetValue("ResourceId", typeof(string));
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("InternalValue", GetValue(), typeof(string));
            info.AddValue("ResourceId", resourceId, typeof(string));
        }
        #endregion

        #region ISettingChanged Members
        public event CADability.SettingChangedDelegate SettingChangedEvent;
        #endregion
    }
}

