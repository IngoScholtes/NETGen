using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NETGen.Visualization
{
    /// <summary>
    /// A collection of settings associated with the drawing of a graph
    /// </summary>
    public class PresentationSettings
    {
        private int _drawWidth;
        private int _drawHeight;

        private double _actualWidth;
        private double _actualHeight;
        private double _actualDepth;
        private int _vertexSize;

        private bool _proportional = false;

        /// <summary>
        /// The constructor sets some initial values
        /// </summary>
        public PresentationSettings(double width, double height, double depth)
        {
            ActualHeight = height;
            ActualWidth = width;
            ActualDepth = depth;
            VertexBrush = Brushes.DarkCyan;
            VertexSize = (int)(width/100);
            EdgePen = Pens.DarkSlateGray;
            ArrowBrush = Brushes.Blue;
            DrawEdges = true;
            DrawVertices = true;
            XOffset = 0;
            YOffset = 0;
        }

        /// <summary>
        /// Whether or not to draw the edges of this graph
        /// </summary>
        public bool DrawEdges { get; set; }

        /// <summary>
        /// Whether or not to draw the vertices of this graph
        /// </summary>
        public bool DrawVertices { get; set; }

        /// <summary>
        /// The brush to use for drawing vertices
        /// </summary>
        public Brush VertexBrush { get; set; }

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
                    XScale = (float)_drawWidth / (float)ActualWidth;
                    YScale = (float)_drawHeight / (float)ActualHeight;
                }
            }
        }

        /// <summary>
        /// The pen to use for drawing edges
        /// </summary>
        public Pen EdgePen { get; set; }

        /// <summary>
        /// The brush to use for drawing arrows of directed edges
        /// </summary>
        public Brush ArrowBrush { get; set; }

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
        /// The actual width (x-coordinate) of the underlying space
        /// </summary>
        public double ActualWidth
        {
            get
            {
                return _actualWidth;
            }
            set
            {
                _actualWidth = value;
                Rescale();
            }
        }

        /// <summary>
        /// The actual height (y-coordinate) of the underlying space
        /// </summary>
        public double ActualHeight
        {
            get
            {
                return _actualHeight;
            }
            set
            {
                _actualHeight = value;
                Rescale();
            }
        }

        /// <summary>
        /// The actual depth (z-coordinate) of the underlying space
        /// </summary>
        internal double ActualDepth
        {
            get { return _actualDepth; }
            set
            {
                _actualDepth = value;
            }
        }

        /// <summary>
        /// The width of the drawing area
        /// </summary>
        public int DrawWidth
        {
            get { return _drawWidth; }
            set
            {
                _drawWidth = value;
                if (_drawWidth == 0)
                    return;
                Rescale();
            }
        }

        public void Rescale(double xscale, double yscale)
        {
            DrawWidth = (int) (_actualWidth * xscale);
            DrawHeight = (int) (_actualHeight * yscale);
            Rescale();
        }

        private void Rescale()
        {
            XScale = (float)_drawWidth / (float)ActualWidth;
            YScale = (float)_drawHeight / (float)ActualHeight;
            if (_proportional)
            {
                XScale = Math.Min(XScale, YScale);
                YScale = XScale;
            }
        }

        public int XOffset { get; set; }

        public int YOffset { get; set; }

        /// <summary>
        /// The height of the drawing area
        /// </summary>
        public int DrawHeight
        {
            get { return _drawHeight; }
            set
            {
                _drawHeight = value;
                if (_drawHeight == 0)
                    return;
                Rescale();
            }
        }

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

    }    
}
