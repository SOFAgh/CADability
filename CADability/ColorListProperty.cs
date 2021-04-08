using CADability.Attribute;
using System;
#if WEBASSEMBLY
using CADability.WebDrawing;
using Point = CADability.WebDrawing.Point;
#else
using System.Drawing;
using Point = System.Drawing.Point;
#endif

namespace CADability.UserInterface
{
    /// <summary>
    /// 
    /// </summary>

    public class ColorListProperty : IShowPropertyImpl, ICommandHandler
        , IPropertyEntry
    {
        private ColorList colorList; // das übergeordnete Objekt
        private int index; // der Index in der ColorList
        private bool changeStyle;
        public ColorListProperty(ColorList ColorList, int Index)
        {
            colorList = ColorList;
            index = Index;
            changeStyle = true;
            base.resourceId = "ColorName";
        }
#region Die Implementierung von IShowProperty
        public override string LabelText
        {
            get
            {
                return colorList.GetName(index);
            }
        }
        public override MenuWithHandler[] ContextMenu
        {
            get
            {
                return MenuResource.LoadMenuDefinition("MenuId.ColorListEntry", false, this);
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.LabelType"/>
        /// </summary>
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                ShowPropertyLabelFlags flags = ShowPropertyLabelFlags.Selectable | ShowPropertyLabelFlags.Editable | ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.ContextMenu;
                if (colorList.CurrentIndex == index)
                    flags |= ShowPropertyLabelFlags.Bold;
                return flags;
            }
        }
        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.LabelChanged (string)"/>
        /// </summary>
        /// <param name="NewText"></param>
		public override void LabelChanged(string NewText)
        {
            if (!colorList.IsStatic(index))
                colorList.SetName(index, NewText);
        }
        #endregion
#region ICommandHandler Members
        private void OnClick(object sender, EventArgs e)
        {
            if (!colorList.IsStatic(index))
            {
                changeStyle = false;
                Color color = colorList.GetColor(index);
               
                if (Frame.UIService.ShowColorDialog(ref color) == Substitutes.DialogResult.OK)
                {
                    colorList.SetColor(index, color);
                }
                changeStyle = true;
            }
        }
        bool ICommandHandler.OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.ColorListEntry.Delete":
                    colorList.RemoveAt(index);
                    propertyTreeView.Refresh(colorList);
                    return true;
                case "MenuId.ColorListEntry.Current":
                    colorList.CurrentIndex = index;
                    propertyTreeView.Refresh(colorList);
                    return true;
                case "MenuId.ColorListEntry.ChangeColor":
                    OnClick(null, null);
                    return true;
                case "MenuId.ColorListEntry.Edit":
                    this.StartEdit(false);
                    return true;
            }
            return false;
        }
        bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            switch (MenuId)
            {
                case "MenuId.ColorListEntry.Delete":
                    if (colorList.CurrentIndex == index)
                        CommandState.Enabled = false;
                    break;
                case "MenuId.ColorListEntry.Current":
                    if (colorList.CurrentIndex == index)
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
            return true;
            // TODO: betreffende MenueIds behandeln
            return false;
        }
        void ICommandHandler.OnSelected(MenuWithHandler selectedMenuItem, bool selected) { }
        #endregion

        #region IPropertyEntry
        protected override bool HasDropDownButton => true;
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
                if (index < 0) return "";
                return "[[ColorBox:" + colorList[index].Color.R.ToString() + ":" + colorList[index].Color.G.ToString() + ":" + colorList[index].Color.B.ToString() + "]]" + colorList[index].Name;
            }
        }
        public override void ListBoxSelected(int selectedIndex)
        {
        }
#endregion
    }
}
