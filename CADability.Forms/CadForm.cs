﻿using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;


namespace CADability.Forms
{
    public partial class CadForm : Form, ICommandHandler
    {
        private CadFrame cadFrame; // the frame, which knows about the views, the ControlCenter (PropertiesExplorer), the menu
        private ProgressForm progressForm;
        public CadForm(string[] args)
        {
            InitializeComponent(); // makes the cadCanvas and the propertiesExplorer
            KeyPreview = true; // used to filter the escape key (and maybe some more?)
            cadFrame = new CadFrame(this);
            cadCanvas.Frame = cadFrame;
            propertiesExplorer.Frame = cadFrame;
            // show this menu in the MainForm
            MenuWithHandler[] mainMenu = MenuResource.LoadMenuDefinition("SDI Menu", true, cadFrame);
            #region DebuggerPlayground, you can remove this region
            // in the following lines a "DebuggerPlayground" object is created via reflection. This class is a playground to write testcode
            // which is not included in the sources. This is why it is constructed via reflection, there is no need to have this class in the project.
            Type dbgplygnd = Type.GetType("CADability.Forms.DebuggerPlayground", false);
            if (dbgplygnd != null)
            {
                MethodInfo connect = dbgplygnd.GetMethod("Connect");
                if (connect != null) mainMenu = connect.Invoke(null, new object[] { cadFrame, mainMenu }) as MenuWithHandler[];
            }
            #endregion DebuggerPlayground
            MainMenuStrip = MenuManager.MakeMainMenu(mainMenu);
            Controls.Add(MainMenuStrip);
            // open an existing Project or create a new one
            ToolBars.CreateOrRestoreToolbars(topToolStripContainer, cadFrame);
            Application.Idle += new EventHandler(OnIdle); // update the toolbars (menues are updated when they popup)
        }
        // Access the components of the MainForm from the CadFrame. 
        public ProgressForm ProgressForm
        { 
            get
            {
                if (progressForm==null)
                {
                    progressForm = new ProgressForm
                    {
                        TopLevel = true,
                        Owner = this,
                        Visible = false
                    };
                }
                return progressForm;
            }
        }
        public PropertiesExplorer PropertiesExplorer => propertiesExplorer;
        public CadCanvas CadCanvas => cadCanvas;
        public CadFrame CadFrame => cadFrame;
        private void OnIdle(object sender, EventArgs e)
        {
            ToolBars.UpdateCommandState(topToolStripContainer.TopToolStripPanel);
        }
        protected override void OnClosing(CancelEventArgs e)
        {   // maybe we need to save the project
            ToolBars.SaveToolbarPositions(topToolStripContainer);
            Settings.SaveGlobalSettings();
            // ToolStripManager.SaveSettings(this); // save the positions of the toolbars (doesn't work correctly)
            base.OnClosing(e);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys nmKeyData = (Keys)((int)keyData & 0x0FFFF);
            bool preProcess = nmKeyData >= Keys.F1 && nmKeyData <= Keys.F24;
            preProcess = preProcess || (nmKeyData == Keys.Escape) ;
            preProcess = preProcess || (nmKeyData == Keys.Up) || (nmKeyData == Keys.Down);
            preProcess = preProcess || (nmKeyData == Keys.Tab) || (nmKeyData == Keys.Enter);
            preProcess = preProcess || keyData.HasFlag(Keys.Control) || keyData.HasFlag(Keys.Alt); // menu shortcut
            if (propertiesExplorer.EntryWithTextBox == null) preProcess |= (nmKeyData == Keys.Delete); // the delete key is preferred by the textbox, if there is one 
            if (preProcess)
            {
                Substitutes.KeyEventArgs e = new Substitutes.KeyEventArgs((Substitutes.Keys) keyData);
                e.Handled = false;
                cadFrame.PreProcessKeyDown(e);
                if (e.Handled) return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public virtual bool OnCommand(string MenuId)
        {
            return false;
        }

        public virtual bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            return false;
        }

        public virtual void OnSelected(string MenuId, bool selected)
        {
            
        }
    }
}
