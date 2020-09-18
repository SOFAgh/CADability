using CADability.Curve2D;
using CADability.GeoObject;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CADability.Forms
{
    /// <summary>
    /// This class is not part of CADability OpenSource. I use it to write testcode for debugging
    /// </summary>
    internal class DebuggerPlayground : ICommandHandler
    {
        private IFrame frame;
        public DebuggerPlayground(IFrame frame)
        {
            this.frame = frame;
        }
        public static MenuWithHandler[] Connect(IFrame frame, MenuWithHandler[] mainMenu)
        {
            MenuWithHandler newItem = new MenuWithHandler();
            newItem.ID = "DebuggerPlayground.Debug";
            newItem.Text = "Debug";
            newItem.Target = new DebuggerPlayground(frame);
            List<MenuWithHandler> tmp = new List<MenuWithHandler>(mainMenu);
            tmp.Add(newItem);
            return tmp.ToArray();
        }

        public bool OnCommand(string MenuId)
        {
            if (MenuId == "DebuggerPlayground.Debug")
            {
                SomeTestCode();
                return true;
            }
            return false;
        }

        public bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            return false;
        }

        private void SomeTestCode()
        {
            string path = @"C:\Zeichnungen\STEP\protocol.txt";
            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Protocol step files extent");
                }
            }
            bool skip = true;
            string lastSkipFile = "NurbsTest.dxf";
            foreach (string file in Directory.EnumerateFiles(@"C:\Zeichnungen\STEP", "*.stp"))
            {
                if (skip)
                {
                    if (file.EndsWith(lastSkipFile)) skip = false;
                    continue;
                }
                try
                {
                    System.Diagnostics.Trace.WriteLine("importing: " + file);
                    Project pr = Project.ReadFromFile(file, "stp");
                    if (pr != null)
                    {
                        BoundingCube ext = pr.GetModel(0).Extent;
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.WriteLine(file + ", " + ext.Xmin + ", " + ext.Xmax + ", " + ext.Ymin + ", " + ext.Ymax + ", " + ext.Zmin + ", " + ext.Zmax);
                        }
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null) System.Diagnostics.Trace.WriteLine(file + ": " + ex.InnerException.Message);
                    else System.Diagnostics.Trace.WriteLine(file + ": " + ex.Message);
                }
            }
        }

    }
}
