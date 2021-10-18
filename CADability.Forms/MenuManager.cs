using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CADability.Forms
{
	internal class ContextMenuWithHandler : ContextMenuStrip
    {
        ICommandHandler commandHandler;
        public string menuID;

        public ContextMenuWithHandler(MenuItemWithHandler[] MenuItems, ICommandHandler Handler, string menuID)
        {
            commandHandler = Handler;
            this.menuID = menuID;
            Items.AddRange(MenuItems);
        }
        public ContextMenuWithHandler(MenuWithHandler[] definitions)
        {
            this.menuID = null;
            commandHandler = null;
            foreach (MenuWithHandler definition in definitions)
            {
                Items.Add(MenuManager.MakeStripItem(definition));
            }
        }
        public MenuItemWithHandler[] Menus
        {
            get
            {
                List<MenuItemWithHandler> sb = new List<MenuItemWithHandler>();
                foreach (ToolStripItem tsi in Items)
                {
                    if (tsi is MenuItemWithHandler mih)
                    {
                        sb.Add(mih);
                    }
                }
                return sb.ToArray();
            }
        }
        private void RecurseCommandState(MenuItemWithHandler mih)
        {
            foreach (MenuItemWithHandler submih in mih.SubMenus)
            {
                UpdateCommand(submih, commandHandler);
                if (submih.IsParent) RecurseCommandState(submih);

            }
        }
        private static void UpdateCommand(MenuItemWithHandler mih, ICommandHandler commandHandler)
        {
            if (mih != null && commandHandler != null)
            {
                CommandState commandState = new CommandState();
                commandHandler.OnUpdateCommand((mih.Tag as MenuWithHandler).ID, commandState);
                mih.Enabled = commandState.Enabled;
                mih.Checked = commandState.Checked;
            }
        }
        public void UpdateCommand()
        {
            foreach (MenuItemWithHandler mih in Menus)
            {
                if (mih.Tag is MenuWithHandler mh)
                {
                    UpdateCommand(mih, mh.Target);
                }
            }
        }
        protected override void OnOpened(EventArgs e)
        {
            UpdateCommand();
            base.OnOpened(e);
        }
        public void SetCommandHandler(ICommandHandler commandHandler)
        {
            foreach (MenuItemWithHandler submih in Menus)
            {
                (submih.Tag as MenuWithHandler).Target = commandHandler;
                if (submih.IsParent) SetCommandHandler(submih, commandHandler);
            }
            this.commandHandler = commandHandler;
        }
        private void SetCommandHandler(MenuItemWithHandler miid, ICommandHandler commandHandler)
        {
            foreach (MenuItemWithHandler submih in miid.SubMenus)
            {
                (submih.Tag as MenuWithHandler).Target = commandHandler;
                if (submih.IsParent) SetCommandHandler(submih, commandHandler);
            }
        }
        public delegate void MenuItemSelectedDelegate(string menuId);
        public event MenuItemSelectedDelegate MenuItemSelectedEvent;
        public void FireMenuItemSelected(string menuId)
        {
            if (MenuItemSelectedEvent != null) MenuItemSelectedEvent(menuId);
        }
        private bool ProcessShortCut(Keys keys, MenuItemWithHandler mih)
        {
            if (mih.IsParent)
            {
                foreach (MenuItemWithHandler submih in mih.SubMenus)
                {
                    if (ProcessShortCut(keys, submih))
                    {
                        return true;
                    }
                }
            }
            else if (mih.ShortcutKeys == keys)
            {
                MenuWithHandler definition = mih.Tag as MenuWithHandler;
                CommandState commandState = new CommandState();
                commandHandler.OnUpdateCommand(definition.ID, commandState);
                if (commandState.Enabled)
                {
                    commandHandler.OnCommand(definition.ID);
                }
                return true;
            }
            return false;
        }
        internal bool ProcessShortCut(Keys keys)
        {
            foreach (MenuItemWithHandler mih in Menus)
            {
                if (ProcessShortCut(keys, mih))
                {
                    return true;
                }
            }
            return false;
        }
    }

    class MenuItemWithHandler : ToolStripMenuItem
    {
        private static Keys ShortcutFromString(string p)
        {   // rather primitive:
            switch (p)
            {
                case "Alt0": return Keys.Alt | Keys.D0;
                case "Alt1": return Keys.Alt | Keys.D1;
                case "Alt2": return Keys.Alt | Keys.D2;
                case "Alt3": return Keys.Alt | Keys.D3;
                case "Alt4": return Keys.Alt | Keys.D4;
                case "Alt5": return Keys.Alt | Keys.D5;
                case "Alt6": return Keys.Alt | Keys.D6;
                case "Alt7": return Keys.Alt | Keys.D7;
                case "Alt8": return Keys.Alt | Keys.D8;
                case "Alt9": return Keys.Alt | Keys.D9;
                case "AltBksp": return Keys.Alt | Keys.Back;
                case "AltDownArrow": return Keys.Alt | Keys.Down;
                case "AltF1": return Keys.Alt | Keys.F1;
                case "AltF10": return Keys.Alt | Keys.F10;
                case "AltF11": return Keys.Alt | Keys.F11;
                case "AltF12": return Keys.Alt | Keys.F12;
                case "AltF2": return Keys.Alt | Keys.F2;
                case "AltF3": return Keys.Alt | Keys.F3;
                case "AltF4": return Keys.Alt | Keys.F4;
                case "AltF5": return Keys.Alt | Keys.F5;
                case "AltF6": return Keys.Alt | Keys.F6;
                case "AltF7": return Keys.Alt | Keys.F7;
                case "AltF8": return Keys.Alt | Keys.F8;
                case "AltF9": return Keys.Alt | Keys.F9;
                case "AltLeftArrow": return Keys.Alt | Keys.Left;
                case "AltRightArrow": return Keys.Alt | Keys.Right;
                case "AltUpArrow": return Keys.Alt | Keys.Up;
                case "Ctrl0": return Keys.Control | Keys.D0;
                case "Ctrl1": return Keys.Control | Keys.D1;
                case "Ctrl2": return Keys.Control | Keys.D2;
                case "Ctrl3": return Keys.Control | Keys.D3;
                case "Ctrl4": return Keys.Control | Keys.D4;
                case "Ctrl5": return Keys.Control | Keys.D5;
                case "Ctrl6": return Keys.Control | Keys.D6;
                case "Ctrl7": return Keys.Control | Keys.D7;
                case "Ctrl8": return Keys.Control | Keys.D8;
                case "Ctrl9": return Keys.Control | Keys.D9;
                case "CtrlA": return Keys.Control | Keys.A;
                case "CtrlB": return Keys.Control | Keys.B;
                case "CtrlC": return Keys.Control | Keys.C;
                case "CtrlD": return Keys.Control | Keys.D;
                case "CtrlDel": return Keys.Control | Keys.Delete;
                case "CtrlE": return Keys.Control | Keys.E;
                case "CtrlF": return Keys.Control | Keys.F;
                case "CtrlF1": return Keys.Control | Keys.F1;
                case "CtrlF10": return Keys.Control | Keys.F10;
                case "CtrlF11": return Keys.Control | Keys.F11;
                case "CtrlF12": return Keys.Control | Keys.F12;
                case "CtrlF2": return Keys.Control | Keys.F2;
                case "CtrlF3": return Keys.Control | Keys.F3;
                case "CtrlF4": return Keys.Control | Keys.F4;
                case "CtrlF5": return Keys.Control | Keys.F5;
                case "CtrlF6": return Keys.Control | Keys.F6;
                case "CtrlF7": return Keys.Control | Keys.F7;
                case "CtrlF8": return Keys.Control | Keys.F8;
                case "CtrlF9": return Keys.Control | Keys.F9;
                case "CtrlG": return Keys.Control | Keys.G;
                case "CtrlH": return Keys.Control | Keys.H;
                case "CtrlI": return Keys.Control | Keys.I;
                case "CtrlIns": return Keys.Control | Keys.Insert;
                case "CtrlJ": return Keys.Control | Keys.J;
                case "CtrlK": return Keys.Control | Keys.K;
                case "CtrlL": return Keys.Control | Keys.L;
                case "CtrlM": return Keys.Control | Keys.M;
                case "CtrlN": return Keys.Control | Keys.N;
                case "CtrlO": return Keys.Control | Keys.O;
                case "CtrlP": return Keys.Control | Keys.P;
                case "CtrlQ": return Keys.Control | Keys.Q;
                case "CtrlR": return Keys.Control | Keys.R;
                case "CtrlS": return Keys.Control | Keys.S;
                case "CtrlShift0": return Keys.Control | Keys.Shift | Keys.D0;
                case "CtrlShift1": return Keys.Control | Keys.Shift | Keys.D1;
                case "CtrlShift2": return Keys.Control | Keys.Shift | Keys.D2;
                case "CtrlShift3": return Keys.Control | Keys.Shift | Keys.D3;
                case "CtrlShift4": return Keys.Control | Keys.Shift | Keys.D4;
                case "CtrlShift5": return Keys.Control | Keys.Shift | Keys.D5;
                case "CtrlShift6": return Keys.Control | Keys.Shift | Keys.D6;
                case "CtrlShift7": return Keys.Control | Keys.Shift | Keys.D7;
                case "CtrlShift8": return Keys.Control | Keys.Shift | Keys.D8;
                case "CtrlShift9": return Keys.Control | Keys.Shift | Keys.D9;
                case "CtrlShiftA": return Keys.Control | Keys.Shift | Keys.A;
                case "CtrlShiftB": return Keys.Control | Keys.Shift | Keys.B;
                case "CtrlShiftC": return Keys.Control | Keys.Shift | Keys.C;
                case "CtrlShiftD": return Keys.Control | Keys.Shift | Keys.D;
                case "CtrlShiftE": return Keys.Control | Keys.Shift | Keys.E;
                case "CtrlShiftF": return Keys.Control | Keys.Shift | Keys.F;
                case "CtrlShiftF1": return Keys.Control | Keys.Shift | Keys.F1;
                case "CtrlShiftF10": return Keys.Control | Keys.Shift | Keys.F10;
                case "CtrlShiftF11": return Keys.Control | Keys.Shift | Keys.F11;
                case "CtrlShiftF12": return Keys.Control | Keys.Shift | Keys.F12;
                case "CtrlShiftF2": return Keys.Control | Keys.Shift | Keys.F2;
                case "CtrlShiftF3": return Keys.Control | Keys.Shift | Keys.F3;
                case "CtrlShiftF4": return Keys.Control | Keys.Shift | Keys.F4;
                case "CtrlShiftF5": return Keys.Control | Keys.Shift | Keys.F5;
                case "CtrlShiftF6": return Keys.Control | Keys.Shift | Keys.F6;
                case "CtrlShiftF7": return Keys.Control | Keys.Shift | Keys.F7;
                case "CtrlShiftF8": return Keys.Control | Keys.Shift | Keys.F8;
                case "CtrlShiftF9": return Keys.Control | Keys.Shift | Keys.F9;
                case "CtrlShiftG": return Keys.Control | Keys.Shift | Keys.G;
                case "CtrlShiftH": return Keys.Control | Keys.Shift | Keys.H;
                case "CtrlShiftI": return Keys.Control | Keys.Shift | Keys.I;
                case "CtrlShiftJ": return Keys.Control | Keys.Shift | Keys.J;
                case "CtrlShiftK": return Keys.Control | Keys.Shift | Keys.K;
                case "CtrlShiftL": return Keys.Control | Keys.Shift | Keys.L;
                case "CtrlShiftM": return Keys.Control | Keys.Shift | Keys.M;
                case "CtrlShiftN": return Keys.Control | Keys.Shift | Keys.N;
                case "CtrlShiftO": return Keys.Control | Keys.Shift | Keys.O;
                case "CtrlShiftP": return Keys.Control | Keys.Shift | Keys.P;
                case "CtrlShiftQ": return Keys.Control | Keys.Shift | Keys.Q;
                case "CtrlShiftR": return Keys.Control | Keys.Shift | Keys.R;
                case "CtrlShiftS": return Keys.Control | Keys.Shift | Keys.S;
                case "CtrlShiftT": return Keys.Control | Keys.Shift | Keys.T;
                case "CtrlShiftU": return Keys.Control | Keys.Shift | Keys.U;
                case "CtrlShiftV": return Keys.Control | Keys.Shift | Keys.V;
                case "CtrlShiftW": return Keys.Control | Keys.Shift | Keys.W;
                case "CtrlShiftX": return Keys.Control | Keys.Shift | Keys.X;
                case "CtrlShiftY": return Keys.Control | Keys.Shift | Keys.Y;
                case "CtrlShiftZ": return Keys.Control | Keys.Shift | Keys.Z;
                case "CtrlT": return Keys.Control | Keys.T;
                case "CtrlU": return Keys.Control | Keys.U;
                case "CtrlV": return Keys.Control | Keys.V;
                case "CtrlW": return Keys.Control | Keys.W;
                case "CtrlX": return Keys.Control | Keys.X;
                case "CtrlY": return Keys.Control | Keys.Y;
                case "CtrlZ": return Keys.Control | Keys.Z;
                case "Del": return Keys.Delete;
                case "F1": return Keys.F1;
                case "F10": return Keys.F10;
                case "F11": return Keys.F11;
                case "F12": return Keys.F12;
                case "F2": return Keys.F2;
                case "F3": return Keys.F3;
                case "F4": return Keys.F4;
                case "F5": return Keys.F5;
                case "F6": return Keys.F6;
                case "F7": return Keys.F7;
                case "F8": return Keys.F8;
                case "F9": return Keys.F9;
                case "Ins": return Keys.Insert;
                case "None": return Keys.None;
                case "ShiftDel": return Keys.Shift | Keys.Delete;
                case "ShiftF1": return Keys.Shift | Keys.F1;
                case "ShiftF10": return Keys.Shift | Keys.F10;
                case "ShiftF11": return Keys.Shift | Keys.F11;
                case "ShiftF12": return Keys.Shift | Keys.F12;
                case "ShiftF2": return Keys.Shift | Keys.F2;
                case "ShiftF3": return Keys.Shift | Keys.F3;
                case "ShiftF4": return Keys.Shift | Keys.F4;
                case "ShiftF5": return Keys.Shift | Keys.F5;
                case "ShiftF6": return Keys.Shift | Keys.F6;
                case "ShiftF7": return Keys.Shift | Keys.F7;
                case "ShiftF8": return Keys.Shift | Keys.F8;
                case "ShiftF9": return Keys.Shift | Keys.F9;
                case "ShiftIns": return Keys.Shift | Keys.Insert;
                default: return Keys.None;
            }
        }
        public MenuItemWithHandler(MenuWithHandler definition)
        {
            Text = definition.Text;
            Tag = definition;
			if (!string.IsNullOrEmpty(definition.Shortcut))
			{
				ShortcutKeys = ShortcutFromString(definition.Shortcut);
				ShowShortcutKeys = definition.ShowShortcut;
			}
			if (definition.SubMenus != null)
            {
                foreach (MenuWithHandler subMenuDef in definition.SubMenus)
                {
                    DropDownItems.Add(MenuManager.MakeStripItem(subMenuDef));
                }
            }
            if (definition.ImageIndex >= 0)
            {
                int ind = definition.ImageIndex;
                if (ind >= 10000) ind = ind - 10000 + ButtonImages.OffsetUserImages;
                Image = ButtonImages.ButtonImageList.Images[ind];
            }
        }
        public MenuItemWithHandler[] SubMenus
        {
            get
            {
                List<MenuItemWithHandler> sb = new List<MenuItemWithHandler>();
                foreach (ToolStripItem tsi in DropDownItems)
                {
                    if (tsi is MenuItemWithHandler mih)
                    {
                        sb.Add(mih);
                    }
                }
                return sb.ToArray();
            }
        }
        public bool IsParent
        {
            get { return SubMenus.Length > 0; }
        }
        protected override void OnClick(EventArgs e)
        {
            MenuWithHandler definition = Tag as MenuWithHandler;
            if (definition.Target != null) definition.Target.OnCommand(definition.ID);
        }
        protected override void OnDropDownOpened(EventArgs e)
        {
            if (IsParent)
            {
                foreach (MenuItemWithHandler mih in SubMenus)
                {
                    if (mih.Tag is MenuWithHandler definition)
                    {
                        if (definition.Target != null)
                        {
                            CommandState commandState = new CommandState();
                            if (definition.Target.OnUpdateCommand(definition.ID, commandState))
                            {
                                mih.Enabled = commandState.Enabled;
                                mih.Checked = commandState.Checked;
                            }
                        }
                    }
                }
            }
            base.OnDropDownOpened(e);
        }
    }

    class MenuManager
    {
        static internal ContextMenuWithHandler MakeContextMenu(MenuWithHandler[] definition)
        {
            ContextMenuWithHandler cm = new ContextMenuWithHandler(definition);
            return cm;
        }
        static internal MenuStrip MakeMainMenu(MenuWithHandler[] definitions)
        {
            MenuStrip res = new MenuStrip();
            foreach (MenuWithHandler definition in definitions)
            {
                res.Items.Add(MakeStripItem(definition));
            }
            return res;
        }
        static internal ToolStripItem MakeStripItem(MenuWithHandler definition)
        {
            if (definition.Text == "-")
            {
                return new ToolStripSeparator();
            }
            else
            {
                return new MenuItemWithHandler(definition);
            }
        }
    }
}
