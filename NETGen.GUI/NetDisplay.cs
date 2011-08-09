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

namespace NETGen.GUI
{    

    public partial class NetDisplay : Form
    {
        System.Threading.Timer _t;
        bool _pan = false;
        Point _panStart = new Point();
        Point _offsetStart = new Point();

        public static LayoutProviders LayoutProviders = new LayoutProviders();
        private static NetDisplay Instance;

        private NetDisplay(double fps)
        {
            InitializeComponent();            
            
            drawPanel.Paint += new PaintEventHandler(drawPanel_Paint);
            drawPanel.MouseDown+=new MouseEventHandler(drawPanel_MouseDown);
            drawPanel.MouseUp+=new MouseEventHandler(drawPanel_MouseUp);
            drawPanel.MouseMove+=new MouseEventHandler(drawPanel_MouseMove);
            this.MouseWheel += new MouseEventHandler(drawPanel_MouseWheel);

            this.FormClosed += new FormClosedEventHandler(NetDisplay_FormClosed);

            NetworkVisualizer.Init(drawPanel.CreateGraphics(), drawPanel.DisplayRectangle);
            NetworkVisualizer.PresentationSettings.DrawWidth = drawPanel.Width;
            NetworkVisualizer.PresentationSettings.DrawHeight = drawPanel.Height;

            if (_t == null)
                _t = new System.Threading.Timer(new System.Threading.TimerCallback(Render), null, 0, (int)(1000d / fps));           
        }

        public static void ShowDisplay(double fps)
        {           
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(runApp), fps);                
        }

        private static void runApp(object o)
        {
            Instance = new NetDisplay((double) o);
            LayoutProviders.ItemAdded += new GUI.LayoutProviders.ItemAddedDelegate(LayoutProviders_ItemAdded);
            Application.Run(Instance);
        }        

        static void LayoutProviders_ItemAdded(string name)
        {
            Instance.Invoke(new addItem(add), name);            
        }

        delegate void addItem(string name);

        static void add(string name)
        {
            ToolStripItem i = Instance.layoutToolStripMenuItem.DropDownItems.Add(name);
            i.Click += new EventHandler(i_Click);
        }

        static void i_Click(object sender, EventArgs e)
        {
            NetworkVisualizer.LayoutProvider = LayoutProviders[(sender as ToolStripItem).Text];
            NetworkVisualizer.Draw(true);
        }       

        void drawPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            NetworkVisualizer.PresentationSettings.Rescale(NetworkVisualizer.PresentationSettings.XScale + 0.0002f * (float)e.Delta, NetworkVisualizer.PresentationSettings.YScale + 0.0002f * (float)e.Delta);
            NetworkVisualizer.Draw();
        }

        void Render(object state)
        {
            this.Invalidate();
        }

        void NetDisplay_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_t != null)
                _t.Dispose();
        }

        void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            NetworkVisualizer.Draw();
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_pan)
            {
                NetworkVisualizer.PresentationSettings.XOffset = _offsetStart.X + e.Location.X - _panStart.X;
                NetworkVisualizer.PresentationSettings.YOffset = _offsetStart.Y + e.Location.Y - _panStart.Y;
                NetworkVisualizer.Draw();
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
        }

        private void drawPanel_Resize(object sender, EventArgs e)
        {
            NetworkVisualizer.Init(drawPanel.CreateGraphics(), drawPanel.DisplayRectangle);
            NetworkVisualizer.PresentationSettings.DrawWidth = drawPanel.Width;
            NetworkVisualizer.PresentationSettings.DrawHeight = drawPanel.Height;
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            NetworkVisualizer.PresentationSettings.VertexSize = Int32.Parse((sender as ToolStripItem).Text);
            NetworkVisualizer.Draw();
        }

        private void edgeWidthToolStripMenuItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            NetworkVisualizer.PresentationSettings.EdgePen = new Pen(NetworkVisualizer.PresentationSettings.EdgePen.Color, (float) Int32.Parse((sender as ToolStripItem).Text));
            NetworkVisualizer.Draw();
        }        
    }

    public sealed class LayoutProviders
    {
        private Dictionary<string, ILayoutProvider> layouts;

        internal delegate void ItemAddedDelegate(string name);
        internal event ItemAddedDelegate ItemAdded;

        internal LayoutProviders()
        {
            layouts = new Dictionary<string, ILayoutProvider>();
        }

        public ILayoutProvider this[string name]
        {
            get
            {
                return layouts.ContainsKey(name) ? layouts[name] : null;
            }
            set
            {
                bool added = false;
                if (!layouts.ContainsKey(name))
                    added = true;
                layouts[name] = value;
                if (added && ItemAdded != null)
                    ItemAdded(name);
            }
        }
    }
}
