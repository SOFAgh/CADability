using CADability.Actions;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

namespace CADability.UserInterface
{
    /// <summary>
    /// State of a menu entry or toolbar button. Used in the <see cref="ICommandHandler.UpdateCommand"/> event.
    /// </summary>
    public class CommandState
    {
        bool enabled;
        bool check;
        bool radio;
        /// <summary>
        /// Creates a command state which is enabled, but not checked and no radio
        /// button set.
        /// </summary>
        public CommandState()
        {
            enabled = true;
            check = false;
            radio = false;
        }
        /// <summary>
        /// Gets or sets the "enabled" state.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }
        /// <summary>
        /// Gets or sets the "checked" state.
        /// </summary>
        public bool Checked
        {
            get { return check; }
            set { check = value; }
        }
        /// <summary>
        /// Gets or sets the "radio" state.
        /// </summary>
        public bool Radio
        {
            get { return radio; }
            set { radio = value; }
        }
    }
    /// <summary>
    /// Objects that implement this interface can receive menu command.
    /// Usually used in calls to <see cref="MenuResource.LoadMenuDefinition(string, bool, ICommandHandler)"/> to specify
    /// the target object for that menu.
    /// </summary>

    public interface ICommandHandler
    {
        /// <summary>
        /// Process the command with the given MenuId. Return true if handled, false otherwise.
        /// </summary>
        /// <param name="MenuId">Id of the menu command to be processed</param>
        /// <returns>true, if handled, false otherwise</returns>
        bool OnCommand(string MenuId);
        /// <summary>
        /// Update the command user interface of the given command. Return true if handled, false otherwise.
        /// </summary>
        /// <param name="MenuId">Id of the menu command to be processed</param>
        /// <param name="CommandState">State object to modify if appropriate</param>
        /// <returns>true, if handled, false otherwise</returns>
        bool OnUpdateCommand(string MenuId, CommandState CommandState);
        /// <summary>
        /// Notify that the menu item is being selected. No need to react on this notification
        /// </summary>
        /// <param name="MenuId">Id of the menu command that was selected</param>
        /// <param name="selected">true, if selected, false if deselected</param>
        /// <returns></returns>
        void OnSelected(MenuWithHandler selectedMenu, bool selected);
    }

    public class SimpleMenuCommand : ICommandHandler
    {
        Func<string, bool> OnCommand;
        Func<string, CommandState , bool> OnUpdateCommand;
        Action<MenuWithHandler> OnSelected;
        public static ICommandHandler HandleCommand(Func<string, bool> action)
        {
            return new SimpleMenuCommand(action);
        }
        
        public static ICommandHandler HandleAndUpdateCommand(Func<string, bool> action, Func<string, CommandState, bool> update)
        {
            return new SimpleMenuCommand(action, update);
        }
        public SimpleMenuCommand(Func<string, bool> action, Func<string, CommandState, bool> update)
        {
            this.OnCommand = action;
            this.OnUpdateCommand = update;
        }
        public SimpleMenuCommand(Func<string, bool> action)
        {
            this.OnCommand = action;
        }
        bool ICommandHandler.OnCommand(string MenuId)
        {
            if (OnCommand!=null) return OnCommand(MenuId);
            return false;
        }

        void ICommandHandler.OnSelected(MenuWithHandler selectedMenu, bool selected) 
        {
            if (OnSelected != null) OnSelected(selectedMenu);
        }

        bool ICommandHandler.OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            if (OnUpdateCommand != null) return OnUpdateCommand(MenuId, CommandState);
            else return true;
        }
    }
}
