namespace CADability.Forms
{
    partial class CadControl
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
            this.toolStripContainer = new System.Windows.Forms.ToolStripContainer();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.cadCanvas = new CADability.Forms.CadCanvas();
            this.propertiesExplorer = new CADability.Forms.PropertiesExplorer();
            this.toolStripContainer.ContentPanel.SuspendLayout();
            this.toolStripContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer
            // 
            // 
            // toolStripContainer.ContentPanel
            // 
            this.toolStripContainer.ContentPanel.Controls.Add(this.splitContainer);
            this.toolStripContainer.ContentPanel.Margin = new System.Windows.Forms.Padding(2);
            this.toolStripContainer.ContentPanel.Size = new System.Drawing.Size(1114, 607);
            this.toolStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer.Margin = new System.Windows.Forms.Padding(2);
            this.toolStripContainer.Name = "toolStripContainer";
            this.toolStripContainer.Size = new System.Drawing.Size(1114, 632);
            this.toolStripContainer.TabIndex = 1;
            this.toolStripContainer.Text = "topToolStripContainer";
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Margin = new System.Windows.Forms.Padding(2);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.cadCanvas);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.propertiesExplorer);
            this.splitContainer.Size = new System.Drawing.Size(1114, 607);
            this.splitContainer.SplitterDistance = 630;
            this.splitContainer.SplitterWidth = 3;
            this.splitContainer.TabIndex = 0;
            // 
            // cadCanvas
            // 
            this.cadCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cadCanvas.Frame = null;
            this.cadCanvas.Location = new System.Drawing.Point(0, 0);
            this.cadCanvas.Name = "cadCanvas";
            this.cadCanvas.Size = new System.Drawing.Size(630, 607);
            this.cadCanvas.TabIndex = 0;
            this.cadCanvas.TabStop = false;
            // 
            // propertiesExplorer
            // 
            this.propertiesExplorer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertiesExplorer.Frame = null;
            this.propertiesExplorer.Location = new System.Drawing.Point(0, 0);
            this.propertiesExplorer.Name = "propertiesExplorer";
            this.propertiesExplorer.Size = new System.Drawing.Size(481, 607);
            this.propertiesExplorer.TabIndex = 0;
            this.propertiesExplorer.TabStop = false;
            // 
            // CadControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStripContainer);
            this.Name = "CadControl";
            this.Size = new System.Drawing.Size(1114, 632);
            this.toolStripContainer.ContentPanel.ResumeLayout(false);
            this.toolStripContainer.ResumeLayout(false);
            this.toolStripContainer.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private CadCanvas cadCanvas;
        private PropertiesExplorer propertiesExplorer;
        private System.Windows.Forms.ToolStripContainer toolStripContainer;
        private System.Windows.Forms.SplitContainer splitContainer;
    }
}

