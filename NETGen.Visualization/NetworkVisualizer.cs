using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using NETGen.Core;
using System.Runtime.CompilerServices;

namespace NETGen.Visualization
{    
	/// <summary>
	/// A class that manages the (double-buffered) drawing of a network to the screen
	/// </summary>
    public class NetworkVisualizer
    {
        private BufferedGraphicsContext _context;
        private BufferedGraphics _bufferedGraphics;
        private Graphics _graphics;    
        private Network _network = null;        
        private PresentationSettings _presentationSettings = null;
        private ILayoutProvider _layoutProvider = null;
        private bool _laidout = false;

        /// <summary>
        /// The network that shall be visualized
        /// </summary>
        public Network Network { 
            get { return _network; } 
            set { 
                _network = value;             
            }
        }

        /// <summary>
        /// The layouting algorithm that assigns nodes topology-dependent positions
        /// </summary>
        public ILayoutProvider LayoutProvider { 
            get { 
                return _layoutProvider; 
            } 
            set { 
                lock (typeof(NetworkVisualizer)) _layoutProvider = value; 
            } 
        }

        /// <summary>
        /// Initializes the visualizer with a graphics handle and a displayrectangle
        /// </summary>
        /// <param name="g"></param>
        /// <param name="displayRectangle"></param>
        public NetworkVisualizer(Network n, ILayoutProvider layout, PresentationSettings presentationSettings)
        {
            _network = n;
            _presentationSettings = presentationSettings;
            _layoutProvider = layout;
        }
		
		/// <summary>
		/// Sets the graphics objects that shall be used to draw the network
		/// </summary>
		/// <param name='g'>
		/// The graphics object to draw to
		/// </param>
		/// <param name='displayRectangle'>
		/// The display rectange
		/// </param>
        public void SetGraphics(Graphics g, Rectangle displayRectangle)
        {
            _graphics = g;
            _context = BufferedGraphicsManager.Current;
            _bufferedGraphics = _context.Allocate(g, displayRectangle);
            _bufferedGraphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            PresentationSettings.ScreenWidth = displayRectangle.Width;
            PresentationSettings.ScreenHeight = displayRectangle.Height;
        }
		
		/// <summary>
		/// Gets the current presentation settings.
		/// </summary>
		/// <value>
		/// The presentation settings.
		/// </value>
        public PresentationSettings PresentationSettings 
        { 
            get 
            {
                if (_presentationSettings == null)
                    _presentationSettings = new PresentationSettings(1000d, 1000d, 0d);
                return _presentationSettings;
            }
            private set
            {
                _presentationSettings = value;
            }
        }
		
		/// <summary>
		/// Forces the layout to be recomputed upon the next drawing operation
		/// </summary>
        public void ForceRelayout()
        {
            _laidout = false;
        }
       
		/// <summary>
		/// Draw the network
		/// </summary>
		/// <param name='force_relayout'>
		/// Whether or not to recompute the network layout
		/// </param>
        public void Draw(bool force_relayout=false)
        {
            if (Network.VertexCount == 0 || _graphics == null)
                return;
            if (force_relayout)
                LayoutProvider.DoLayout(PresentationSettings.WorldWidth, PresentationSettings.WorldHeight, Network);
            else if (!_laidout)
            {
                LayoutProvider.DoLayout(PresentationSettings.WorldWidth, PresentationSettings.WorldHeight, Network);
                _laidout = true;
            }
            lock (_context)
            {
                if (_bufferedGraphics == null || _bufferedGraphics.Graphics == null)
                    return;
                _bufferedGraphics.Graphics.Clear(PresentationSettings.BackgroundColor);
                lock (Network)
                {
                    if (PresentationSettings.DrawEdges)
                        foreach (Edge e in Network.Edges)
                            DrawEdge(e);
                    if (PresentationSettings.DrawVertices)
                        foreach (Vertex v in Network.Vertices)
                            DrawVertex(v);
                }

                _bufferedGraphics.Render(_graphics);
            }
        }
		
		/// <summary>
		/// Returns which vertex is at a given screen coordinate
		/// </summary>
		/// <returns>
		/// The vertex at the specified screen position
		/// </returns>
		/// <param name='screencoord'>
		/// A screen coordinate (e.g. the position the user has clicked)
		/// </param>
        public Vertex GetVertexAtPosition(Point screencoord)
        {
            Vertex v = null;
            if(LayoutProvider==null)
                return v;

            Vector3 worldcoord = PresentationSettings.ScreenToWorld(screencoord);
            

            double minDist = double.MaxValue;

                foreach(Vertex x in Network.Vertices)
                {

                    if (Vector3.Distance(worldcoord, LayoutProvider.GetPositionOfNode(x)) < minDist)
                    {
                        v = x;
                        minDist = Vector3.Distance(worldcoord, LayoutProvider.GetPositionOfNode(x));
                    }
                }
            return v;

        }
		
		/// <summary>
		/// Draws a single vertex
		/// </summary>
		/// <param name='v'>
		/// The vertex to draw
		/// </param>
        private void DrawVertex(Vertex v)
        {
            Vector3 p = LayoutProvider.GetPositionOfNode(v);

            if (!double.IsNaN(p.X) &&
               !double.IsNaN(p.Y) &&
               !double.IsNaN(p.Z))
                _bufferedGraphics.Graphics.FillEllipse(
					PresentationSettings.CustomColors.HasCustomColor(v)?PresentationSettings.CustomColors.GetVertexBrush(v):PresentationSettings.DefaultVertexBrush,
                  PresentationSettings.ScaleX(p.X) - PresentationSettings.VertexSize / 2,
                  PresentationSettings.ScaleY(p.Y) - PresentationSettings.VertexSize / 2,
                  PresentationSettings.VertexSize,
                  PresentationSettings.VertexSize);           
        }

		/// <summary>
		/// Draws a single edge
		/// </summary>
		/// <param name='e'>
		/// The edge to draw
		/// </param>
        private void DrawEdge(Edge e)
        {
            Vector3 p1 = LayoutProvider.GetPositionOfNode(e.Source);
            Vector3 p2 = LayoutProvider.GetPositionOfNode(e.Target);

            _bufferedGraphics.Graphics.DrawLine(
				PresentationSettings.CustomColors.HasCustomColor(e)?PresentationSettings.CustomColors.GetEdgePen(e):PresentationSettings.DefaultEdgePen,
                    PresentationSettings.ScaleX(p1.X),
                    PresentationSettings.ScaleY(p1.Y),
                    PresentationSettings.ScaleX(p2.X),
                    PresentationSettings.ScaleY(p2.Y));

            if (e.EdgeType != EdgeType.Undirected)            
                _bufferedGraphics.Graphics.FillPolygon(PresentationSettings.DefaultArrowBrush, getArrowPoints(p1, p2));
        }

        /// <summary>
        /// Computes three polygon points for the arrow
        /// </summary>
        /// <returns>An array of three points</returns>
        private Point[] getArrowPoints(Vector3 posA, Vector3 posB)
        {
            Point[] p = new Point[3];
            
            // length of hypothenuse and opposite side
            double h = Vector3.Distance(posA, posB);
            double a = Math.Abs(posB.Y - posA.Y);

            int vertexSize = PresentationSettings.VertexSize;

            if (h < vertexSize || h == 0)
                return p;

            // compute which angle the edge forms with a circle around the target node
            // we need to take the opposite angle in a right-angled triangle, so we use 90 - angle
            double angle = DegToRad(90d) - Math.Asin(a / h);

            // interpret the resulting angle differently, depending on the quadrant the source vertex is in
            if (posB.Y < posA.Y && posB.X < posA.X)
                // Source is below and right of Target
                angle = DegToRad(90d) - angle;
            else if (posB.Y < posA.Y && posB.X > posA.X)
                // Source is below and left of Target
                angle = DegToRad(90d) + angle;
            else if (posB.Y > posA.Y && posB.X < posA.X)
                // Source is above and right of target
                angle = DegToRad(270d) + angle;
            else if (posB.Y > posA.Y && posB.X > posA.X)
                // Source is above and left of target
                angle = DegToRad(270d) - angle;
            else if (posB.Y == posA.Y && posB.X > posA.X)
                // Source is left from Target
                angle = DegToRad(180d);
            else if (posB.Y == posA.Y && posB.X < posA.X)
                //Source is right from target
                angle = 0d;
            else if (posB.X == posA.X && posB.Y > posA.Y)
                // Source is above Target
                angle = DegToRad(270d);
            else if (posB.X == posA.X && posB.Y < posA.Y)
                //Source is below target
                angle = DegToRad(90d);

            PresentationSettings s = PresentationSettings;

            // compute the arrow positions
            p[0] = new Point(s.ScaleX(posB.X), s.ScaleY(posB.Y));
            p[1] = new Point(s.ScaleX(posB.X) + (int)(Math.Cos(angle - DegToRad(15d) * vertexSize) * 2), s.ScaleY(posB.Y) + (int)(Math.Sin(angle - DegToRad(15d)) * vertexSize) * 2);
            p[2] = new Point(s.ScaleX(posB.X) + (int)(Math.Cos(angle + DegToRad(15d) * vertexSize) * 2), s.ScaleY(posB.Y) + (int)(Math.Sin(angle + DegToRad(15d)) * vertexSize) * 2);

            // done.
            return p;
        }

		/// <summary>
		/// Helper function that transforms degrees into radian angles
		/// </summary>
		/// <returns>
		/// The angle in radians
		/// </returns>
		/// <param name='p'>
		/// the angle in degrees
		/// </param>
        private static double DegToRad(double d)
        {
            return (Math.PI / 180d) * d;
        }       
    }
}
