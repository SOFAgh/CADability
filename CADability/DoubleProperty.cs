using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace CADability.UserInterface
{

    /// <summary>
    /// Implements a hotspot <see cref="IHotSpot"/> to manipulate a lenth via a length property
    /// </summary>

    public class DoubleHotSpot : IHotSpot
    {
        private DoubleProperty doubleProperty;
        public GeoPoint Position;
        /// <summary>
        /// Constructs a hotspot to manipulate a double property
        /// </summary>
        /// <param name="doubleProperty">the property to be manipulated</param>
        public DoubleHotSpot(DoubleProperty doubleProperty)
        {
            this.doubleProperty = doubleProperty;
        }
        #region IHotSpot Members
        /// <summary>
        /// Implements <see cref="CADability.IHotSpot.GetHotspotPosition ()"/>
        /// </summary>
        /// <returns></returns>
        public GeoPoint GetHotspotPosition()
        {
            return Position;
        }
        public bool IsSelected
        {
            get
            {
                return doubleProperty.IsSelected;
            }
            set
            {
                doubleProperty.IsSelected = value;
            }
        }
        /// <summary>
        /// Implements <see cref="CADability.IHotSpot.StartDrag ()"/>
        /// </summary>
        public void StartDrag(IFrame frame)
        {
            doubleProperty.StartModifyWithMouse();
        }
        public string GetInfoText(CADability.UserInterface.InfoLevelMode Level)
        {
            return doubleProperty.LabelText;
        }
        public MenuWithHandler[] ContextMenu
        {
            get
            {
                return doubleProperty.ContextMenu;
            }
        }
        public bool Hidden { get { return doubleProperty.ReadOnly; } }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// 
    // created by MakeClassComVisible
    [Serializable()]
    public class DoublePropertyOld : IShowPropertyImpl, ISerializable, ISettingChanged, IDeserializationCallback
    {
        private object ObjectWithDouble;
        private PropertyInfo TheProperty;
        private string text; // das was im editfeld steht
        // private Control textBoxContainer; // für den Fall, dass das Locked Symbol angezeigt wird
        private bool lockable;
        private bool locked;
        private bool highlight;
        private bool notifyOnLostFocusOnly;
        private NumberFormatInfo numberFormatInfo;
        private bool IsSetting;
        private bool useContext;
        private string contextMenuId; // wenn !=null, dann ContextMenue
        private double minValue; // Grenzen für die Eingabe
        private double maxValue;
        private double internalValue;
        private ICommandHandler contextMenuHandler;
        private string settingName;
        private Dictionary<double, string> specialValues;
        public event EditingDelegate EditingTextboxEvent;
        public event ModifyWithMouseDelegate ModifyWithMouseEvent;
        public UserData UserData;
        public delegate void SetDoubleDelegate(DoublePropertyOld sender, double l);
        public delegate double GetDoubleDelegate(DoublePropertyOld sender);
        public event SetDoubleDelegate SetDoubleEvent;
        public event GetDoubleDelegate GetDoubleEvent;
        protected DoublePropertyOld(IFrame frame)
        {
            UserData = new UserData();
            IsSetting = false;
            this.Frame = frame;

            numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            int decsym = Settings.GlobalSettings.GetIntValue("Formatting.Decimal", 0); // Systemeinstellung | Punkt | Komma
            // wenn 0, dann unverändert
            if (decsym == 1) numberFormatInfo.NumberDecimalSeparator = ".";
            else if (decsym == 2) numberFormatInfo.NumberDecimalSeparator = ",";
            if (frame != null)
            {
                numberFormatInfo.NumberDecimalDigits = frame.GetIntSetting("Formatting.GeneralDouble", 3);
            }
            else
            {
                numberFormatInfo.NumberDecimalDigits = 3;
            }
            minValue = double.MinValue;
            maxValue = double.MaxValue;

        }
        public DoublePropertyOld(object ObjectWithDouble, string PropertyName, string ResourceId, IFrame Frame)
            : this(Frame)
        {
            this.ObjectWithDouble = ObjectWithDouble;
            base.resourceId = ResourceId;
            TheProperty = ObjectWithDouble.GetType().GetProperty(PropertyName);
            if (TheProperty == null)
            {   // speziell für ERSACAD: dort haben wg. der flexiblen Namensvergabe die Properties manchmal
                // einen prefix, der nicht entsprechend geändert wird. Hier wird, wenn eine passende Property
                // nicht gefunden wird, eine solche gesucht, die mit dem namen endet. Ggf könnte man auch den Typ sicherstellen
                PropertyInfo[] props = ObjectWithDouble.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    if (props[i].Name.EndsWith(PropertyName, true, CultureInfo.InvariantCulture))
                    {
                        TheProperty = props[i];
                        break;
                    }
                }
            }
            //textBox = new ManagedKeysTextbox();
            //textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            //textBox.AcceptsReturn = true;
            //textBox.LostFocus += new EventHandler(OnLostFocus);
            //textBox.KeyUp += new KeyEventHandler(OnKeyUp);
            //textBox.KeyPress += new KeyPressEventHandler(OnKeyPress);
            DoubleChanged(); // das hier führt zu Problemen, immer noch???
            useContext = true;
            settingChanged = false;
        }
        //public DoubleProperty(object ObjectWithDouble, string PropertyName, string resourceId)
        //    : this()
        //{
        //    this.ObjectWithDouble = ObjectWithDouble;
        //    base.resourceId = resourceId;
        //    TheProperty = ObjectWithDouble.GetType().GetProperty(PropertyName);
        //    textBox = new ManagedKeysTextbox();

        //    textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        //    textBox.AcceptsReturn = true;
        //    textBox.LostFocus += new EventHandler(OnLostFocus);
        //    textBox.KeyUp += new KeyEventHandler(OnKeyUp);
        //    textBox.KeyPress += new KeyPressEventHandler(OnKeyPress);
        //    Refresh(); // wo ist hier das Problem? so stand es im Kommentar
        //    useContext = true;
        //    settingChanged = false;
        //}
        public DoublePropertyOld(string resourceId, IFrame Frame)
            : this(Frame)
        {
            base.resourceId = resourceId;

            //textBox = new ManagedKeysTextbox();
            //textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            //textBox.AcceptsReturn = true;
            //textBox.LostFocus += new EventHandler(OnLostFocus);
            //textBox.KeyUp += new KeyEventHandler(OnKeyUp);
            //textBox.KeyPress += new KeyPressEventHandler(OnKeyPress);
            useContext = true;
            settingChanged = false;
        }
        public DoublePropertyOld(string SettingName, string resourceId, double initialValue, IFrame frame)
            : this(frame)
        {
            IsSetting = false;
            base.resourceId = resourceId;
            UserData = new UserData();
            settingName = SettingName;
            internalValue = initialValue;
            //textBox = new ManagedKeysTextbox();
            //textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            //textBox.AcceptsReturn = true;
            //textBox.LostFocus += new EventHandler(OnLostFocus);
            //textBox.KeyUp += new KeyEventHandler(OnKeyUp);
            //textBox.KeyPress += new KeyPressEventHandler(OnKeyPress);
            useContext = true;
            SetText(internalValue);
            settingChanged = false;
        }
        public int DecimalDigits
        {
            get
            {
                return numberFormatInfo.NumberDecimalDigits;
            }
            set
            {
                numberFormatInfo.NumberDecimalDigits = value;
                Refresh();
            }
        }
        public void InitSpinSettings(double min, double max, double step)
        {
            DoubleChanged();
        }
        public void SetMinMax(double min, double max)
        {
            minValue = min;
            maxValue = max;
        }
        public bool UseContext
        {
            set { useContext = value; }
            get { return useContext; }
        }
        public bool ReadOnly
        {
            set { base.ReadOnly = value; }
            get { return base.ReadOnly; }
        }
        public bool Lockable
        {
            get { return lockable; }
            set { lockable = value; }
        }
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (locked == value) return;
                locked = value;
                if (LockedChangedEvent != null) LockedChangedEvent(locked);
                if (propertyPage != null) propertyPage.Refresh(this);
            }
        }
        public delegate void LockedChangedDelegate(bool locked);
        public event LockedChangedDelegate LockedChangedEvent;
        public void AddSpecialValue(double val, string resourceId)
        {
            if (specialValues == null) specialValues = new Dictionary<double, string>();
            specialValues[val] = resourceId;
            Refresh();
        }
        public bool Highlight
        {
            get
            {
                return highlight;
            }
            set
            {
                highlight = value;
                if (propertyTreeView != null) propertyTreeView.Refresh(this);
            }
        }
        public bool NotifyOnLostFocusOnly
        {
            get
            {
                return notifyOnLostFocusOnly;
            }
            set
            {
                notifyOnLostFocusOnly = value;
            }
        }
        public void SetContextMenu(string contextMenuId, ICommandHandler contextMenuHandler)
        {
            this.contextMenuId = contextMenuId;
            this.contextMenuHandler = contextMenuHandler;
        }
        public bool IsSelected
        {
            get
            {
                return (propertyTreeView.GetCurrentSelection() == this);
            }
            set
            {
                if (value) propertyTreeView.SelectEntry(this);
                else
                {
                    if (propertyTreeView.GetCurrentSelection() == this)
                        propertyTreeView.SelectEntry(null);
                }
            }
        }
        private void SetText(double d)
        {
            // spin not implemented yet
            if (numberFormatInfo != null && Math.Abs(d) < Math.Pow(10, -numberFormatInfo.NumberDecimalDigits))
            {
                NumberFormatInfo niclone = numberFormatInfo.Clone() as NumberFormatInfo;
                double l = Math.Log10(Math.Abs(d));
                if (l < -8)
                {
                    text = d.ToString("g", niclone);
                }
                else if ((-l + 1) > 0 && (-l + 1) < 20)
                {
                    niclone.NumberDecimalDigits = (int)(-l + 1);
                    text = d.ToString("f", niclone);
                }
                else
                {
                    text = d.ToString("f", numberFormatInfo);
                }
            }
            else
            {
                text = d.ToString("f", numberFormatInfo);
            }
            if (propertyTreeView != null) propertyTreeView.Refresh(this);
        }
        private void SetDouble(double d)
        {
            if (IsSetting) return;
            try
            {
                IsSetting = true;
                if (!settingChanged) settingChanged = internalValue != d;
                internalValue = d;
                if (SetDoubleEvent != null)
                {
                    SetDoubleEvent(this, d);
                }
                else if (TheProperty != null)
                {
                    MethodInfo mi = TheProperty.GetSetMethod();
                    object[] prm = new object[1];
                    prm[0] = d;
                    try
                    {
                        mi.Invoke(ObjectWithDouble, prm);
                    }
                    catch (TargetInvocationException) { }
                }
                SetText(d);
            }
            finally
            {
                IsSetting = false;
            }
        }
        private double GetDouble()
        {
            if (GetDoubleEvent != null)
            {
                return GetDoubleEvent(this);
            }
            else if (TheProperty != null)
            {
                MethodInfo mi = TheProperty.GetGetMethod();
                object[] prm = new Object[0];
                try
                {
                    return (double)mi.Invoke(ObjectWithDouble, prm);
                }
                catch (TargetInvocationException) { }
            }
            return internalValue;
        }

        private bool settingChanged;
        public double DoubleValue
        {
            get { return GetDouble(); }
            set
            {
                SetDouble(value);
                SetText(value);
            }
        }
        /// <summary>
        /// Der Besitzer dieses Objektes muss GeoPointChanged aufrufen, um eine neue Anzeige zu erzwingen.
        /// </summary>
        public void DoubleChanged()
        {
            if (IsSetting) return; // wir sind selbst der Urheber
            double d = GetDouble();
            if (specialValues != null)
            {
                foreach (KeyValuePair<double, string> kv in specialValues)
                {
                    if (kv.Key == d)
                    {
                        text = StringTable.GetString(kv.Value);
                        if (text.StartsWith("!")) text = text.Substring(1);
                        return;
                    }
                }
            }
            SetText(d);
        }

        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.Refresh ()"/>
        /// </summary>
        public override void Refresh()
        {
            DoubleChanged();
        }
        #region IShowPropertyImpl Overrides
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.LabelType"/>
        /// </summary>
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                ShowPropertyLabelFlags res;
                if (contextMenuId != null)
                    res = base.LabelType | ShowPropertyLabelFlags.ContextMenu;
                else
                    res = base.LabelType;
                if (highlight) res |= ShowPropertyLabelFlags.Highlight;
                return res;
            }
        }
        public override PropertyEntryType Flags
        {
            get
            {
                PropertyEntryType res = PropertyEntryType.Selectable | PropertyEntryType.ValueEditable;
                if (contextMenuId != null) res |= PropertyEntryType.ContextMenu;
                if (highlight) res |= PropertyEntryType.Highlight;
                return res;
            }
        }
        public override string Value
        {
            get
            {
                // SetText(internalValue); no! internalValue is not up to date
                return text;
            }
        }
        private bool Decode(string txt)
        {
            bool success = false;
            double d = 0.0;
            string trimmed;
            trimmed = txt.Trim();
            try
            {
                d = double.Parse(trimmed, numberFormatInfo);
                success = true;
            }
            catch (FormatException)
            {
                if (trimmed == "-") success = true; // allow to start with "-"
            }
            catch (System.OverflowException)
            {
            }
            if (!success)
            {
                try
                {
                    //Scripting s = new Scripting();
                    //if (Frame != null)
                    //{
                    //    d = s.GetDouble(Frame.Project.NamedValues, trimmed);
                    //    success = true;
                    //}
                }
                catch //(ScriptingException)
                {
                }
            }
            if (success) SetDouble(d);
            return success;
        }
        private double valueBeforeEdit;
        public override void StartEdit(bool editValue)
        {
            valueBeforeEdit = internalValue;
        }
        public override bool EditTextChanged(string newValue)
        {
            using (Frame.Project.Undo.ContextFrame(this))
            {
                return Decode(newValue);
            }
        }
        public override void EndEdit(bool aborted, bool modified, string newValue)
        {
            if (aborted)
            {
                SetDouble(valueBeforeEdit);
            }
            else if (modified)
            {
                Decode(newValue);
            }
            Frame.Project.Undo.ClearContext();
        }
        public override MenuWithHandler[] ContextMenu
        {
            get
            {
                if (contextMenuId != null)
                {
                    Frame.ContextMenuSource = this;
                    return MenuResource.LoadMenuDefinition(contextMenuId, false, contextMenuHandler);
                }
                return null;
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Added"/>
        /// </summary>
        /// <param name="propertyTreeView"></param>
        public override void Added(IPropertyTreeView propertyTreeView)
        {
            // propertyTreeView.InfoPopup.Add(textBox, resourceId);
            base.Added(propertyTreeView);
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Removed"/>
        /// </summary>
        /// <param name="propertyTreeView">the IPropertyTreeView from which it was removed</param>
        public override void Removed(IPropertyTreeView propertyTreeView)
        {
            // propertyTreeView.InfoPopup.Remove(textBox);
            base.Removed(propertyTreeView);
        }
        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.Selected ()"/>
        /// </summary>
        public override void Selected()
        {
            base.Selected();
        }
        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.UnSelected ()"/>
        /// </summary>
        public override void UnSelected()
        {
            if (Frame != null)
            {
                Frame.Project.Undo.ClearContext();
            }
            base.UnSelected();
        }
        #endregion

        public void StartModifyWithMouse()
        {
            if (ModifyWithMouseEvent != null)
            {
                ModifyWithMouseEvent(this, true);
            }
        }

        #region ISerializable Members
        /// <summary>
        /// Constructor required by deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected DoublePropertyOld(SerializationInfo info, StreamingContext context)
        {
            internalValue = (double)info.GetValue("InternalValue", typeof(double));
            resourceId = (string)info.GetValue("resourceId", typeof(string));
            settingName = (string)info.GetValue("SettingName", typeof(string));
            try
            {
                minValue = info.GetDouble("MinValue");
                maxValue = info.GetDouble("MaxValue");
            }
            catch (SerializationException)
            {
                minValue = double.MinValue;
                maxValue = double.MaxValue;
            }
        }
        /// <summary>
        /// Implements <see cref="ISerializable.GetObjectData"/>
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (<see cref="System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        virtual public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("InternalValue", internalValue, internalValue.GetType());
            info.AddValue("resourceId", resourceId, resourceId.GetType());
            info.AddValue("SettingName", settingName, settingName.GetType());
            info.AddValue("MinValue", minValue);
            info.AddValue("MaxValue", maxValue);
        }
        #endregion
        #region IDeserializationCallback Members
        void IDeserializationCallback.OnDeserialization(object sender)
        {
            numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            if (Settings.GlobalSettings != null)
            {   // leider gibt es das noch nicht, wenn GlobalSettings gelesen werden
                int decsym = Settings.GlobalSettings.GetIntValue("Formatting.Decimal", 0); // Systemeinstellung | Punkt | Komma
                // wenn 0, dann unverändert
                if (decsym == 1) numberFormatInfo.NumberDecimalSeparator = ".";
                else if (decsym == 2) numberFormatInfo.NumberDecimalSeparator = ",";
            }
            numberFormatInfo.NumberDecimalDigits = 3;

            IsSetting = false;
            UserData = new UserData();
            //textBox = new ManagedKeysTextbox();
            //textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            //textBox.AcceptsReturn = true;
            //textBox.LostFocus += new EventHandler(OnLostFocus);
            //textBox.KeyUp += new KeyEventHandler(OnKeyUp);
            //textBox.KeyPress += new KeyPressEventHandler(OnKeyPress);
            useContext = true;
            SetText(internalValue);
        }
        #endregion

        #region ISettingChanged Members

        public event CADability.SettingChangedDelegate SettingChangedEvent;

        #endregion

    }

    public class DoubleProperty : EditableProperty<double>, IJsonSerialize
    {
        private NumberFormatInfo numberFormatInfo;

        public DoubleProperty(IFrame frame, string resourceId = null) : base(resourceId, null)
        {
            InitFormat(frame);
        }

        public DoubleProperty(GetValueDelegate getValueDelegate, SetValueDelegate setValueDelegate, IFrame frame, string resourceId = null) : base(getValueDelegate, setValueDelegate, resourceId, null)
        {
            InitFormat(frame);
        }

        public DoubleProperty(object ObjectWithLength, string PropertyName, string resourceId, IFrame frame, bool autoModifyWithMouse = true)
            : base(ObjectWithLength, PropertyName, resourceId, null)
        {
            InitFormat(frame);
        }

        public DoubleProperty(string SettingName, string resourceId, double initialValue, IFrame frame) : base(resourceId, null)
        {
            if (frame != null) InitFormat(frame);
            else
            {
                numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
                numberFormatInfo.NumberDecimalDigits = 29;
            }
            SetValue(initialValue, false);
        }

        private void InitFormat(IFrame frame)
        {
            numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            int decsym = Settings.GlobalSettings.GetIntValue("Formatting.Decimal", 0); // Systemeinstellung | Punkt | Komma
                                                                                       // wenn 0, dann unverändert
            if (decsym == 1) numberFormatInfo.NumberDecimalSeparator = ".";
            else if (decsym == 2) numberFormatInfo.NumberDecimalSeparator = ",";
            if (frame != null)
            {
                numberFormatInfo.NumberDecimalDigits = frame.GetIntSetting("Formatting.GeneralDouble", 3);
            }
            else
            {
                numberFormatInfo.NumberDecimalDigits = 3;
            }
        }

        public void SetDouble(double l)
        {
            SetValue(l, true);
        }
        public double GetDouble()
        {
            return GetValue();
        }
        public void DoubleChanged()
        {
            propertyPage?.Refresh(this);
        }

        public int DecimalDigits { get => numberFormatInfo.NumberDecimalDigits; set => numberFormatInfo.NumberDecimalDigits = value; }

        protected override bool TextToValue(string text, out double val)
        {
            bool success = false;
            if (numberFormatInfo.NumberDecimalSeparator == ".") text = text.Replace(",", ".");
            if (numberFormatInfo.NumberDecimalSeparator == ",") text = text.Replace(".", ",");
            val = 0.0;
            try
            {
                val = double.Parse(text, numberFormatInfo);
                success = true;
            }
            catch (FormatException)
            {
                if (text == "-") success = true; // allow to start with "-"
            }
            catch (OverflowException)
            {
            }
            //Locked = false;
            if (!success)
            {
                try
                {
                    //Scripting s = new Scripting();
                    //IFrame Frame = propertyPage.Frame;
                    //if (Frame != null)
                    //{
                    //    if (numberFormatInfo.NumberDecimalSeparator == ",") text = text.Replace(",", "."); // this is ambiguous: there might be function calls with commas, 
                    //    // which are destroyed here, but I don't know what to do. When using formulas, the user should be forced to have "." as decimal separator
                    //    val = s.GetDouble(Frame.Project.NamedValues, text);
                    //    success = true;
                    //}
                }
                catch //(ScriptingException)
                {
                }
            }
            return success;

        }
        protected override string ValueToText(double val)
        {
            if (numberFormatInfo.NumberDecimalDigits == 29)
            {
                if (Math.Abs(val)<1.0) return val.ToString("0.#########################", numberFormatInfo);
                else return val.ToString("G29", numberFormatInfo);
            }
            return val.ToString("f", numberFormatInfo);
        }

        #region deprecated adaption to old implementation of DoubleProperty
        [Obsolete("Parameter autoModifyWithMouse no longer supported, use DoubleProperty(IFrame frame, string resourceId) instead")]
        public DoubleProperty(string resourceId, IFrame frame) : this(frame, resourceId)
        {
        }
        [Obsolete("Use DoubleProperty.GetDouble or DoubleProperty.SetDouble instead")]
        public double DoubleValue
        {
            get { return GetDouble(); }
            set
            {
                SetDouble(value);
            }
        }
        [Obsolete("use DoubleProperty.SetValueDelegate instead")]
        public delegate void SetDoubleDelegate(DoubleProperty sender, double l);
        [Obsolete("use DoubleProperty.GetValueDelegate instead")]
        public delegate double GetDoubleDelegate(DoubleProperty sender);
        [Obsolete("use delegate DoubleProperty.OnSetValue instead")]
        public event SetDoubleDelegate SetDoubleEvent
        {
            add
            {
                base.OnSetValue = delegate (double l) { value(this, l); };
            }
            remove
            {
                base.OnSetValue = null;
            }
        }
        [Obsolete("use delegate DoubleProperty.OnGetValue instead")]
        public event GetDoubleDelegate GetDoubleEvent
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
        [Obsolete("use delegate DoubleProperty.ModifyWithMouse instead")]
        public event ModifyWithMouseDelegate ModifyWithMouseEvent
        {
            add
            {
                ModifyWithMouse = delegate (IShowProperty pe, bool start)
                {
                    value(this, start);
                };
            }
            remove
            {
                ModifyWithMouse = null;
            }
        }
        [Obsolete("use delegate DoubleProperty.LockedChanged instead")]
        public event LockedChangedDelegate LockedChangedEvent
        {
            add
            {
                LockedChanged = delegate (bool locked)
                {
                    value(locked);
                };
            }
            remove
            {
                LockedChanged = null;
            }
        }
        [Obsolete("method has no functionality, remove this call")]
        public void CheckMouseButton(bool Check) { }
        public delegate void LabelChangedDelegate(DoubleProperty sender, string newLabel);
        [Obsolete("use delegate DoubleProperty.LabelTextChanged instead")]
        public event LabelChangedDelegate LabelChangedEvent;
        #endregion
        #region IJsonSerialize implementation (when this property is used as a setting)
        /// <summary>
        /// Empty constructor for IJsonSerialize  deserialisation
        /// </summary>
        protected DoubleProperty()
        {
            numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            numberFormatInfo.NumberDecimalDigits = 29;
        }
        void IJsonSerialize.GetObjectData(IJsonWriteData data)
        {
            data.AddProperty("ResourceId", resourceId);
            data.AddProperty("Value", GetValue());
        }
        void IJsonSerialize.SetObjectData(IJsonReadData data)
        {
            resourceId = data.GetProperty<string>("ResourceId");
            SetValue(data.GetProperty<double>("Value"), false);
        }
        #endregion
    }

}
