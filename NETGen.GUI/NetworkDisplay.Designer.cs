namespace NETGen.GUI
{
    partial class NetworkDisplay
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
            this.components = new System.ComponentModel.Container();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.drawPanel = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.networkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsGraphMLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsPDFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.visualizationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
            this.edgeWidthToolStripMenuItem = new System.Windows.Forms.ToolStripComboBox();
            this.layoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutNETGenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.drawPanel.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // drawPanel
            // 
            this.drawPanel.Controls.Add(this.menuStrip1);
            this.drawPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.drawPanel.Location = new System.Drawing.Point(0, 0);
            this.drawPanel.Name = "drawPanel";
            this.drawPanel.Size = new System.Drawing.Size(819, 439);
            this.drawPanel.TabIndex = 0;
            this.toolTip1.SetToolTip(this.drawPanel, "Test");
            this.drawPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.drawPanel_Paint);
            this.drawPanel.Resize += new System.EventHandler(this.drawPanel_Resize);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.networkToolStripMenuItem,
            this.visualizationToolStripMenuItem,
            this.layoutToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(819, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // networkToolStripMenuItem
            // 
            this.networkToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveAsGraphMLToolStripMenuItem,
            this.saveAsPDFToolStripMenuItem});
            this.networkToolStripMenuItem.Name = "networkToolStripMenuItem";
            this.networkToolStripMenuItem.Size = new System.Drawing.Size(64, 20);
            this.networkToolStripMenuItem.Text = "Network";
            // 
            // saveAsGraphMLToolStripMenuItem
            // 
            this.saveAsGraphMLToolStripMenuItem.Name = "saveAsGraphMLToolStripMenuItem";
            this.saveAsGraphMLToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.saveAsGraphMLToolStripMenuItem.Text = "Save as GraphML";
            this.saveAsGraphMLToolStripMenuItem.Click += new System.EventHandler(this.saveAsGraphMLToolStripMenuItem_Click);
            // 
            // saveAsPDFToolStripMenuItem
            // 
            this.saveAsPDFToolStripMenuItem.Name = "saveAsPDFToolStripMenuItem";
            this.saveAsPDFToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.saveAsPDFToolStripMenuItem.Text = "Save as PDF";
            this.saveAsPDFToolStripMenuItem.Click += new System.EventHandler(this.saveAsPDFToolStripMenuItem_Click);
            // 
            // visualizationToolStripMenuItem
            // 
            this.visualizationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripComboBox1,
            this.edgeWidthToolStripMenuItem});
            this.visualizationToolStripMenuItem.Name = "visualizationToolStripMenuItem";
            this.visualizationToolStripMenuItem.Size = new System.Drawing.Size(85, 20);
            this.visualizationToolStripMenuItem.Text = "Visualization";
            // 
            // toolStripComboBox1
            // 
            this.toolStripComboBox1.Items.AddRange(new object[] {
            "2",
            "4",
            "8",
            "16",
            "32",
            "64"});
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            this.toolStripComboBox1.Size = new System.Drawing.Size(121, 23);
            this.toolStripComboBox1.Text = "Vertex Size";
            this.toolStripComboBox1.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBox1_SelectedIndexChanged);
            // 
            // edgeWidthToolStripMenuItem
            // 
            this.edgeWidthToolStripMenuItem.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10"});
            this.edgeWidthToolStripMenuItem.Name = "edgeWidthToolStripMenuItem";
            this.edgeWidthToolStripMenuItem.Size = new System.Drawing.Size(121, 23);
            this.edgeWidthToolStripMenuItem.Text = "Edge Width";
            this.edgeWidthToolStripMenuItem.SelectedIndexChanged += new System.EventHandler(this.edgeWidthToolStripMenuItem_SelectedIndexChanged);
            // 
            // layoutToolStripMenuItem
            // 
            this.layoutToolStripMenuItem.Name = "layoutToolStripMenuItem";
            this.layoutToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
            this.layoutToolStripMenuItem.Text = "Layout";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem,
            this.aboutNETGenToolStripMenuItem});
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.aboutToolStripMenuItem.Text = "Help";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this.helpToolStripMenuItem.Text = "NETGen Documentation";
            // 
            // aboutNETGenToolStripMenuItem
            // 
            this.aboutNETGenToolStripMenuItem.Name = "aboutNETGenToolStripMenuItem";
            this.aboutNETGenToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this.aboutNETGenToolStripMenuItem.Text = "About NETGen";
            // 
            // NetworkDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(819, 439);
            this.Controls.Add(this.drawPanel);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "NetworkDisplay";
            this.Text = "NETGen Display";
            this.drawPanel.ResumeLayout(false);
            this.drawPanel.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem networkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsGraphMLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsPDFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem visualizationToolStripMenuItem;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox1;
        private System.Windows.Forms.ToolStripComboBox edgeWidthToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem layoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Panel drawPanel;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutNETGenToolStripMenuItem;
    }
}

