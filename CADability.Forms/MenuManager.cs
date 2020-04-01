using CADability.UserInterface;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CADability.Forms
{
    internal class ContextMenuWithHandler : ContextMenu
    {
        ICommandHandler commandHandler;
        public string menuID;

        public ContextMenuWithHandler(MenuItem[] MenuItems, ICommandHandler Handler, string menuID) : base(MenuItems)
        {
            commandHandler = Handler;
            this.menuID = menuID;
        }

        public ContextMenuWithHandler(MenuWithHandler[] definition) : base()
        {
            this.menuID = null;
            commandHandler = null;
            MenuItem[] sub = new MenuItem[definition.Length];
            for (int i = 0; i < sub.Length; i++)
            {
                sub[i] = new MenuItemWithHandler(definition[i]);
            }
            base.MenuItems.AddRange(sub);
        }

        private void RecurseCommandState(MenuItemWithHandler miid)
        {
            foreach (MenuItem mi in miid.MenuItems)
            {
                MenuItemWithHandler submiid = mi as MenuItemWithHandler;
                if (submiid != null)
                {
                    if (commandHandler != null)
                    {
                        CommandState commandState = new CommandState();
                        commandHandler.OnUpdateCommand((submiid.Tag as MenuWithHandler).ID, commandState);
                        submiid.Enabled = commandState.Enabled;
                        submiid.Checked = commandState.Checked;
                    }
                    if (submiid.IsParent) RecurseCommandState(submiid);
                }
            }
        }
        public void UpdateCommand()
        {
            foreach (MenuItem mi in MenuItems)
            {
                MenuItemWithHandler miid = mi as MenuItemWithHandler;
                if (mi.Tag is MenuWithHandler menuWithHandler)
                {
                    if (menuWithHandler.Target != null)
                    {
                        CommandState commandState = new CommandState();
                        menuWithHandler.Target.OnUpdateCommand(menuWithHandler.ID, commandState);
                        miid.Enabled = commandState.Enabled;
                        miid.Checked = commandState.Checked;
                        if (miid.IsParent) RecurseCommandState(miid);
                    }
                }
            }
        }
        protected override void OnPopup(EventArgs e)
        {
            UpdateCommand();
            base.OnPopup(e);
        }

        public void SetCommandHandler(ICommandHandler hc)
        {
            foreach (MenuItem mi in MenuItems)
            {
                MenuItemWithHandler submiid = mi as MenuItemWithHandler;
                if (submiid != null)
                {
                    (submiid.Tag as MenuWithHandler).Target = hc;
                    if (submiid.IsParent) SetCommandHandler(submiid, hc);
                }
            }
            this.commandHandler = hc;
        }

        private void SetCommandHandler(MenuItemWithHandler miid, ICommandHandler hc)
        {
            foreach (MenuItem mi in miid.MenuItems)
            {
                MenuItemWithHandler submiid = mi as MenuItemWithHandler;
                if (submiid != null)
                {
                    (submiid.Tag as MenuWithHandler).Target = hc;
                    if (submiid.IsParent) SetCommandHandler(submiid, hc);
                }
            }
        }
        public delegate void MenuItemSelectedDelegate(string menuId);
        public event MenuItemSelectedDelegate MenuItemSelectedEvent;
        public void FireMenuItemSelected(string menuId)
        {
            if (MenuItemSelectedEvent != null) MenuItemSelectedEvent(menuId);
        }

        private bool ProcessShortCut(Keys keys, MenuItem mi)
        {
            if (mi.IsParent)
            {
                foreach (MenuItem mii in mi.MenuItems)
                {
                    if (ProcessShortCut(keys, mii))
                        return true;
                }
            }
            else if (mi.Shortcut == (Shortcut)(int)keys)
            {
                MenuItemWithHandler miid = mi as MenuItemWithHandler;
                CommandState commandState = new CommandState();
                commandHandler.OnUpdateCommand((miid.Tag as MenuWithHandler).ID, commandState);
                if (commandState.Enabled)
                    commandHandler.OnCommand((miid.Tag as MenuWithHandler).ID);
                return true;
            }
            return false;
        }
        internal bool ProcessShortCut(Keys keys)
        {
            foreach (MenuItem mi in MenuItems)
            {
                if (ProcessShortCut(keys, mi))
                    return true;
            }
            return false;
        }
    }

    class MenuItemWithHandler : MenuItem
    {
        private bool doubleChecked; // the MenuItem Checked property behaves strange. Maybe "because it is a field of a marshal-by-reference class" is the problem? // so here is a copy of this flag
        public MenuItemWithHandler(MenuWithHandler definition) : base()
        {
            OwnerDraw = true;
            Text = definition.Text;
            Tag = definition;
            // no need for the following, if Update
            //CommandState commandState = new CommandState();
            //if (definition.Target.OnUpdateCommand(definition.ID, commandState))
            //{
            //    Enabled = commandState.Enabled;
            //    Checked = commandState.Checked;
            //    doubleChecked = commandState.Checked;
            //}
            if (definition.SubMenus != null)
            {
                MenuItem[] sub = new MenuItem[definition.SubMenus.Length];
                for (int i = 0; i < definition.SubMenus.Length; i++)
                {
                    sub[i] = new MenuItemWithHandler(definition.SubMenus[i]);
                }
                MenuItems.AddRange(sub);
            }
        }
        protected override void OnClick(EventArgs e)
        {
            MenuWithHandler definition = (Tag as MenuWithHandler);
            if (definition.Target != null) definition.Target.OnCommand(definition.ID);
        }
        protected override void OnMeasureItem(System.Windows.Forms.MeasureItemEventArgs e)
        {
            MenuWithHandler definition = Tag as MenuWithHandler;
            if (this.Text == "-")
            {
                e.ItemHeight = 3;
            }
            else
            {
                Font f = SystemInformation.MenuFont;
                string toMeassure = this.Text;
                if (!string.IsNullOrEmpty(definition.Shortcut))
                {
                    toMeassure += "    " + definition.Shortcut;
                }
                SizeF sz = e.Graphics.MeasureString(toMeassure, SystemInformation.MenuFont);
                if (this.Parent is MainMenu)
                {
                    e.ItemHeight = SystemInformation.MenuHeight;
                    e.ItemWidth = (int)sz.Width; // TopLevel hat kein Bildchen
                }
                else
                {
                    e.ItemHeight = ButtonImages.ButtonImageList.ImageSize.Height + 6; // 3 oben, 3 unten
                    e.ItemWidth = (ButtonImages.ButtonImageList.ImageSize.Width + 6) + ButtonImages.ButtonImageList.ImageSize.Width / 2 + (int)sz.Width;
                    // (Bildchen + 3 links + 3 rechts) + TextOffset + Textbreite
                }
            }
        }
        protected override void OnDrawItem(System.Windows.Forms.DrawItemEventArgs e)
        {
            MenuWithHandler definition = Tag as MenuWithHandler;
            if (this.Text == "-")
            {
                Rectangle LeftSquare = e.Bounds;
                Rectangle Text = e.Bounds;
                LeftSquare.Width = ButtonImages.ButtonImageList.ImageSize.Width + 6;
                Text.Offset(LeftSquare.Width, 0);
                Text.Width -= LeftSquare.Width;
                LinearGradientBrush leftBrush = new LinearGradientBrush(LeftSquare, SystemColors.ControlLightLight, SystemColors.ControlDark, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(leftBrush, LeftSquare);
                e.Graphics.FillRectangle(SystemBrushes.ControlLightLight, Text);
                e.Graphics.DrawLine(SystemPens.Control, e.Bounds.Left + (ButtonImages.ButtonImageList.ImageSize.Width + 6) + ButtonImages.ButtonImageList.ImageSize.Width / 2, e.Bounds.Top + 1, e.Bounds.Right, e.Bounds.Top + 1);
            }
            else
            {
                if (this.Parent is MainMenu)
                {
                    if ((e.State & DrawItemState.HotLight) != 0)
                    {
                        Color c = Color.FromArgb(
                            ((int)SystemColors.ActiveCaption.R + 2 * (int)SystemColors.ControlLightLight.R) / 3,
                            ((int)SystemColors.ActiveCaption.G + 2 * (int)SystemColors.ControlLightLight.G) / 3,
                            ((int)SystemColors.ActiveCaption.B + 2 * (int)SystemColors.ControlLightLight.B) / 3);
                        e.Graphics.FillRectangle(new SolidBrush(c), e.Bounds);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Top);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Right - 1, e.Bounds.Bottom - 1, e.Bounds.Left, e.Bounds.Bottom - 1);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Left, e.Bounds.Top);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(SystemBrushes.Control, e.Bounds);
                    }
                    float TextX = e.Bounds.Left;
                    float TextY = e.Bounds.Top + (e.Bounds.Height - SystemInformation.MenuFont.GetHeight(e.Graphics)) / 2;
                    StringFormat sf = StringFormat.GenericDefault;
                    sf.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
                    Brush textBrush;
                    if (!Enabled)
                        textBrush = SystemBrushes.FromSystemColor(SystemColors.GrayText);
                    else
                        textBrush = SystemBrushes.WindowText;
                    e.Graphics.DrawString(this.Text, SystemInformation.MenuFont, textBrush, TextX, TextY, sf);
                    //if (!Enabled)
                    //    e.Graphics.DrawString(this.Text,SystemInformation.MenuFont,SystemBrushes.FromSystemColor(SystemColors.GrayText),TextX,TextY,sf);
                    //else
                    //    e.Graphics.DrawString(this.Text,SystemInformation.MenuFont,SystemBrushes.WindowText,TextX,TextY,sf);
                }
                else
                {
                    Rectangle LeftSquare = e.Bounds;
                    Rectangle Text = e.Bounds;
                    LeftSquare.Width = ButtonImages.ButtonImageList.ImageSize.Width + 6;
                    Text.Offset(LeftSquare.Width, 0);
                    Text.Width -= LeftSquare.Width;
                    LinearGradientBrush leftBrush = new LinearGradientBrush(LeftSquare, SystemColors.ControlLightLight, SystemColors.ControlDark, LinearGradientMode.Horizontal);
                    if ((e.State & DrawItemState.Selected) != 0)
                    {
                        Color c = Color.FromArgb(
                        ((int)SystemColors.ActiveCaption.R + 2 * (int)SystemColors.ControlLightLight.R) / 3,
                        ((int)SystemColors.ActiveCaption.G + 2 * (int)SystemColors.ControlLightLight.G) / 3,
                        ((int)SystemColors.ActiveCaption.B + 2 * (int)SystemColors.ControlLightLight.B) / 3);
                        e.Graphics.FillRectangle(new SolidBrush(c), e.Bounds);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Top);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Right - 1, e.Bounds.Bottom - 1, e.Bounds.Left, e.Bounds.Bottom - 1);
                        e.Graphics.DrawLine(SystemPens.ControlText, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Left, e.Bounds.Top);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(leftBrush, LeftSquare);
                        e.Graphics.FillRectangle(SystemBrushes.ControlLightLight, Text);
                    }
                    float TextX = e.Bounds.Left + (ButtonImages.ButtonImageList.ImageSize.Width + 6) + ButtonImages.ButtonImageList.ImageSize.Width / 2;
                    float TextY = e.Bounds.Top + (e.Bounds.Height - SystemInformation.MenuFont.GetHeight(e.Graphics)) / 2;
                    StringFormat sf = StringFormat.GenericDefault;
                    sf.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
                    Brush textBrush;
                    if (!Enabled)
                        textBrush = SystemBrushes.FromSystemColor(SystemColors.GrayText);
                    else
                        textBrush = SystemBrushes.WindowText;
                    e.Graphics.DrawString(this.Text, SystemInformation.MenuFont, textBrush, TextX, TextY, sf);
                    string shortcut = definition.Shortcut;// GetShortcutString();
                    if (!string.IsNullOrEmpty(shortcut))
                    {
                        SizeF sz = e.Graphics.MeasureString(shortcut, SystemInformation.MenuFont);
                        e.Graphics.DrawString(shortcut, SystemInformation.MenuFont, textBrush, e.Bounds.Right - e.Bounds.Height / 2 - sz.Width, TextY, sf);
                    }
                    int dx = (e.Bounds.Height - ButtonImages.ButtonImageList.ImageSize.Height) / 2;
                    if (definition.ImageIndex >= 0)
                    {
                        int ind = definition.ImageIndex;
                        if (ind >= 10000) ind = ind - 10000 + ButtonImages.OffsetUserImages;
                        if (Enabled)
                            ButtonImages.ButtonImageList.Draw(e.Graphics, e.Bounds.Left + 3, e.Bounds.Top + 3, ind);
                        else
                            ControlPaint.DrawImageDisabled(e.Graphics, ButtonImages.ButtonImageList.Images[ind], e.Bounds.Left + 3, e.Bounds.Top + 3, Color.FromArgb(0, 0, 0, 0));
                    }
                    if ((e.State & DrawItemState.Checked) != 0 || doubleChecked)
                    {
                        if (definition.ImageIndex < 0)
                        {
                            ButtonImages.ButtonImageList.Draw(e.Graphics, e.Bounds.Left + 3, e.Bounds.Top + 3, 84); // das ist das rote Häckchen
                        }
                        int x1 = e.Bounds.Left + 1;
                        int y1 = e.Bounds.Top + 1;
                        int x2 = x1 + ButtonImages.ButtonImageList.ImageSize.Width + 4;
                        int y2 = y1 + ButtonImages.ButtonImageList.ImageSize.Height + 4;
                        e.Graphics.DrawLine(SystemPens.ControlText, x1, y1, x2, y1);
                        e.Graphics.DrawLine(SystemPens.ControlText, x2, y1, x2, y2);
                        e.Graphics.DrawLine(SystemPens.ControlText, x2, y2, x1, y2);
                        e.Graphics.DrawLine(SystemPens.ControlText, x1, y2, x1, y1);
                    }
                }
            }
        }
        protected override void OnPopup(EventArgs e)
        {
            if (this.IsParent)
            {
                foreach (MenuItem mi in MenuItems)
                {
                    MenuWithHandler definition = (mi.Tag as MenuWithHandler);
                    if (definition != null)
                    {
                        if (definition.Target != null)
                        {
                            CommandState commandState = new CommandState();
                            if (definition.Target.OnUpdateCommand(definition.ID, commandState))
                            {
                                mi.Enabled = commandState.Enabled;
                                mi.Checked = commandState.Checked;
                                (mi as MenuItemWithHandler).doubleChecked = commandState.Checked;
                            }
                            else
                            {
                                //IFrameInternal frm = ActiveFrame.Frame as IFrameInternal;
                                //bool handled = false;
                                //if (frm != null) frm.UpdateContextMenu(miid.mTarget, miid.mID, commandState, ref handled);
                                //if (handled)
                                //{
                                //    miid.Enabled = commandState.Enabled;
                                //    miid.Checked = commandState.Checked;
                                //    miid.doubleChecked = commandState.Checked;
                                //}
                            }
                        }
                    }
                }
            }
            base.OnPopup(e);
        }
    }
    class MenuManager
    {
        static internal ContextMenuWithHandler MakeContextMenu(MenuWithHandler[] definition)
        {
            ContextMenuWithHandler cm = new ContextMenuWithHandler(definition);
            return cm;
        }

        static internal MainMenu MakeMainMenu(MenuWithHandler[] definition)
        {
            MainMenu res = new MainMenu();
            MenuItem[] items = new MenuItem[definition.Length];
            for (int i = 0; i < definition.Length; i++)
            {
                items[i] = new MenuItemWithHandler(definition[i]);
            }
            res.MenuItems.AddRange(items);
            return res;
        }
    }
}
