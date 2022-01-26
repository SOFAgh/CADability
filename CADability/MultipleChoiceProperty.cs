using System;
using System.ComponentModel;
using System.Drawing;

using Wintellect.PowerCollections;

namespace CADability.UserInterface
{

    /* derived:
    public class LinePatternSelectionProperty : MultipleChoiceProperty
    public class LayerSelectionProperty : MultipleChoiceProperty
    public class ColorSelectionProperty : MultipleChoiceProperty
    public class BooleanProperty : MultipleChoiceProperty, ISerializable, IDeserializationCallback, ISettingChanged, IJsonSerialize, IJsonSerializeDone
    public class StyleSelectionProperty : MultipleChoiceProperty
    public class DimensionStyleSelectionProperty : MultipleChoiceProperty
    public class LineWidthSelectionProperty : MultipleChoiceProperty
    public class HatchStyleSelectionProperty : MultipleChoiceProperty
    public class MultipleChoiceSetting : MultipleChoiceProperty, ISerializable, ISettingChanged
     * */

    public class MultipleChoiceProperty : PropertyEntryImpl
    {
        protected string propertyLabelText; // der Text auf der linken Seite
        internal string[] choices; // die Auswahlmöglichkeiten
        //protected int[] imageIndices; // die Indizes in der ImageList, selbe Reihenfolge wie choices
        //protected ImageList images; // die ImageList (kann fehlen)
        protected string selectedText; // der ausgewählte Text
        protected string unselectedText; // der Text, wenn nichts ausgewählt ist (wenn null, dann ganz leer)
        private bool popup;
        /// <value>
        /// Back reference to any user item. Not used by the MultipleChoiceProperty object itself.
        /// </value>
        public object user;

        /// <summary>
        /// Konstruktor mit Initialisierung.
        /// </summary>
        /// <param name="LabelText">Der Text der Anzeige auf der linken Seite</param>
        /// <param name="Choices">Die Auswahlmöglichkeiten</param>
        public MultipleChoiceProperty(string resourceId, string[] Choices, string InitialSelection)
            : this()
        {
            base.resourceId = resourceId;
            choices = Choices;
            selectedText = InitialSelection;
        }
        public MultipleChoiceProperty(string resourceId, int InitialSelection)
            : this()
        {
            base.resourceId = resourceId;
            choices = StringTable.GetSplittedStrings(resourceId + ".Values");
            if (InitialSelection >= choices.Length) InitialSelection = 0;
            selectedText = choices[InitialSelection];
        }
        /// <summary>
        /// Leerer Konstruktor (für die Verwendung von abgeleiteten Klassen).
        /// wenigstens propertyLabelText und choices müssen gesetzt werden.
        /// </summary>
        public MultipleChoiceProperty()
        {
            popup = true;
        }
        //private int ImageIndex(int Index)
        //{
        //    if (imageIndices != null) return imageIndices[Index];
        //    return -1;
        //}
        //private void fillPopupList()
        //{
        //    if (popupList != null)
        //    {
        //        popupList.RemoveAllItems();
        //        for (int i = 0; i < choices.Length; ++i)
        //        {
        //            popupList.AddItem(choices[i], ImageIndex(i), choices[i]);
        //            if (selectedText == choices[i]) popupList.SelectedIndex = i;
        //        }
        //    }
        //}
        public string[] Choices
        {
            get
            {
                return choices.Clone() as string[];
            }
            set
            {
                choices = value.Clone() as string[];
                //fillPopupList();
            }
        }
        public string SelectedText
        {
            get
            {
                return selectedText;
            }
        }
        public int ChoiceIndex(string choice)
        {
            for (int i = 0; i < choices.Length; ++i)
            {
                if (choices[i] == choice) return i;
            }
            return -1;
        }
        public void SetUnselectedText(string ResourceID)
        {
            unselectedText = StringTable.GetString(ResourceID);
        }
        public delegate void SelectionChangedDelegate(MultipleChoiceProperty sender, EventArgs arg);
        public event SelectionChangedDelegate SelectionChangedEvent;
        public event ValueChangedDelegate ValueChangedEvent;
        public virtual void SetSelection(int toSelect)
        {
            if (toSelect < 0)
            {
                selectedText = null;
            }
            else
            {
                try
                {
                    selectedText = choices[toSelect];
                }
                catch (IndexOutOfRangeException)
                {
                    selectedText = null;
                }
            }
        }
        protected virtual void OnSelectionChanged(string selected)
        {
            if (SelectionChangedEvent != null) SelectionChangedEvent(this, EventArgs.Empty);
            selectedText = selected;
            if (ValueChangedEvent != null) ValueChangedEvent(this, selectedText);
        }
        public int CurrentIndex
        {
            get
            {
                for (int i = 0; i < choices.Length; i++)
                    if (selectedText == choices[i])
                        return i;
                return -1;
            }
        }
        #region IPropertyEntry Members
        public override PropertyEntryType Flags 
        {
            get
            {
                return PropertyEntryType.Selectable | PropertyEntryType.DropDown;
            }
        }
        public override string[] GetDropDownList()
        {
            return Choices;
        }
        public override string Value
        {
            get
            {
                if (selectedText != null) return selectedText;
                if (unselectedText != null) return "[[ColorTxt:128:128:128]]" + unselectedText;
                return "";
            }
        }
        public override void ListBoxSelected(int selectedIndex)
        {
            if (selectedIndex >= 0)
            {
                selectedText = Choices[selectedIndex];
                OnSelectionChanged(selectedText);
                // the following is already done by OnSelectionChanged
                //SelectionChangedEvent?.Invoke(this, EventArgs.Empty);
                //ValueChangedEvent?.Invoke(this, selectedText);
            }
            if (propertyPage != null) propertyPage.Refresh(this);
        }
        #endregion
    }
}
