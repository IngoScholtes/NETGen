using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NETGen.Visualization
{
    /// <summary>
    /// A collection of settings associated with the drawing, coloring and scaling of a network
    /// </summary>
    public class PresentationSettings
    {
        private int _screenWidth;
        private int _screenHeight;

        private double _worldWidth;
        private double _worldHeight;
        private double _worldDepth;
		
        private int _vertexSize;

        private bool _proportional = false;
		
		/// <summary>
        /// Whether or not to draw the edges of this graph
        /// </summary>
        public bool DrawEdges { get; set; }

        /// <summary>
        /// Whether or not to draw the vertices of this graph
        /// </summary>
        public bool DrawVertices { get; set; }
		
			
        /// <summary>
        /// The default pen to use for drawing edges
        /// </summary>
        public Pen DefaultEdgePen { get; set; }
		
		/// <summary>
		/// Gets or sets the color of the background.
		/// </summary>
		/// <value>
		/// The color of the background.
		/// </value>
		public Color BackgroundColor { get; set; }
		
        /// <summary>
        /// The default brush to use for drawing vertices
        /// </summary>
        public Brush DefaultVertexBrush { get; set; }
		
        /// <summary>
        /// The default brush to use for drawing arrows of directed edges
        /// </summary>
        public Brush DefaultArrowBrush { get; set; }
		
		/// <summary>
		/// Gets or sets custom color assignments of individual nodes and edges
		/// </summary>
		/// <value>
		/// The custom colors.
		/// </value>
		public CustomColorIndexer CustomColors { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NETGen.Visualization.PresentationSettings"/> class.
		/// </summary>
		/// <param name='width'>
		/// The width of the space the network lives in
		/// </param>
		/// <param name='height'>
		/// The height of the space the network lives in
		/// </param>
		/// <param name='depth'>
		/// The depth of the space the network lives in
		/// </param>
        public PresentationSettings(double width, double height, double depth=0d)
        {
            WorldHeight = height;
            WorldWidth = width;
            WorldDepth = depth;            
            VertexSize = (int)(width/100);			
            
            XOffset = 0;
            YOffset = 0;
			
			DefaultVertexBrush = Brushes.DarkCyan;
            DefaultEdgePen = Pens.DarkSlateGray;
            DefaultArrowBrush = Brushes.Blue;
			BackgroundColor = Color.White;
			
			DrawEdges = true;
            DrawVertices = true;
            CustomColors = new CustomColorIndexer();
        }

        /// <summary>
        /// Gets or sets whether scaling shall keep proportions
        /// </summary>
        public bool Proportional
        {
            get { return _proportional; }
            set
            {
                _proportional = value;
                if (_proportional)
                {
                    XScale = Math.Min(XScale, YScale);
                    YScale = XScale;
                }
                else
                {
                    XScale = (float)_screenWidth / (float)WorldWidth;
                    YScale = (float)_screenHeight / (float)WorldHeight;
                }
            }
        }

        /// <summary>
        /// The (scaled) size of a vertex
        /// </summary>
        public int VertexSize 
        {
            get
            {
                return (int) (_vertexSize * XScale);
            }
            set
            {
                _vertexSize = value;
            }
        }

        /// <summary>
        /// The current horizontal scaling that is used when drawing the graph
        /// </summary>
        public float XScale { get; private set; }

        /// <summary>
        /// The current vertical scaling that is used when drawing the graph
        /// </summary>
        public float YScale { get; private set; }
		
		/// <summary>
		/// Gets or sets the X offset.
		/// </summary>
		/// <value>
		/// The X offset.
		/// </value>
        public int XOffset { get; set; }

		/// <summary>
		/// Gets or sets the Y offset.
		/// </summary>
		/// <value>
		/// The Y offset.
		/// </value>
        public int YOffset { get; set; }

        /// <summary>
        /// The width (x-coordinate) of the world space
        /// </summary>
        public double WorldWidth
        {
            get
            {
                return _worldWidth;
            }
            set
            {
                _worldWidth = value;
                Rescale();
            }
        }

        /// <summary>
        /// The height (y-coordinate) of the world space
        /// </summary>
        public double WorldHeight
        {
            get
            {
                return _worldHeight;
            }
            set
            {
                _worldHeight = value;
                Rescale();
            }
        }

        /// <summary>
        /// The depth (z-coordinate) of the world space
        /// </summary>
        internal double WorldDepth
        {
            get { return _worldDepth; }
            set
            {
                _worldDepth = value;
            }
        }

        /// <summary>
        /// The width (x-coordinate) of the screen area to which the network shall be drawn
        /// </summary>
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set
            {
                _screenWidth = value;
                if (_screenWidth == 0)
                    return;
                Rescale();
            }
        }
		
        /// <summary>
        /// The height (z-coordinate) of the screen area to which the network shall be drawn
        /// </summary>
        public int ScreenHeight
        {
            get { return _screenHeight; }
            set
            {
                _screenHeight = value;
                if (_screenHeight == 0)
                    return;
                Rescale();
            }
        }

		/// <summary>
		/// Performs a manual rescaling of the screen width and height
		/// </summary>
		/// <param name='xscale'>
		/// Xscale.
		/// </param>
		/// <param name='yscale'>
		/// Yscale.
		/// </param>
        public void Rescale(double xscale, double yscale)
        {
            ScreenWidth = (int) (_worldWidth * xscale);
            ScreenHeight = (int) (_worldHeight * yscale);
            Rescale();
        }

		/// <summary>
		/// Recomputes the X and Y scale according to the current world and screen sizes
		/// </summary>
        private void Rescale()
        {
            XScale = (float)_screenWidth / (float)WorldWidth;
            YScale = (float)_screenHeight / (float)WorldHeight;
            if (_proportional)
            {
                XScale = Math.Min(XScale, YScale);
                YScale = XScale;
            }
        }
		
		/// <summary>
		/// Transforms a point in screen coordinates to the network's world coordinates
		/// </summary>
		/// <returns>
		/// The to world.
		/// </returns>
		/// <param name='screencoord'>
		/// Screencoord.
		/// </param>
        public Vector3 ScreenToWorld(Point screencoord)
        {
            Vector3 worldcoord = new Vector3((screencoord.X / XScale) - XOffset, (screencoord.Y / YScale) - YOffset, 0d);
            return worldcoord;
        }

        /// <summary>
        /// Scales an x-coordinate according to the current draw size
        /// </summary>
        /// <param name="x"></param>
        /// <returns>scaled x-coordinate</returns>
        public int ScaleX(double x)
        {
            return XOffset + (int)(x * XScale);
        }

        /// <summary>
        /// Scales an y-coordinate according to the current draw size
        /// </summary>
        /// <param name="y"></param>
        /// <returns>scaled y-coordinate</returns>
        public int ScaleY(double y)
        {
            return YOffset + (int)(y * YScale);
        }
		
		/// <summary>
		/// Clone this instance.
		/// </summary>
		public PresentationSettings Clone()
		{
			return MemberwiseClone() as PresentationSettings;
		}
		
    }    
}
