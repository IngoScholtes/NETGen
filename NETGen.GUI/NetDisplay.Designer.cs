namespace NETGen.GUI
{
    partial class NetDisplay
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
            this.drawPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // drawPanel
            // 
            this.drawPanel.Location = new System.Drawing.Point(12, 49);
            this.drawPanel.Name = "drawPanel";
            this.drawPanel.Size = new System.Drawing.Size(700, 378);
            this.drawPanel.TabIndex = 0;
            // 
            // NetDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 439);
            this.Controls.Add(this.drawPanel);
            this.Name = "NetDisplay";
            this.Text = "NETGen Display";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel drawPanel;
    }
}

