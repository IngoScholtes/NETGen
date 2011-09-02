using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using NETGen.Core;
using System.Runtime.CompilerServices;

namespace NETGen.Visualization
{    
    public class NetworkVisualizer
    {
        private BufferedGraphicsContext _context;
        private BufferedGraphics _bufferedGraphics;
        private Graphics _graphics;    
        private Network _network = null;
        public CustomColorIndexer CustomColors { get; set; }
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
                CustomColors = new CustomColorIndexer();                
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
            CustomColors = new CustomColorIndexer();
        }

        public void SetGraphics(Graphics g, Rectangle displayRectangle)
        {
            _graphics = g;
            _context = BufferedGraphicsManager.Current;
            _bufferedGraphics = _context.Allocate(g, displayRectangle);
            _bufferedGraphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            PresentationSettings.DrawWidth = displayRectangle.Width;
            PresentationSettings.DrawHeight = displayRectangle.Height;
        }

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

        public void ForceRelayout()
        {
            _laidout = false;
        }
       
        public void Draw(bool force_relayout=false)
        {
            if (Network.VertexCount == 0 || _graphics == null)
                return;
            if (force_relayout)
                LayoutProvider.DoLayout(PresentationSettings.ActualWidth, PresentationSettings.ActualHeight, Network);
            else if (!_laidout)
            {
                LayoutProvider.DoLayout(PresentationSettings.ActualWidth, PresentationSettings.ActualHeight, Network);
                _laidout = true;
            }
            lock (_context)
            {
                if (_bufferedGraphics == null || _bufferedGraphics.Graphics == null)
                    return;
                _bufferedGraphics.Graphics.Clear(Color.White);
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

        private void DrawVertex(Vertex v)
        {
            Vector3 p = LayoutProvider.GetPositionOfNode(v);

            if (!double.IsNaN(p.X) &&
               !double.IsNaN(p.Y) &&
               !double.IsNaN(p.Z))
                _bufferedGraphics.Graphics.FillEllipse(CustomColors.HasCustomColor(v)?CustomColors.GetVertexBrush(v):PresentationSettings.VertexBrush,
                  PresentationSettings.ScaleX(p.X) - PresentationSettings.VertexSize / 2,
                  PresentationSettings.ScaleY(p.Y) - PresentationSettings.VertexSize / 2,
                  PresentationSettings.VertexSize,
                  PresentationSettings.VertexSize);           
        }

        private void DrawEdge(Edge e)
        {
            Vector3 p1 = LayoutProvider.GetPositionOfNode(e.Source);
            Vector3 p2 = LayoutProvider.GetPositionOfNode(e.Target);

            _bufferedGraphics.Graphics.DrawLine(CustomColors.HasCustomColor(e)?CustomColors.GetEdgePen(e):PresentationSettings.EdgePen,
                    PresentationSettings.ScaleX(p1.X),
                    PresentationSettings.ScaleY(p1.Y),
                    PresentationSettings.ScaleX(p2.X),
                    PresentationSettings.ScaleY(p2.Y));

            if (e.EdgeType != EdgeType.Undirected)            
                _bufferedGraphics.Graphics.FillPolygon(PresentationSettings.ArrowBrush, getArrowPoints(p1, p2));
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

        private static double DegToRad(double p)
        {
            return (Math.PI / 180d) * p;
        }       
    }
}
