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
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        protected override void OnCollapse(EventArgs e)
        {
            base.OnCollapse(e);
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
        private static Shortcut ShortcutFromString(string p)
        {   // rather primitive:
            switch (p)
            {
                case "Alt0": return Shortcut.Alt0;
                case "Alt1": return Shortcut.Alt1;
                case "Alt2": return Shortcut.Alt2;
                case "Alt3": return Shortcut.Alt3;
                case "Alt4": return Shortcut.Alt4;
                case "Alt5": return Shortcut.Alt5;
                case "Alt6": return Shortcut.Alt6;
                case "Alt7": return Shortcut.Alt7;
                case "Alt8": return Shortcut.Alt8;
                case "Alt9": return Shortcut.Alt9;
                case "AltBksp": return Shortcut.AltBksp;
                case "AltDownArrow": return Shortcut.AltDownArrow;
                case "AltF1": return Shortcut.AltF1;
                case "AltF10": return Shortcut.AltF10;
                case "AltF11": return Shortcut.AltF11;
                case "AltF12": return Shortcut.AltF12;
                case "AltF2": return Shortcut.AltF2;
                case "AltF3": return Shortcut.AltF3;
                case "AltF4": return Shortcut.AltF4;
                case "AltF5": return Shortcut.AltF5;
                case "AltF6": return Shortcut.AltF6;
                case "AltF7": return Shortcut.AltF7;
                case "AltF8": return Shortcut.AltF8;
                case "AltF9": return Shortcut.AltF9;
                case "AltLeftArrow": return Shortcut.AltLeftArrow;
                case "AltRightArrow": return Shortcut.AltRightArrow;
                case "AltUpArrow": return Shortcut.AltUpArrow;
                case "Ctrl0": return Shortcut.Ctrl0;
                case "Ctrl1": return Shortcut.Ctrl1;
                case "Ctrl2": return Shortcut.Ctrl2;
                case "Ctrl3": return Shortcut.Ctrl3;
                case "Ctrl4": return Shortcut.Ctrl4;
                case "Ctrl5": return Shortcut.Ctrl5;
                case "Ctrl6": return Shortcut.Ctrl6;
                case "Ctrl7": return Shortcut.Ctrl7;
                case "Ctrl8": return Shortcut.Ctrl8;
                case "Ctrl9": return Shortcut.Ctrl9;
                case "CtrlA": return Shortcut.CtrlA;
                case "CtrlB": return Shortcut.CtrlB;
                case "CtrlC": return Shortcut.CtrlC;
                case "CtrlD": return Shortcut.CtrlD;
                case "CtrlDel": return Shortcut.CtrlDel;
                case "CtrlE": return Shortcut.CtrlE;
                case "CtrlF": return Shortcut.CtrlF;
                case "CtrlF1": return Shortcut.CtrlF1;
                case "CtrlF10": return Shortcut.CtrlF10;
                case "CtrlF11": return Shortcut.CtrlF11;
                case "CtrlF12": return Shortcut.CtrlF12;
                case "CtrlF2": return Shortcut.CtrlF2;
                case "CtrlF3": return Shortcut.CtrlF3;
                case "CtrlF4": return Shortcut.CtrlF4;
                case "CtrlF5": return Shortcut.CtrlF5;
                case "CtrlF6": return Shortcut.CtrlF6;
                case "CtrlF7": return Shortcut.CtrlF7;
                case "CtrlF8": return Shortcut.CtrlF8;
                case "CtrlF9": return Shortcut.CtrlF9;
                case "CtrlG": return Shortcut.CtrlG;
                case "CtrlH": return Shortcut.CtrlH;
                case "CtrlI": return Shortcut.CtrlI;
                case "CtrlIns": return Shortcut.CtrlIns;
                case "CtrlJ": return Shortcut.CtrlJ;
                case "CtrlK": return Shortcut.CtrlK;
                case "CtrlL": return Shortcut.CtrlL;
                case "CtrlM": return Shortcut.CtrlM;
                case "CtrlN": return Shortcut.CtrlN;
                case "CtrlO": return Shortcut.CtrlO;
                case "CtrlP": return Shortcut.CtrlP;
                case "CtrlQ": return Shortcut.CtrlQ;
                case "CtrlR": return Shortcut.CtrlR;
                case "CtrlS": return Shortcut.CtrlS;
                case "CtrlShift0": return Shortcut.CtrlShift0;
                case "CtrlShift1": return Shortcut.CtrlShift1;
                case "CtrlShift2": return Shortcut.CtrlShift2;
                case "CtrlShift3": return Shortcut.CtrlShift3;
                case "CtrlShift4": return Shortcut.CtrlShift4;
                case "CtrlShift5": return Shortcut.CtrlShift5;
                case "CtrlShift6": return Shortcut.CtrlShift6;
                case "CtrlShift7": return Shortcut.CtrlShift7;
                case "CtrlShift8": return Shortcut.CtrlShift8;
                case "CtrlShift9": return Shortcut.CtrlShift9;
                case "CtrlShiftA": return Shortcut.CtrlShiftA;
                case "CtrlShiftB": return Shortcut.CtrlShiftB;
                case "CtrlShiftC": return Shortcut.CtrlShiftC;
                case "CtrlShiftD": return Shortcut.CtrlShiftD;
                case "CtrlShiftE": return Shortcut.CtrlShiftE;
                case "CtrlShiftF": return Shortcut.CtrlShiftF;
                case "CtrlShiftF1": return Shortcut.CtrlShiftF1;
                case "CtrlShiftF10": return Shortcut.CtrlShiftF10;
                case "CtrlShiftF11": return Shortcut.CtrlShiftF11;
                case "CtrlShiftF12": return Shortcut.CtrlShiftF12;
                case "CtrlShiftF2": return Shortcut.CtrlShiftF2;
                case "CtrlShiftF3": return Shortcut.CtrlShiftF3;
                case "CtrlShiftF4": return Shortcut.CtrlShiftF4;
                case "CtrlShiftF5": return Shortcut.CtrlShiftF5;
                case "CtrlShiftF6": return Shortcut.CtrlShiftF6;
                case "CtrlShiftF7": return Shortcut.CtrlShiftF7;
                case "CtrlShiftF8": return Shortcut.CtrlShiftF8;
                case "CtrlShiftF9": return Shortcut.CtrlShiftF9;
                case "CtrlShiftG": return Shortcut.CtrlShiftG;
                case "CtrlShiftH": return Shortcut.CtrlShiftH;
                case "CtrlShiftI": return Shortcut.CtrlShiftI;
                case "CtrlShiftJ": return Shortcut.CtrlShiftJ;
                case "CtrlShiftK": return Shortcut.CtrlShiftK;
                case "CtrlShiftL": return Shortcut.CtrlShiftL;
                case "CtrlShiftM": return Shortcut.CtrlShiftM;
                case "CtrlShiftN": return Shortcut.CtrlShiftN;
                case "CtrlShiftO": return Shortcut.CtrlShiftO;
                case "CtrlShiftP": return Shortcut.CtrlShiftP;
                case "CtrlShiftQ": return Shortcut.CtrlShiftQ;
                case "CtrlShiftR": return Shortcut.CtrlShiftR;
                case "CtrlShiftS": return Shortcut.CtrlShiftS;
                case "CtrlShiftT": return Shortcut.CtrlShiftT;
                case "CtrlShiftU": return Shortcut.CtrlShiftU;
                case "CtrlShiftV": return Shortcut.CtrlShiftV;
                case "CtrlShiftW": return Shortcut.CtrlShiftW;
                case "CtrlShiftX": return Shortcut.CtrlShiftX;
                case "CtrlShiftY": return Shortcut.CtrlShiftY;
                case "CtrlShiftZ": return Shortcut.CtrlShiftZ;
                case "CtrlT": return Shortcut.CtrlT;
                case "CtrlU": return Shortcut.CtrlU;
                case "CtrlV": return Shortcut.CtrlV;
                case "CtrlW": return Shortcut.CtrlW;
                case "CtrlX": return Shortcut.CtrlX;
                case "CtrlY": return Shortcut.CtrlY;
                case "CtrlZ": return Shortcut.CtrlZ;
                case "Del": return Shortcut.Del;
                case "F1": return Shortcut.F1;
                case "F10": return Shortcut.F10;
                case "F11": return Shortcut.F11;
                case "F12": return Shortcut.F12;
                case "F2": return Shortcut.F2;
                case "F3": return Shortcut.F3;
                case "F4": return Shortcut.F4;
                case "F5": return Shortcut.F5;
                case "F6": return Shortcut.F6;
                case "F7": return Shortcut.F7;
                case "F8": return Shortcut.F8;
                case "F9": return Shortcut.F9;
                case "Ins": return Shortcut.Ins;
                case "None": return Shortcut.None;
                case "ShiftDel": return Shortcut.ShiftDel;
                case "ShiftF1": return Shortcut.ShiftF1;
                case "ShiftF10": return Shortcut.ShiftF10;
                case "ShiftF11": return Shortcut.ShiftF11;
                case "ShiftF12": return Shortcut.ShiftF12;
                case "ShiftF2": return Shortcut.ShiftF2;
                case "ShiftF3": return Shortcut.ShiftF3;
                case "ShiftF4": return Shortcut.ShiftF4;
                case "ShiftF5": return Shortcut.ShiftF5;
                case "ShiftF6": return Shortcut.ShiftF6;
                case "ShiftF7": return Shortcut.ShiftF7;
                case "ShiftF8": return Shortcut.ShiftF8;
                case "ShiftF9": return Shortcut.ShiftF9;
                case "ShiftIns": return Shortcut.ShiftIns;
                default: return Shortcut.None;
            }
        }
        public MenuItemWithHandler(MenuWithHandler definition) : base()
        {
            OwnerDraw = true;
            Text = definition.Text;
            Tag = definition;
            if (!string.IsNullOrEmpty(definition.Shortcut))
            {
                Shortcut = ShortcutFromString(definition.Shortcut);
                ShowShortcut = definition.ShowShortcut;
            }
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
            // Sometimes all menu texts are blank. I cannot reproduce it. I guess, it is because the menus are not disposed. I debugged by overriding Dispose of ContextMenuWithHandler
            // If a menu item is clicked, the menu disappears and can be disposed. This is forced here. But if the menu disappears because of some other reason (ESC, click somewhere else)
            // Dispose doesn't get called until maybe much later or when the form closes.
            ContextMenuWithHandler toDispose = null;
            Menu parent = this.Parent;
            while (parent != null)
            {
                if (parent is ContextMenuWithHandler cmh)
                {
                    toDispose = cmh;
                    break;
                }
                if (parent is MenuItem mi) parent = mi.Parent;
                else break;
            }
            toDispose?.Dispose();
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
                            ButtonImages.ButtonImageList.Draw(e.Graphics, e.Bounds.Left + 3, e.Bounds.Top + 3, 84); // the red check mark
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
                                //IFrameInternal frm = FrameImpl.MainFrame as IFrameInternal;
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
        protected override void OnSelect(EventArgs e)
        {
            MenuWithHandler definition = (Tag as MenuWithHandler);
            if (definition.Target != null) definition.Target.OnSelected(definition, true);
            base.OnSelect(e);
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
