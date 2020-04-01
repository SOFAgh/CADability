using CADability.Attribute;
using CADability.GeoObject;
using System;
using System.Drawing;
using System.Reflection;

namespace CADability.UserInterface
{

    public class ColorSelectionProperty : MultipleChoiceProperty
        , IPropertyEntry
    {
        private IGeoObject toWatch;
        private ColorList colorList;
        private PropertyInfo propertyInfo;
        private object objectWithProperty;
        private ColorList.StaticFlags useFlags;
        private IColorDef iColorDef;
        private ColorDef selectedCD;
        private bool showUnselectedGray;
        public ColorSelectionProperty(string resourceId, ColorList clrTable, ColorDef select, ColorList.StaticFlags flags)
        {
            selectedCD = select;
            useFlags = flags;
            flags = clrTable.Flags;
            clrTable.Flags = useFlags;
            base.resourceId = resourceId;
            colorList = clrTable;
            choices = clrTable.Names;
            ExtendChoices();
            if (select != null)
            {
                selectedText = select.Name;
            }
            else
            {
                selectedText = unselectedText;
            }
            clrTable.Flags = flags;
            unselectedText = StringTable.GetString("ColorDef.Undefined");
            showUnselectedGray = true;
        }
        public ColorSelectionProperty(IColorDef iColorDef, string resourceId, ColorList clrTable, ColorList.StaticFlags flags)
        {
            useFlags = flags;
            flags = clrTable.Flags;
            clrTable.Flags = useFlags;
            base.resourceId = resourceId;
            colorList = clrTable;
            ColorDef selectedColor = iColorDef.ColorDef;
            selectedCD = selectedColor;
            this.iColorDef = iColorDef;
            choices = clrTable.Names;
            ExtendChoices();
            if (selectedColor != null)
            {
                selectedText = selectedColor.Name;
            }
            else
            {
                selectedText = unselectedText;
            }
            clrTable.Flags = flags;
            toWatch = iColorDef as IGeoObject; // kann natürlich null sein
            showUnselectedGray = true;
        }
        private void ExtendChoices()
        {
            if ((useFlags & ColorList.StaticFlags.allowUndefined) != 0)
            {
                unselectedText = StringTable.GetString("ColorDef.Undefined");
                // sollte es den Namen schon geben, werden solange - davor und dahintergemacht, bis es den Namen mehr gibt
                while (colorList.Find(unselectedText) != null) unselectedText = "-" + unselectedText + "-";
                choices = new string[colorList.Names.Length + 1];
                colorList.Names.CopyTo(choices, 1);
                choices[0] = unselectedText;
            }
        }
        public ColorSelectionProperty(object go, string propertyName, string resourceId, ColorList clrTable, ColorList.StaticFlags flags)
        {
            useFlags = flags;
            flags = clrTable.Flags;
            clrTable.Flags = useFlags;
            base.resourceId = resourceId;
            colorList = clrTable;
            choices = clrTable.Names;

            objectWithProperty = go;
            propertyInfo = objectWithProperty.GetType().GetProperty(propertyName);
            MethodInfo mi = propertyInfo.GetGetMethod();
            object[] prm = new object[0];
            ColorDef selectedColor = (ColorDef)mi.Invoke(objectWithProperty, prm);
            if (selectedColor != null)
            {
                selectedText = selectedColor.Name;
            }
            selectedCD = selectedColor;
            clrTable.Flags = flags;
            unselectedText = StringTable.GetString("ColorDef.Undefined");
            toWatch = go as IGeoObject; // kann natürlich null sein
        }
        public override void SetSelection(int toSelect)
        {
            if (toSelect >= 0 && toSelect < choices.Length)
                selectedCD = colorList[toSelect];
            base.SetSelection(toSelect);
        }
        public bool ShowAllowUndefinedGray
        {
            get
            {
                return showUnselectedGray;
            }
            set
            {
                showUnselectedGray = value;
            }
        }
        protected override void OnSelectionChanged(string selected)
        {
            ColorList.StaticFlags flags = colorList.Flags;
            colorList.Flags = useFlags;
            ColorDef found = colorList.Find(selected);
            if (iColorDef != null)
            {
                iColorDef.ColorDef = found;
            }
            else if (propertyInfo != null)
            {
                MethodInfo mi = propertyInfo.GetSetMethod();
                object[] prm = new object[1];
                prm[0] = colorList.Find(selected);
                mi.Invoke(objectWithProperty, prm);
            }
            else if (ColorDefChangedEvent != null)
            {
                ColorDefChangedEvent(found);
            }
            base.OnSelectionChanged(selected);
            colorList.Flags = flags;
        }
        private void colorList_DidModify(object sender, EventArgs args)
        {
            Choices = colorList.Names;
            ExtendChoices();
            ColorDef selectedColor = null;
            if (iColorDef != null)
            {
                selectedColor = iColorDef.ColorDef;
            }
            else if (propertyInfo != null)
            {
                MethodInfo mi = propertyInfo.GetGetMethod();
                object[] prm = new object[0];
                selectedColor = (ColorDef)mi.Invoke(objectWithProperty, prm);
            }
            if (selectedColor != null)
            {
                selectedText = selectedColor.Name;
            }
            else if (selectedCD != null)
                selectedText = selectedCD.Name;
        }
        public delegate void ColorDefChangedDelegate(ColorDef selected);
        public event ColorDefChangedDelegate ColorDefChangedEvent;
        private void GeoObjectDidChange(IGeoObject Sender, GeoObjectChange Change)
        {
            if (Sender == toWatch && Change.OnlyAttributeChanged && propertyPage != null)
            {
                if ((Change as GeoObjectChange).MethodOrPropertyName == "ColorDef" ||
                    (Change as GeoObjectChange).MethodOrPropertyName == "Style")
                {
                    if ((toWatch as IColorDef).ColorDef != null) selectedCD = (toWatch as IColorDef).ColorDef;
                    else selectedCD = null;
                    if (propertyPage != null) propertyPage.Refresh(this);
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
                iColorDef = value as IColorDef;
                if (toWatch != null) toWatch.DidChangeEvent += new ChangeDelegate(GeoObjectDidChange);
            }
        }
#region IPropertyEntry
        public override void Added(IPropertyPage pp)
        {
            base.Added(pp);
            colorList.DidModifyEvent += new DidModifyDelegate(colorList_DidModify);
            if (toWatch != null) toWatch.DidChangeEvent += new ChangeDelegate(GeoObjectDidChange);
        }
        public override void Removed(IPropertyPage propertyTreeView)
        {
            base.Removed(propertyTreeView);
            colorList.DidModifyEvent -= new DidModifyDelegate(colorList_DidModify);
            if (toWatch != null) toWatch.DidChangeEvent -= new ChangeDelegate(GeoObjectDidChange);
        }
        string[] IPropertyEntry.GetDropDownList()
        {
            string[] res = new string[colorList.Count];
            for (int i = 0; i < colorList.Count; i++)
            {
                res[i] = "[[ColorBox:" + colorList[i].Color.R.ToString() + ":" + colorList[i].Color.G.ToString() + ":" + colorList[i].Color.B.ToString() + "]]" + colorList[i].Name;
            }
            return res;
        }
        string IPropertyEntry.Value
        {
            get
            {
                if (selectedCD == null) return "";
                return "[[ColorBox:" + selectedCD.Color.R.ToString() + ":" + selectedCD.Color.G.ToString() + ":" + selectedCD.Color.B.ToString() + "]]" + selectedCD.Name;
            }
        }
        public override void ListBoxSelected(int selectedIndex)
        {
            if (selectedIndex < 0) return;
            selectedCD = colorList[selectedIndex];
            if (iColorDef != null)
            {
                iColorDef.ColorDef = selectedCD;
            }
            else if (propertyInfo != null)
            {
                MethodInfo mi = propertyInfo.GetSetMethod();
                object[] prm = new object[1];
                prm[0] = selectedCD;
                mi.Invoke(objectWithProperty, prm);
            }
            else if (ColorDefChangedEvent != null)
            {
                ColorDefChangedEvent(selectedCD);
            }
            if (propertyPage != null) propertyPage.Refresh(this);
        }

#endregion

    }
}
