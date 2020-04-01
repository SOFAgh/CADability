using CADability.Actions;
using CADability.GeoObject;
using CADability.Shapes;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Printing;

using Action = CADability.Actions.Action;
using CADability.Attribute;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Security.Principal;
using System.Text;
using System.Threading;
using CADability.UserInterface;

namespace CADability
{

    /// <summary>
    /// This class provides access to the current active frame (IFrame) object.
    /// It is typically the one and only SingleDocumentFrame in the application but
    /// may be different in future, when there will be a MultiDocumenFrame.
    /// </summary>

    public class ActiveFrame
    {
        private static Dictionary<string, IFrame> namedFrames = new Dictionary<string, IFrame>();
        /// <summary>
        /// Gets the currently active frame (IFrame), may be null
        /// </summary>
        public static IFrame Frame;
        public static void SetNamedFrame(string name, IFrame frame)
        {
            namedFrames[name] = frame;
        }
        public static void RemoveNamedFrame(string name)
        {
            namedFrames.Remove(name);
        }
        public static IFrame GetNamedFrame(string name)
        {
            return namedFrames[name];
        }
    }
#region IMessageFilter Members
#endregion
#region PROBLEME mit 32 / 64 Bit Version Suchbegriffe: Konfiguration Referenz
#endregion
#if DEBUG
#endif
#if DEBUG
#endif
#if DEBUG
#endif
#if DEBUG
#endif
#if DEBUG
#endif
#region helper
#endregion
#region ICommandHandler Members
#endregion
#region command handling methods
#if DEBUG
#endif
#if DEBUG
#endif
#endregion
#region Snap Einstellungen
#endregion
#region IFrame Members
#endregion
#region IFrameInternal Members
#endregion
#if DEBUG
#endif
}
