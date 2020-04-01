using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADability.Forms
{
    /// <summary>
    /// This class is not part of CADability OpenSource. I use it to write testcode for debugging
    /// </summary>
    internal class DebuggerPlaground: ICommandHandler
    {
        private IFrame frame;
        public DebuggerPlaground(IFrame frame)
        {
            this.frame = frame;
        }
        public static MenuWithHandler[] Connect(IFrame frame, MenuWithHandler[] mainMenu)
        {
            MenuWithHandler newItem = new MenuWithHandler();
            newItem.ID = "DebuggerPlaground.Debug";
            newItem.Text = "Debug";
            newItem.Target = new DebuggerPlaground(frame);
            List<MenuWithHandler> tmp = new List<MenuWithHandler>(mainMenu);
            tmp.Add(newItem);
            return tmp.ToArray();
        }

        public bool OnCommand(string MenuId)
        {
            return false;
        }

        public bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            return false;
        }
    }
}
