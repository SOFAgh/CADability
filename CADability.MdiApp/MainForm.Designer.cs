
namespace CADability.MdiApp
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnCloseAllMdi = new System.Windows.Forms.Button();
            this.btnAdd10MdiChild = new System.Windows.Forms.Button();
            this.btnAddMdiChild = new System.Windows.Forms.Button();
            this.btnEndlessLoop = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnEndlessLoop);
            this.panel1.Controls.Add(this.btnCloseAllMdi);
            this.panel1.Controls.Add(this.btnAdd10MdiChild);
            this.panel1.Controls.Add(this.btnAddMdiChild);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 497);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(879, 100);
            this.panel1.TabIndex = 2;
            // 
            // btnCloseAllMdi
            // 
            this.btnCloseAllMdi.Location = new System.Drawing.Point(93, 14);
            this.btnCloseAllMdi.Name = "btnCloseAllMdi";
            this.btnCloseAllMdi.Size = new System.Drawing.Size(75, 23);
            this.btnCloseAllMdi.TabIndex = 2;
            this.btnCloseAllMdi.Text = "Close All Mdi";
            this.btnCloseAllMdi.UseVisualStyleBackColor = true;
            this.btnCloseAllMdi.Click += new System.EventHandler(this.btnCloseAllMdi_Click);
            // 
            // btnAdd10MdiChild
            // 
            this.btnAdd10MdiChild.Location = new System.Drawing.Point(12, 43);
            this.btnAdd10MdiChild.Name = "btnAdd10MdiChild";
            this.btnAdd10MdiChild.Size = new System.Drawing.Size(75, 23);
            this.btnAdd10MdiChild.TabIndex = 1;
            this.btnAdd10MdiChild.Text = "Add 10x";
            this.btnAdd10MdiChild.UseVisualStyleBackColor = true;
            this.btnAdd10MdiChild.Click += new System.EventHandler(this.btnAdd10MdiChild_Click);
            // 
            // btnAddMdiChild
            // 
            this.btnAddMdiChild.Location = new System.Drawing.Point(12, 14);
            this.btnAddMdiChild.Name = "btnAddMdiChild";
            this.btnAddMdiChild.Size = new System.Drawing.Size(75, 23);
            this.btnAddMdiChild.TabIndex = 0;
            this.btnAddMdiChild.Text = "Add 1x";
            this.btnAddMdiChild.UseVisualStyleBackColor = true;
            this.btnAddMdiChild.Click += new System.EventHandler(this.btnAddMdiChild_Click);
            // 
            // btnEndlessLoop
            // 
            this.btnEndlessLoop.Location = new System.Drawing.Point(93, 43);
            this.btnEndlessLoop.Name = "btnEndlessLoop";
            this.btnEndlessLoop.Size = new System.Drawing.Size(75, 23);
            this.btnEndlessLoop.TabIndex = 3;
            this.btnEndlessLoop.Text = "Endless Loop";
            this.btnEndlessLoop.UseVisualStyleBackColor = true;
            this.btnEndlessLoop.Click += new System.EventHandler(this.btnEndlessLoop_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(879, 597);
            this.Controls.Add(this.panel1);
            this.IsMdiContainer = true;
            this.Name = "MainForm";
            this.Text = "Main Form Mdi";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnAddMdiChild;
        private System.Windows.Forms.Button btnCloseAllMdi;
        private System.Windows.Forms.Button btnAdd10MdiChild;
        private System.Windows.Forms.Button btnEndlessLoop;
    }
}

