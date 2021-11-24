using CADability.Attribute;
using CADability.GeoObject;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System;
using System.ComponentModel;
using System.IO;

namespace CADability.Forms
{
#if DEBUG
    internal class Trace
    {
        static public void Clear()
        {
            if (File.Exists(@"C:\Temp\CADability.Trace.txt"))
                File.Delete(@"C:\Temp\CADability.Trace.txt");
        }
        static public void WriteLine(string text)
        {
            lock (typeof(Trace))
            {
                using (StreamWriter w = File.AppendText(@"C:\Temp\CADability.Trace.txt"))
                {
                    w.WriteLine(text);
                }
            }
        }
    }

    /// <summary>
    /// This Form is almost identical to the MainForm. It is being created via reflection from CADability kernel and used to display CADability objects in a ModelView.
    /// </summary>
    public class DebugForm : CadForm, IDebugForm
    {
        public DebugForm() : base(new string[] { })
        {
            // Trace.WriteLine("DebugForm constructor");
            Text = "CADability.Forms.DebugForm";
            CadFrame.GenerateNewProject();
        }

        protected override void OnShown(EventArgs e)
        {
            // Trace.WriteLine("DebugForm OnShown");
            base.OnShown(e);
            // zoom total and make all layers visible
            ((CadFrame as IFrame).ActiveView as ModelView).ZoomTotal(1.1);
            Layer[] vl = ((CadFrame as IFrame).ActiveView as ModelView).ProjectedModel.GetVisibleLayers();
            for (int i = 0; i < vl.Length; ++i)
            {
                ((CadFrame as IFrame).ActiveView as ModelView).ProjectedModel.RemoveVisibleLayer(vl[i]);
            }
            GeoPoint cnt = Model.Extent.GetCenter();
            ((CadFrame as IFrame).ActiveView as ModelView).FixPoint = cnt;
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            //foreach (IGeoObject go in Model)
            //{
            //    if (go is IColorDef cd) // resetting the colors to null, which where set in the DebuggerContainer
            //    {
            //        if (cd.ColorDef.Name == "auto point") cd.ColorDef = null;
            //        if (cd.ColorDef.Name == "auto curve") cd.ColorDef = null;
            //        if (cd.ColorDef.Name == "auto face") cd.ColorDef = null;
            //    }
            //}
            base.OnClosing(e);
        }
        /// <summary>
        /// Gives the owner access to the model
        /// </summary>
        public Model Model
        {
            get
            {
                return base.CadFrame.Project.GetActiveModel();
            }
        }
        /// <summary>
        /// Shows this form as the context of the DialogVisualizer
        /// </summary>
        /// <param name="windowService"></param>
        public void ShowDialog(IDialogVisualizerService windowService)
        {
            // Trace.WriteLine("DebugForm ShowDialog");
            windowService.ShowDialog(this);
        }
    }
#endif
}
