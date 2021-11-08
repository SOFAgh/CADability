
namespace CADability.MdiApp
{
    partial class frmMdiForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cadControl1 = new CADability.Forms.CadControl();
            this.SuspendLayout();
            // 
            // cadControl1
            // 
            this.cadControl1.CreateMainMenu = false;
            this.cadControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cadControl1.Location = new System.Drawing.Point(0, 0);
            this.cadControl1.Name = "cadControl1";
            this.cadControl1.ProgressAction = null;
            this.cadControl1.PropertiesExplorerVisible = true;
            this.cadControl1.Size = new System.Drawing.Size(800, 450);
            this.cadControl1.TabIndex = 0;
            this.cadControl1.ToolbarsVisible = true;
            // 
            // frmMdiForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.cadControl1);
            this.KeyPreview = true;
            this.Name = "frmMdiForm";
            this.Text = "CadControl Mdi Form";
            this.Load += new System.EventHandler(this.frmMdiForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Forms.CadControl cadControl1;
    }
}