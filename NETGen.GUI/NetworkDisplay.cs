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
using System.Runtime.CompilerServices;
using System.Threading;

namespace NETGen.GUI
{    

    /// <summary>
    /// Represents a graphical frontend that allows to visualize, save, export and interact with networks
    /// </summary>
    public partial class NetworkDisplay : Form
    {
        
        /// <summary>
        /// Are we in panning mode (mouse button being hold down)?
        /// </summary>
        private bool _pan = false;

        /// <summary>
        /// When the mouse button is released, did the user pan or was it a click?
        /// </summary>
        private bool _panned = false;

        /// <summary>
        /// The point at which panning started
        /// </summary>
        private Point _panStart = new Point();

        /// <summary>
        /// 
        /// </summary>
        private Point _offsetStart = new Point();
        
        public LayoutOptions LayoutOptions = new LayoutOptions();        
        private Thread _mainThread = null;
        public NetworkVisualizer NetworkVisualizer { get; set; }

        public int DisplayCounter { get; private set; }
		
		public static NetworkDisplay CreateDisplay(NetworkVisualizer visualizer, int fps = 25, LayoutOptions options = null)
		{
			return new NetworkDisplay(visualizer, fps, options);
		}

        /// <summary>
        /// Private constructor, instances are created by the static function CreateDisplay()
        /// </summary>
        private NetworkDisplay(NetworkVisualizer visualizer, int fps, LayoutOptions options)
        {
            if (options == null)
                LayoutOptions = new GUI.LayoutOptions();
            else
                LayoutOptions = options;
            NetworkVisualizer = visualizer;
            DisplayCounter++;

            // start event queue
            _mainThread = new Thread(new ThreadStart(new Action(delegate() {
                InitializeComponent();
                visualizer.SetGraphics(drawPanel.CreateGraphics(), drawPanel.DisplayRectangle);

                this.Text = visualizer.Network.Name;

                drawPanel.Paint += new PaintEventHandler(drawPanel_Paint);
                drawPanel.MouseDown += new MouseEventHandler(drawPanel_MouseDown);
                drawPanel.MouseUp += new MouseEventHandler(drawPanel_MouseUp);
                drawPanel.MouseMove += new MouseEventHandler(drawPanel_MouseMove);
                drawPanel.MouseClick += new MouseEventHandler(drawPanel_MouseClick);

                this.MouseWheel += new MouseEventHandler(drawPanel_MouseWheel);

                

                LayoutOptions.ItemAdded += new GUI.LayoutOptions.ItemAddedDelegate(LayoutOptions_ItemAdded);

                foreach (string name in options.LayoutNames)
                {
                    ToolStripItem i = layoutToolStripMenuItem.DropDownItems.Add(name);
                    i.Click += new EventHandler(i_Click);
                }

                System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o)
                {
                    while (!this.IsDisposed)
                    {
                        lock(this)
                            NetworkVisualizer.Draw();
                        System.Threading.Thread.Sleep(1000 / fps);
                    }
                }));

                Application.Run(this);
            })));
            _mainThread.SetApartmentState(ApartmentState.STA);
            _mainThread.Name = "STA Thread for NETGen Display";
            _mainThread.Start();
        }

        void LayoutOptions_ItemAdded(string name)
        {
            Invoke(new Action(delegate()
            {
                ToolStripItem i = layoutToolStripMenuItem.DropDownItems.Add(name);
                i.Click += new EventHandler(i_Click);
            }));
        }

        void drawPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (!_panned)
            {
                Vertex x = NetworkVisualizer.GetVertexAtPosition(e.Location);
                if(x!=null)
                    toolTip1.Show(x.Label, this, e.Location);
            }               
        }

        void i_Click(object sender, EventArgs e)
        {
            lock(this)
                NetworkVisualizer.LayoutProvider = LayoutOptions[(sender as ToolStripItem).Text];
            NetworkVisualizer.ForceRelayout();
        }

        void drawPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            NetworkVisualizer.PresentationSettings.Rescale(NetworkVisualizer.PresentationSettings.XScale + 0.001f * (float)e.Delta, NetworkVisualizer.PresentationSettings.YScale + 0.001f * (float)e.Delta);
            NetworkVisualizer.PresentationSettings.XOffset += (int) (0.001f * (float)e.Delta);
            NetworkVisualizer.PresentationSettings.YOffset += (int) (0.001f * (float)e.Delta);
        }

        void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            NetworkVisualizer.Draw();
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_pan)
            {
                _panned = true;
                NetworkVisualizer.PresentationSettings.XOffset = _offsetStart.X + e.Location.X - _panStart.X;
                NetworkVisualizer.PresentationSettings.YOffset = _offsetStart.Y + e.Location.Y - _panStart.Y;
            }
        }

        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {
            _pan = true;
            _panStart = e.Location;
            _offsetStart = new Point(NetworkVisualizer.PresentationSettings.XOffset, NetworkVisualizer.PresentationSettings.YOffset);
        }

        private void drawPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _pan = false;
            _panned = false;
        }

        private void drawPanel_Resize(object sender, EventArgs e)
        {
            NetworkVisualizer.SetGraphics(drawPanel.CreateGraphics(), drawPanel.DisplayRectangle);
            NetworkVisualizer.PresentationSettings.ScreenWidth = drawPanel.Width;
            NetworkVisualizer.PresentationSettings.ScreenHeight = drawPanel.Height;
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            NetworkVisualizer.PresentationSettings.VertexSize = Int32.Parse((sender as ToolStripItem).Text);
        }

        private void edgeWidthToolStripMenuItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            NetworkVisualizer.PresentationSettings.DefaultEdgePen = new Pen(NetworkVisualizer.PresentationSettings.DefaultEdgePen.Color, (float) Int32.Parse((sender as ToolStripItem).Text));
        }

        private void saveAsGraphMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "GraphML (*.graphml)|*.graphml|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Network.SaveToGraphML(saveFileDialog1.FileName, NetworkVisualizer.Network);
            }
        }

        private void saveAsPDFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Portable Document Format (*.pdf)|*.pdf|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                PDFExporter.CreatePDF(saveFileDialog1.FileName, NetworkVisualizer.Network, NetworkVisualizer.PresentationSettings, NetworkVisualizer.LayoutProvider);
            }
        }   
    }    
}
