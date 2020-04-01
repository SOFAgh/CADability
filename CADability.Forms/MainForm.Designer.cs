namespace CADability.Forms
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.cadCanvas = new CADability.Forms.CadCanvas();
            this.propertiesExplorer = new CADability.Forms.PropertiesExplorer();
            this.topToolStripContainer = new System.Windows.Forms.ToolStripContainer();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.topToolStripContainer.ContentPanel.SuspendLayout();
            this.topToolStripContainer.TopToolStripPanel.SuspendLayout();
            this.topToolStripContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // cadCanvas
            // 
            this.cadCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cadCanvas.Frame = null;
            this.cadCanvas.Location = new System.Drawing.Point(0, 0);
            this.cadCanvas.Margin = new System.Windows.Forms.Padding(4);
            this.cadCanvas.Name = "cadCanvas";
            this.cadCanvas.Size = new System.Drawing.Size(841, 751);
            this.cadCanvas.TabIndex = 0;
            this.cadCanvas.TabStop = false;
            // 
            // propertiesExplorer
            // 
            this.propertiesExplorer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertiesExplorer.Frame = null;
            this.propertiesExplorer.Location = new System.Drawing.Point(0, 0);
            this.propertiesExplorer.Margin = new System.Windows.Forms.Padding(4);
            this.propertiesExplorer.Name = "propertiesExplorer";
            this.propertiesExplorer.Size = new System.Drawing.Size(640, 751);
            this.propertiesExplorer.TabIndex = 0;
            this.propertiesExplorer.TabStop = false;
            // 
            // topToolStripContainer
            // 
            // 
            // topToolStripContainer.ContentPanel
            // 
            this.topToolStripContainer.ContentPanel.Controls.Add(this.splitContainer);
            this.topToolStripContainer.ContentPanel.Size = new System.Drawing.Size(1485, 751);
            this.topToolStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topToolStripContainer.Location = new System.Drawing.Point(0, 0);
            this.topToolStripContainer.Name = "topToolStripContainer";
            this.topToolStripContainer.Size = new System.Drawing.Size(1485, 778);
            this.topToolStripContainer.TabIndex = 1;
            this.topToolStripContainer.Text = "topToolStripContainer";
            // 
            // topToolStripContainer.TopToolStripPanel
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.cadCanvas);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.propertiesExplorer);
            this.splitContainer.Size = new System.Drawing.Size(1485, 751);
            this.splitContainer.SplitterDistance = 841;
            this.splitContainer.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1485, 778);
            this.Controls.Add(this.topToolStripContainer);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "CADability.Forms";
            this.topToolStripContainer.ContentPanel.ResumeLayout(false);
            this.topToolStripContainer.TopToolStripPanel.ResumeLayout(false);
            this.topToolStripContainer.TopToolStripPanel.PerformLayout();
            this.topToolStripContainer.ResumeLayout(false);
            this.topToolStripContainer.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private CadCanvas cadCanvas;
        private PropertiesExplorer propertiesExplorer;
        private System.Windows.Forms.ToolStripContainer topToolStripContainer;
        private System.Windows.Forms.SplitContainer splitContainer;
    }
}

