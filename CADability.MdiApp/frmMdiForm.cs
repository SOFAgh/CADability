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
    public partial class frmMdiForm : Form
    {
        public frmMdiForm()
        {
            InitializeComponent();
        }

        private void frmMdiForm_Load(object sender, EventArgs e)
        {
            cadControl1.CadFrame.GenerateNewProject();
        }
    }
}
