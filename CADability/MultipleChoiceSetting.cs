using CADability.UserInterface;
using System;
using System.Runtime.Serialization;

namespace CADability
{
    /// <summary>
    /// Wrappes an int value. The value represents a choice of severel predefined choices.
    /// It represents a setting defined by a <see cref="SettingName"/> and a <see cref="Selected"/> value.
    /// Ths setting can be displayed and modified in the control center. 
    /// It is represented as a combo box. The label left of the combo box is given
    /// by the resourceId <see cref="StringTable.GetString"/>, the values are
    /// </summary>
    [Serializable()]
    public class MultipleChoiceSetting : MultipleChoiceProperty, ISerializable, ISettingChanged, IJsonSerialize
    {
        int selected;
        string settingName;
        public MultipleChoiceSetting(string resourceId, string settingName) : base()
        {
            this.resourceId = resourceId;
            this.settingName = settingName;
            base.propertyLabelText = StringTable.GetString(this.resourceId);
            string SelText = StringTable.GetString(resourceId + ".Values");
            base.choices = StringTable.GetSplittedStrings(resourceId + ".Values");
            selected = -1;
        }
        public MultipleChoiceSetting(string resourceId, string settingName, string[] values)
            : base()
        {
            this.resourceId = resourceId;
            this.settingName = settingName;
            base.propertyLabelText = StringTable.GetString(this.resourceId);
            string SelText = StringTable.GetString(resourceId + ".Values");
            base.choices = values;
            selected = -1;
        }
        public int CurrentSelection
        {
            get { return selected; }
            set
            {
                selected = value;
                base.SetSelection(selected);
            }
        }
        public override void SetSelection(int toSelect)
        {
            base.SetSelection(toSelect);
            selected = toSelect;
        }
        #region ISerializable Members
        /// <summary>
        /// Constructor required by deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected MultipleChoiceSetting(SerializationInfo info, StreamingContext context)
        {
            selected = (int)info.GetValue("Selected", typeof(int));
            resourceId = (string)info.GetValue("ResourceId", typeof(string));
            settingName = (string)info.GetValue("SettingName", typeof(string));

            base.propertyLabelText = StringTable.GetString(this.resourceId);
            base.choices = StringTable.GetSplittedStrings(resourceId + ".Values");
            if (selected >= 0 && selected < base.choices.Length) base.selectedText = base.choices[selected];
        }
        /// <summary>
        /// Implements <see cref="ISerializable.GetObjectData"/>
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (<see cref="System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Selected", selected, selected.GetType());
            info.AddValue("ResourceId", resourceId, resourceId.GetType());
            info.AddValue("SettingName", settingName, settingName.GetType());
        }
        public void GetObjectData(IJsonWriteData data)
        {
            data.AddProperty("Selected", selected);
            data.AddProperty("ResourceId", resourceId);
            data.AddProperty("SettingName", settingName);
        }
        public void SetObjectData(IJsonReadData data)
        {
            selected = data.GetProperty<int>("Selected");
            resourceId = data.GetProperty<string>("ResourceId");
            settingName = data.GetProperty<string>("SettingName");

            base.propertyLabelText = StringTable.GetString(this.resourceId);
            base.choices = StringTable.GetSplittedStrings(resourceId + ".Values");
            if (selected >= 0 && selected < base.choices.Length) base.selectedText = base.choices[selected];
        }
        protected MultipleChoiceSetting() { }
        #endregion
        protected override void OnSelectionChanged(string selected)
        {
            base.OnSelectionChanged(selected);
            this.selected = -1;
            for (int i = 0; i < choices.Length; ++i)
            {
                if (choices[i] == selected)
                {
                    this.selected = i;
                    break;
                }
            }
            if (SettingChangedEvent != null) SettingChangedEvent(settingName, this.selected);
        }

        #region ISettingChanged Members

        public event CADability.SettingChangedDelegate SettingChangedEvent;

        #endregion
    }
}
