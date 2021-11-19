using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CADability.MdiApp
{
    public partial class MainForm : Form
    {
        int mdiCounter;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnAddMdiChild_Click(object sender, EventArgs e)
        {
            mdiCounter++;

            frmMdiForm mdiForm = new frmMdiForm();
            mdiForm.Text = "CadControl Mdi Form No." + mdiCounter.ToString();
            mdiForm.MdiParent = this;
            mdiForm.Show();
        }

        private void btnAdd10MdiChild_Click(object sender, EventArgs e)
        {
            for(int i=0;i<10;i++)
            {
                mdiCounter++;

                frmMdiForm mdiForm = new frmMdiForm();
                mdiForm.Text = "CadControl Mdi Form No." + mdiCounter.ToString();
                mdiForm.MdiParent = this;
                mdiForm.Show();
            }            
        }

        private void btnCloseAllMdi_Click(object sender, EventArgs e)
        {
            foreach (var frm in this.MdiChildren)
                frm.Close();
            GC.Collect();
            long totalMemory = GC.GetTotalMemory(true);
            System.Diagnostics.Trace.WriteLine("CloseAllMdi, totalMemory: " + totalMemory.ToString());
        }

        private void btnEndlessLoop_Click(object sender, EventArgs e)
        {
            while(true)
            {
                mdiCounter++;

                frmMdiForm mdiForm = new frmMdiForm();
                mdiForm.Text = "CadControl Mdi Form No." + mdiCounter.ToString();
                mdiForm.MdiParent = this;
                mdiForm.Show();

                if(mdiCounter % 10 == 0)
                {
                    foreach (var frm in this.MdiChildren)
                        frm.Close();

                    GC.Collect();
                    long totalMemory = GC.GetTotalMemory(true);
                    System.Diagnostics.Trace.WriteLine("CloseAllMdi, totalMemory: " + totalMemory.ToString());
                    Application.DoEvents();
                }
            }
        }
    }
}
