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
        public MenuItemWithHandler(MenuWithHandler definition)
        {
            Text = definition.Text;
            Tag = definition;
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
