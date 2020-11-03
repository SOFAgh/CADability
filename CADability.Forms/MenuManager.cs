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
