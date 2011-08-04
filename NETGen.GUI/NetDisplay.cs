using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.GUI
{
    public partial class NetDisplay : Form
    {

        public NetDisplay()
        {
            InitializeComponent();

            NetworkVisualizer.Graphics = drawPanel.CreateGraphics();

            drawPanel.Paint += new PaintEventHandler(drawPanel_Paint);
            this.FormClosed += new FormClosedEventHandler(NetDisplay_FormClosed);
            NetworkVisualizer.PresentationSettings.DrawWidth = drawPanel.Width;
            NetworkVisualizer.PresentationSettings.DrawHeight = drawPanel.Height;
            NetworkVisualizer.StartRendering(25d);
          
        }

        void NetDisplay_FormClosed(object sender, FormClosedEventArgs e)
        {
            NetworkVisualizer.StopRendering();
        }

        void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            NetworkVisualizer.Draw();
        }
        


    }
}
