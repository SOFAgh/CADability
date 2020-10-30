# Organizational or root classes of CADability

### IFrame and FrameImpl
The name and idea for this class resembles the MFC CFrameWnd class, which was long time ago used by the predecessor of CADability.

While [IFrame](../api/CADability.IFrame.html) is an interface which is always used when we have a reference to the frame, [FrameImpl](../api/CADability.FrameImpl.html) 
is an implementation of that interface. And by now the only implementation. The idea was that you could also have different implementations of IFrame for multiple documents
or multiple concurrent views, but it seems, this will not be necessary.

The frame gives you access to the 
- [IControlCenter](../api/CADability.UserInterface.IControlCenter.html) (the abstraction of a properties explorer, which is implemented in CADability.Forms)
- currently open [Project](../api/CADability.Project.html)
- the active [View](../api/CADability.IView.html) on the project and the list of available views
- the [GlobalSettings](../api/CADability.Settings.html#CADability_Settings_GlobalSettings)
- the [ActionStack](../api/CADability.ActionStack.html) and the currently running [Action](../api/CADability.Actions.Action.html)
- the [UIService](../api/CADability.UserInterface.IUIService.html), which gives you access to user interface stuff like showing a standard dialog or getting the mouse position

Furthermore the frame handles most of the global menus, like opening a file or drawing a line. It is also the place where you can set (i.e. start) an [Action](../api/CADability.Actions.Action.html).

The CadFrame implements the still abstract class IFrameImpl, by implementing those things, you cannot do in .NET core.




 