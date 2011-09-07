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
        /// The offset that was set when panning began
        /// </summary>
        private Point _offsetStart = new Point();
        
        /// <summary>
        /// The main thread that takes care of event queue processing
        /// </summary>
        private Thread _mainThread = null;
		
		/// <summary>
		/// Gets or sets the network visualizer that will be used to visualize a network
		/// </summary>
		/// <value>
		/// The network visualizer.
		/// </value>
        public NetworkVisualizer NetworkVisualizer { get; set; }
		
		/// <summary>
		/// Gets the number of active Network Display instances
		/// </summary>
		/// <value>
		/// The number of running instances
		/// </value>
        public static int DisplayCounter { get; private set; }
		
		/// <summary>
		/// Can be used to set network layouting options that shall be available from the menu
		/// </summary>
		public LayoutOptions LayoutOptions = new LayoutOptions(); 
		
		/// <summary>
		/// Creates a new Network Display frontend and starts rendering using the specified network visualizer
		/// </summary>
		/// <returns>
		/// The network display instance
		/// </returns>
		/// <param name='visualizer'>
		/// The network visualizer that takes care of layouting and rendering the network
		/// </param>
		/// <param name='fps'>
		/// The number of frames per seconds that will be rendered, default is 25
		/// </param>
		/// <param name='options'>
		/// The layouting options that shall be available from the menu
		/// </param>
		public static NetworkDisplay CreateDisplay(NetworkVisualizer visualizer, int fps = 25, LayoutOptions options = null)
		{
			return new NetworkDisplay(visualizer, fps, options);
		}

        /// <summary>
        /// Private constructor, instances are created by the static function CreateDisplay()
        /// </summary>
        private NetworkDisplay(NetworkVisualizer visualizer, int fps, LayoutOptions options)
        {
			if(options !=null)
            	LayoutOptions = options;
			else
				LayoutOptions = new LayoutOptions();
			
            NetworkVisualizer = visualizer;
			
            NetworkDisplay.DisplayCounter++;

            // start event queue in new main thread
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
				
				// Add the layout options to the menu
                LayoutOptions.ItemAdded += new GUI.LayoutOptions.ItemAddedDelegate(LayoutOptions_ItemAdded);
                foreach (string name in LayoutOptions.LayoutNames)
                {
                    ToolStripItem i = layoutToolStripMenuItem.DropDownItems.Add(name);
                    i.Click += new EventHandler(i_Click);
                }
				
				System.Threading.Timer t = new System.Threading.Timer(timerCallbackFunction, null, 50, 1000 / fps);
				
                Application.Run(this);											
            })));
			
			// Set the main thread to Single Thread Apartment
            _mainThread.SetApartmentState(ApartmentState.STA);
            _mainThread.Name = "STA Thread for NETGen Display";
			
			// Startup the thread ... 
            _mainThread.Start();
        }
			
		private void timerCallbackFunction(object state)
		{ 
		  	drawPanel.Invalidate();
		}

		
		/// <summary>
		/// Executed whenever a new layouting option is added
		/// </summary>
		/// <param name='name'>
		/// The name of the layouting option
		/// </param>
        private void LayoutOptions_ItemAdded(string name)
        {
            Invoke(new Action(delegate()
            {
                ToolStripItem i = layoutToolStripMenuItem.DropDownItems.Add(name);
                i.Click += new EventHandler(i_Click);
            }));
        }

		/// <summary>
		/// The user has clicked within the panel
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void drawPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (!_panned)
            {
                Vertex x = NetworkVisualizer.GetVertexAtPosition(e.Location);
                if(x!=null)
                    toolTip1.Show(x.Label, this, e.Location);
            }               
        }
		
		/// <summary>
		/// A layouting option has been clicked
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void i_Click(object sender, EventArgs e)
        {
            NetworkVisualizer.LayoutProvider = LayoutOptions[(sender as ToolStripItem).Text];
        }
		
		/// <summary>
		/// The mouse wheel has been turned in order to zoom the network
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void drawPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            NetworkVisualizer.PresentationSettings.Rescale(NetworkVisualizer.PresentationSettings.XScale + 0.001f * (float)e.Delta, NetworkVisualizer.PresentationSettings.YScale + 0.001f * (float)e.Delta);
            NetworkVisualizer.PresentationSettings.XOffset += (int) (0.001f * (float)e.Delta);
            NetworkVisualizer.PresentationSettings.YOffset += (int) (0.001f * (float)e.Delta);
        }
		
		/// <summary>
		/// This method is executed whenever the panel needs to repaint itself
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            NetworkVisualizer.Draw();
        }

		/// <summary>
		/// The mouse has been moved within the panel
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_pan)
            {
                _panned = true;
                NetworkVisualizer.PresentationSettings.XOffset = _offsetStart.X + e.Location.X - _panStart.X;
                NetworkVisualizer.PresentationSettings.YOffset = _offsetStart.Y + e.Location.Y - _panStart.Y;
            }
        }

		/// <summary>
		/// A mouse button has been pressed down. If it is the left one, we need to pan the network. 
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {
			if (e.Button == MouseButtons.Left)
			{
	            _pan = true;
	            _panStart = e.Location;
	            _offsetStart = new Point(NetworkVisualizer.PresentationSettings.XOffset, NetworkVisualizer.PresentationSettings.YOffset);
			}
        }
		
		/// <summary>
		/// A mouse button has been released. If it is the left one, we need to stop panning. 
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void drawPanel_MouseUp(object sender, MouseEventArgs e)
        {
			if (e.Button == MouseButtons.Left)
			{
	            _pan = false;
	            _panned = false;
			}
        }
		
		/// <summary>
		/// The panel has been resized. The presentation settings need to be changed accordingly.
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void drawPanel_Resize(object sender, EventArgs e)
        {
            NetworkVisualizer.SetGraphics(drawPanel.CreateGraphics(), drawPanel.DisplayRectangle);
            NetworkVisualizer.PresentationSettings.ScreenWidth = drawPanel.Width;
            NetworkVisualizer.PresentationSettings.ScreenHeight = drawPanel.Height;
        }
		
		/// <summary>
		/// The vertex size has been changed
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            NetworkVisualizer.PresentationSettings.VertexSize = Int32.Parse((sender as ToolStripItem).Text);
        }
		
		/// <summary>
		/// The edge size has been changed
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void edgeWidthToolStripMenuItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            NetworkVisualizer.PresentationSettings.DefaultEdgePen = new Pen(NetworkVisualizer.PresentationSettings.DefaultEdgePen.Color, (float) Int32.Parse((sender as ToolStripItem).Text));
        }
		
		/// <summary>
		/// The network shall be saved as a graphml file
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void saveAsGraphMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "GraphML (*.graphml)|*.graphml|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)           
                Network.SaveToGraphML(saveFileDialog1.FileName, NetworkVisualizer.Network);            
        }

		/// <summary>
		/// The current view of the network shall be saved as a pdf
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
        private void saveAsPDFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Portable Document Format (*.pdf)|*.pdf|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog()== System.Windows.Forms.DialogResult.OK)            
                PDFExporter.CreatePDF(saveFileDialog1.FileName, NetworkVisualizer.Network, NetworkVisualizer.PresentationSettings, NetworkVisualizer.LayoutProvider);            
        }   
    }    
}
