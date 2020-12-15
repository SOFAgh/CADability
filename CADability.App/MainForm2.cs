using CADability.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CADability.App
{
    public partial class MainForm2 : Form
    {
        private string[] args;
        private ProgressForm progressForm;

        public MainForm2(string[] args)
        {
            InitializeComponent();

            this.args = args;

            progressForm = new ProgressForm
            {
                TopLevel = true,
                Owner = this,
                Visible = false
            };

            cadControl.CreateMainMenu = true;
            cadControl.ProgressAction = (show, percent, title) => { progressForm.ShowProgressBar(show, percent, title); };
            cadControl.CommandRequest = CadControl_CommandRequest;
            // In this moment cadControl.CadFrame is not inizialized
            // We must use the OnLoad method
        }

        private bool CadControl_CommandRequest(string MenuId)
        {
            bool handled = false;
            if (MenuId == "MenuId.App.Exit")
            {   // this command cannot be handled by CADability.dll
                Application.Exit();
                handled = true;
            }
            return handled;
        }

        protected override void OnLoad(EventArgs e)
        {
            // interpret the command line arguments as a name of a file, which should be opened
            string fileName = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-"))
                {
                    fileName = args[i];
                    break;
                }
            }
            Project toOpen = null;
            if (!String.IsNullOrWhiteSpace(fileName))
            {
                try
                {
                    toOpen = Project.ReadFromFile(fileName);
                }
                catch { }
            }
            if (toOpen == null) cadControl.CadFrame.GenerateNewProject();
            else cadControl.CadFrame.Project = toOpen;

            base.OnLoad(e);
        }


    }
}
