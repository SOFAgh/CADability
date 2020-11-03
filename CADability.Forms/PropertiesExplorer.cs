using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CADability.Forms
{

    public class PropertiesExplorer : Control, IControlCenter
    {
        private List<PropertyPage> tabPages; // the TabPages with titleId and iconId
        TabControl tabControl; // the TabControl containing the tab pages
        public IFrame Frame { get; set; }
        HashSet<string> entriesToHide;
        public PropertiesExplorer()
        {
            tabPages = new List<PropertyPage>();

            listBox = new ListBox(); // a ListBox which only becomes visible (and positioned and filled) when a dropdown button of an PropertyEntry has been clicked
            listBox.Visible = false;
            listBox.LostFocus += ListBox_LostFocus;
            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            listBox.MouseClick += ListBox_MouseClick;
            listBox.DrawMode = DrawMode.OwnerDrawFixed;
            listBox.Name = "the list box";
            listBox.TabStop = false;
            listBox.DrawItem += ListBox_DrawItem;

            textBox = new TextBox();
            textBox.Visible = false;
            textBox.KeyUp += TextBox_KeyUp;
            textBox.Name = "the text box";
            textBox.TabStop = false;

            labelExtension = new Label();
            labelExtension.Visible = false;
            labelExtension.Name = "the label extension";
            labelExtension.TabStop = false;
            labelExtension.Paint += LabelExtension_Paint;

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Name = "the tab control";
            tabControl.TabStop = false; // we have our own tab handling mechanism
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(27, 21);
            tabControl.DrawItem += new DrawItemEventHandler(tabControl_DrawItem);
            tabControl.ShowToolTips = true;
            
            Controls.Add(tabControl);
            Controls.Add(listBox);
            Controls.Add(textBox);
            Controls.Add(labelExtension);

            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);

            System.Drawing.Bitmap bmp = BitmapTable.GetBitmap("Icons.bmp");
            Color clr = bmp.GetPixel(0, 0);
            if (clr.A != 0) bmp.MakeTransparent(clr);
            imageList.Images.AddStrip(bmp);
            tabControl.ImageList = imageList;

            entriesToHide = new HashSet<string>();
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (EntryWithTextBox != null && !ActivePropertyPage.ContainsEntry(EntryWithTextBox))
            {
                EntryWithTextBox.EndEdit(true, textBox.Modified, textBox.Text);
                HideTextBox();
            }
            if (EntryWithListBox != null && !ActivePropertyPage.ContainsEntry(EntryWithTextBox))
            {
                HideListBox();
            }
            ActivePropertyPage.SelectEntry(ActivePropertyPage.GetCurrentSelection()); // this shows the textbox or listbox
        }
        private static PropertyPage GetPropertyPage(IPropertyEntry propertyEntry)
        {
            IPropertyEntry pe = propertyEntry;
            while (pe != null)
            {
                if (pe.Parent is IPropertyPage propertyPage) return propertyPage as PropertyPage;
                else pe = pe.Parent as IPropertyEntry;
            }
            return null;
        }

        #region listBox 
        ListBox listBox; // the only listbox, which is hidden and, when needed, moved and filled befor it is shown
        public void ShowListBox(Rectangle screenLocation, string[] items, int selected, IPropertyEntry sender)
        {
            if (EntryWithListBox == sender) return;
            EntryWithListBox = sender;
            int height = items.Length * screenLocation.Height + 2 * SystemInformation.BorderSize.Height + listBox.Margin.Top + listBox.Margin.Bottom;
            Rectangle entryRect = RectangleToClient(screenLocation);
            Rectangle bounds;
            if (entryRect.Top > ClientRectangle.Height - entryRect.Bottom)
            {
                // place the list box above the entry
                height = Math.Min(height, entryRect.Top);
                bounds = new Rectangle(entryRect.Left, entryRect.Top - height, entryRect.Width, height);
            }
            else
            {
                // place the list box below the entry
                height = Math.Min(height, ClientRectangle.Height - entryRect.Bottom);
                bounds = new Rectangle(entryRect.Left, entryRect.Bottom, entryRect.Width, height);
            }
            listBox.Items.AddRange(items);
            listBox.Visible = true;
            listBox.BringToFront();
            listBox.ItemHeight = screenLocation.Height;
            listBox.SelectedIndex = selected;
            listBox.Bounds = bounds;
            listBox.Show();
            listBox.Focus();
        }
        public void HideListBox()
        {
            if (EntryWithListBox != null)
            {
                PropertyPage pp = GetPropertyPage(EntryWithListBox);
                IPropertyEntry elb = EntryWithListBox;
                EntryWithListBox = null;
                pp?.Refresh(elb);
            }
            listBox.Hide();
            listBox.Items.Clear();
        }
        private void ListBox_MouseClick(object sender, MouseEventArgs e)
        {
            EntryWithListBox.ListBoxSelected(listBox.SelectedIndex);
            HideListBox();
        }
        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // we (currently) do not react on a simple change (e.g. with the arrow keys) of the selection, only when clicked or enter pressed we notify the entry
        }
        private void ListBox_LostFocus(object sender, EventArgs e)
        {
            if (EntryWithListBox != null)
            {   // nothong has been selected, the user clicked somwhere else
                HideListBox();
            }
        }
        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            string text = listBox.Items[e.Index].ToString();
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
                PropertyPage.DrawString(e.Graphics, text, this.Font, SystemBrushes.HighlightText, e.Bounds, e.State == DrawItemState.Checked);
            }
            else
            {
                e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
                PropertyPage.DrawString(e.Graphics, text, this.Font, SystemBrushes.ControlText, e.Bounds, e.State == DrawItemState.Checked);
            }
        }
        #endregion

        #region textBox
        TextBox textBox; // there is only one TextBox for editing labels or values. It is normally hidden and moved, filled and activated when needed
        public void ShowTextBox(Rectangle screenLocation, string initialText, IPropertyEntry sender, Point screenClickPos)
        {
            if (EntryWithTextBox != null) EntryWithTextBox.EndEdit(true, textBox.Modified, textBox.Text);
            EntryWithTextBox = sender;
            textBox.Text = initialText;
            textBox.Modified = false;
            textBox.Visible = true;
            textBox.Bounds = RectangleToClient(screenLocation);
            textBox.BringToFront();
            int lastInd = Math.Max(initialText.Length - 1, 0);
            Point lastCharPos = textBox.GetPositionFromCharIndex(initialText.Length - 1);
            int charWidth = textBox.Font.Height * 5 / 7; // some guess
            if (initialText.Length > 0) charWidth = lastCharPos.X / initialText.Length;
            Point clickPos = textBox.PointToClient(screenClickPos);
            int charindex = textBox.GetCharIndexFromPosition(new Point(clickPos.X + charWidth / 2, clickPos.Y));
            if (clickPos.X > lastCharPos.X + charWidth)
            {   // clicked behind the last character: select all
                textBox.SelectAll();
            }
            else
            {   // clicked somewhere inside the text
                textBox.Select(charindex, 0);
            }
            textBox.Show();
            textBox.Focus();
        }
        public void HideTextBox()
        {
            // we cannot call EntryWithTextBox.EndEdit, because we don't know, whether it was aborted or not.
            // EntryWithTextBox.EndEdit must be called where Tab, Enter or click occurs
            if (EntryWithTextBox != null)
            {
                PropertyPage pp = GetPropertyPage(EntryWithTextBox);
                IPropertyEntry etb = EntryWithTextBox;
                EntryWithTextBox = null;
                pp?.Refresh(etb);
            }
            textBox.Hide();
        }
        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (EntryWithTextBox != null && e.KeyCode != Keys.Tab && textBox.Modified)
            {   // EditTextChanged fixes the input of a ConstructAction, i.e. the mouse-move is not forwarded to this input any more
                // so if you start entering text into a text-box you don't want the mouse-movement to alter this text.
                // Now using the member variable textBoxIsUpdatingFromKeystroke, which is true, when a change happens because of a keystroke
                // in the text-box.
                textBoxIsUpdatingFromKeystroke = true;
                try
                {
                    bool ok = EntryWithTextBox.EditTextChanged(textBox.Text);
                    textBox.Modified = false; // so on EndEdit we do not update the same value twice
                    // curly red underlines when OK is false?
                }
                finally
                {
                    textBoxIsUpdatingFromKeystroke = false;
                }

            }
        }
        bool textBoxIsUpdatingFromKeystroke = false;
        #endregion

        #region labelextension
        Label labelExtension; // there is only one LabelExtension for editing labels or values. It is normally hidden and moved, filled and activated when needed
        public void ShowLabelExtension(Rectangle screenLocation, string initialText, IPropertyEntry sender)
        {
            EntryWithLabelExtension = sender;
            labelExtension.Font = textBox.Font;
            labelExtension.BackColor = textBox.BackColor;
            labelExtension.Text = initialText;
            labelExtension.Visible = true;
            labelExtension.Bounds = RectangleToClient(screenLocation);
            labelExtension.BringToFront();
            labelExtension.Show();
            labelExtension.Focus();
        }
        public void HideLabelExtension()
        {
            EntryWithLabelExtension = null;
            labelExtension.Hide();
        }
        private void LabelExtension_Paint(object sender, PaintEventArgs e)
        {
            StringFormat stringFormat = new StringFormat(StringFormat.GenericDefault);
            stringFormat.FormatFlags = StringFormatFlags.NoWrap;
            stringFormat.LineAlignment = StringAlignment.Center;
            stringFormat.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(EntryWithLabelExtension.Label, Font, SystemBrushes.ControlText, labelExtension.Bounds, stringFormat);
        }
        #endregion

        private void tabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                TabPage tp = tabControl.TabPages[e.Index];
                Image img = tabControl.ImageList.Images[tp.ImageIndex];
                int x = e.Bounds.Left + (e.Bounds.Width - img.Width) / 2;
                int y = e.Bounds.Top + (e.Bounds.Height - img.Height) / 2;
                if ((e.State & DrawItemState.Selected) == 0) y += 2;
                Brush brush;
                if ((e.State & DrawItemState.Selected) == 0)
                    brush = new SolidBrush(SystemColors.Control);
                else
                    brush = new SolidBrush(SystemColors.Window);
                e.Graphics.FillRectangle(brush, e.Bounds);
                brush.Dispose();
                e.Graphics.DrawImageUnscaled(img, x, y);
            }
            catch (ArgumentException) { }
        }

        public IPropertyPage AddPropertyPage(string titleId, int iconId)
        {
            PropertyPage res = new PropertyPage(titleId, iconId);
            tabPages.Add(res);
            tabControl.TabPages.Add(res);
            res.Frame = Frame;
            res.ImageIndex = iconId;
            res.ToolTipText = StringTable.GetString(titleId);

            return res;
        }
        public IPropertyPage ActivePropertyPage
        {
            get
            {
                PropertyPage selected = tabControl.SelectedTab as PropertyPage;
                return selected;
            }
        }
        public bool SelectNextEntry(bool forward)
        {
            PropertyPage sel = ActivePropertyPage as PropertyPage;
            if (sel != null)
            {
                sel.SelectNextPropertyEntry(forward);
            }
            return true;
        }
        public IPropertyPage GetPropertyPage(string titleId)
        {
            for (int i = 0; i < tabPages.Count; i++)
            {
                if (tabPages[i].TitleId == titleId) return tabPages[i];
            }
            return null;
        }
        public void DisplayHelp(string helpID)
        {
            throw new NotImplementedException();
        }
        public bool ShowPropertyPage(string titleId)
        {
            for (int i = 0; i < tabPages.Count; i++)
            {
                if (tabPages[i].TitleId == titleId)
                {
                    tabControl.SelectedIndex = i;
                    return true;
                }
            }
            return false;
        }
        public void Refresh(IPropertyEntry toRefresh)
        {
            // we get here e.g. when a textbox is open and we start to enter a value. then in the construct action the "highlight" flag is removed and
            // refresh is called. But we are in the middle of entering text and changing the value would not be desired.
            if (EntryWithTextBox == toRefresh && textBox.Visible && !textBoxIsUpdatingFromKeystroke)
            {
                bool allSelected = textBox.SelectionLength == textBox.Text.Length;
                textBox.Text = toRefresh.Value;
                textBox.Modified = false;
                if (allSelected) textBox.SelectAll();
                textBox.Update(); // to show smooth updateing of the text when the mouse is moving
            }
        }
        public void UnSelected(IPropertyEntry unselected)
        {   // the entry with the textbox or listbox is beeing unselected (by as mouseclick on a different entry), so we accept the input as valid (not aborted)
            if (EntryWithTextBox == unselected)
            {
                EntryWithTextBox.EndEdit(false, textBox.Modified, textBox.Text);
                HideTextBox();
            }
            else if (EntryWithListBox == unselected)
            {
                EntryWithListBox.ListBoxSelected(listBox.SelectedIndex);
                HideListBox();
            }
        }
        public void Selected(IPropertyEntry selectedEntry)
        {
            if (EntryWithTextBox != null && EntryWithTextBox != selectedEntry) HideTextBox();
            if (EntryWithListBox != null && EntryWithListBox != selectedEntry) HideListBox();
        }

        public void PreProcessKeyDown(Substitutes.KeyEventArgs e)
        {   // this is beeing called after the ActiveAction has preprocessed (and not handled) the key down message
            // we can handle it here, before it gets handled by maybe and edit text box or something else (menu?)
            switch (e.KeyData) // was KeyCode, but didn't work
            {
                case Substitutes.Keys.Tab:
                    if (e.Control)
                    {   // we use the Ctrl key to switch between property pages
                        int sel = -1;
                        for (int i = 0; i < tabControl.TabPages.Count; i++)
                        {
                            if (tabControl.TabPages[i] == tabControl.SelectedTab)
                            {
                                sel = i;
                                break;
                            }
                        }
                        if (sel >= 0)
                        {
                            if (e.Shift) sel = (sel + tabControl.TabPages.Count - 1) % tabControl.TabPages.Count;
                            else sel = (sel + 1) % tabControl.TabPages.Count;
                            tabControl.SelectedTab = tabControl.TabPages[sel];
                        }
                    }
                    else
                    {
                        if (EntryWithTextBox != null)
                        {
                            EntryWithTextBox.EndEdit(false, textBox.Modified, textBox.Text);
                            HideTextBox();
                        }
                        else if (EntryWithListBox != null)
                        {
                            EntryWithListBox.ListBoxSelected(listBox.SelectedIndex);
                            HideListBox();
                        }
                        SelectNextEntry(!e.Shift);
                    }
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Substitutes.Keys.Enter:
                    if (EntryWithTextBox != null)
                    {
                        EntryWithTextBox.EndEdit(false, textBox.Modified, textBox.Text);
                        HideTextBox();
                    }
                    else if (EntryWithListBox != null)
                    {
                        EntryWithListBox.ListBoxSelected(listBox.SelectedIndex);
                        HideListBox();
                    }
                    else
                    {
                        (ActivePropertyPage as PropertyPage).OnEnter();
                    }
                    e.Handled = true;
                    break;
                case Substitutes.Keys.Escape:
                    if (EntryWithTextBox != null)
                    {
                        EntryWithTextBox.EndEdit(true, textBox.Modified, textBox.Text);
                        HideTextBox();
                        e.Handled = true;
                    }
                    else if (EntryWithListBox != null)
                    {
                        // no notification of the entry, nothing has changed until now
                        HideListBox();
                        e.Handled = true;
                    }
                    break;
                case Substitutes.Keys.Down:
                    SelectNextEntry(true);
                    e.Handled = true;
                    break;
                case Substitutes.Keys.Up:
                    SelectNextEntry(false);
                    e.Handled = true;
                    break;
            }
        }

        public void HideEntry(string entryId, bool hide)
        {
            if (hide) entriesToHide.Add(entryId);
            else entriesToHide.Remove(entryId);
        }
        public bool isHidden(string entryId)
        {
            return entriesToHide.Contains(entryId);
        }
        public IPropertyEntry EntryWithListBox { get; private set; }
        public IPropertyEntry EntryWithTextBox { get; private set; }
        public IPropertyEntry EntryWithLabelExtension { get; private set; }
    }
}

