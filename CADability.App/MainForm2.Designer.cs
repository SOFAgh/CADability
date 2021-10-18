namespace CADability.App
{
    partial class MainForm2
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
            this.cadControl = new CADability.Forms.CadControl();
            this.SuspendLayout();
            // 
            // cadControl
            // 
            this.cadControl.CreateMainMenu = false;
            this.cadControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cadControl.Location = new System.Drawing.Point(0, 0);
            this.cadControl.Name = "cadControl";
            this.cadControl.ProgressAction = null;
            this.cadControl.PropertiesExplorerVisible = true;
            this.cadControl.Size = new System.Drawing.Size(800, 450);
            this.cadControl.TabIndex = 0;
            this.cadControl.ToolbarsVisible = true;
            // 
            // MainForm2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.cadControl);
            this.KeyPreview = true;
            this.Name = "MainForm2";
            this.Text = "MainForm2";
            this.ResumeLayout(false);

        }

        #endregion

        private Forms.CadControl cadControl;
    }
}