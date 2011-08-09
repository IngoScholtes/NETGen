using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using NETGen.Core;
using System.Runtime.CompilerServices;

namespace NETGen.Visualization
{
    public sealed class CustomColorIndexer
    {
        private Dictionary<Vertex, SolidBrush> _customVertexColors = new Dictionary<Vertex, SolidBrush>();
        private Dictionary<Edge, Pen> _customEdgeColors = new Dictionary<Edge, Pen>();

        internal CustomColorIndexer()
        {
            _customEdgeColors = new Dictionary<Edge, Pen>();
            _customVertexColors = new Dictionary<Vertex, SolidBrush>();
        }

        internal SolidBrush GetVertexBrush(Vertex v)
        {
            if (_customVertexColors.ContainsKey(v))
                return _customVertexColors[v];
            else return (NetworkVisualizer.PresentationSettings.VertexBrush as SolidBrush);
        }

        internal Pen GetEdgePen(Edge e)
        {
            if (_customEdgeColors.ContainsKey(e))
                return _customEdgeColors[e];
            else return NetworkVisualizer.PresentationSettings.EdgePen;
        }

        public Color this[Vertex v]
        {
            get
            {
                if (_customVertexColors.ContainsKey(v))
                    return _customVertexColors[v].Color;
                else return (NetworkVisualizer.PresentationSettings.VertexBrush as SolidBrush).Color;
            }
            set
            {
                _customVertexColors[v] = new SolidBrush(value);
            }
        }

        public Color this[Edge e]
        {
            get
            {
                if (_customEdgeColors.ContainsKey(e))
                    return _customEdgeColors[e].Color;
                else return NetworkVisualizer.PresentationSettings.EdgePen.Color;
            }
            set
            {
                _customEdgeColors[e] = new Pen(value);
            }
        }
    }


    public static class NetworkVisualizer
    {
        private static BufferedGraphicsContext _context;
        private static BufferedGraphics _bufferedGraphics;
        private static Graphics _graphics;
        private static Dictionary<Edge, Point[]> _arrowDictionary = new Dictionary<Edge, Point[]>();        
        private static Network _network = null;
        public static CustomColorIndexer CustomColors { get; private set; }
        private static PresentationSettings _presentationSettings = null;

        /// <summary>
        /// The network that shall be visualized
        /// </summary>
        public static Network Network { 
            get { return _network; } 
            set { 
                _network = value;
                _arrowDictionary = new Dictionary<Edge, Point[]>();
                CustomColors = new CustomColorIndexer();
                
            }
        }

        /// <summary>
        /// The layouting algorithm that assigns nodes topology-dependent positions
        /// </summary>
        public static ILayoutProvider LayoutProvider { get; set; }

        /// <summary>
        /// Initializes the visualizer with a graphics handle and a displayrectangle
        /// </summary>
        /// <param name="g"></param>
        /// <param name="displayRectangle"></param>
        public static void Init(Graphics g, Rectangle displayRectangle)
        {
            _graphics = g;
            _context = BufferedGraphicsManager.Current;
            _bufferedGraphics = _context.Allocate(g, displayRectangle);
        }

        public static PresentationSettings PresentationSettings 
        { 
            get 
            {
                if (_presentationSettings == null)
                    _presentationSettings = new PresentationSettings(100d, 100d, 0d);
                return _presentationSettings;
            }
            set
            {
                _presentationSettings = value;
            }
        }
       
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Draw(bool layout=false)
        {
            if (layout)
                LayoutProvider.DoLayout(NetworkVisualizer.PresentationSettings.ActualWidth, NetworkVisualizer.PresentationSettings.ActualHeight, Network);

            if (_bufferedGraphics == null || _bufferedGraphics.Graphics == null)
                return;
            _bufferedGraphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            _bufferedGraphics.Graphics.Clear(Color.White);            
            if (PresentationSettings.DrawEdges)
                foreach(Edge e in Network.Edges)
                    DrawEdge(e);
            if (PresentationSettings.DrawVertices)
                foreach (Vertex v in Network.Vertices)
                    DrawVertex(v);

            _bufferedGraphics.Render(_graphics);
        }
        

        private static void DrawVertex(Vertex v)
        {
            PresentationSettings s = PresentationSettings;
            Vector3 p = LayoutProvider.GetPositionOfNode(v);

            if (!double.IsNaN(p.X) &&
               !double.IsNaN(p.Y) &&
               !double.IsNaN(p.Z))
                _bufferedGraphics.Graphics.FillEllipse(CustomColors.GetVertexBrush(v),
                  s.ScaleX(p.X) - s.VertexSize  / 2,
                  s.ScaleY(p.Y) - s.VertexSize / 2,
                  s.VertexSize,
                  s.VertexSize);           
        }

        private static void DrawEdge(Edge e)
        {
            PresentationSettings s = PresentationSettings;
            Vector3 p1 = LayoutProvider.GetPositionOfNode(e.Source);
            Vector3 p2 = LayoutProvider.GetPositionOfNode(e.Target);

            _bufferedGraphics.Graphics.DrawLine(CustomColors.GetEdgePen(e),
                    PresentationSettings.ScaleX(p1.X),
                    PresentationSettings.ScaleY(p1.Y),
                    PresentationSettings.ScaleX(p2.X),
                    PresentationSettings.ScaleY(p2.Y));

            if (e.EdgeType != EdgeType.Undirected)
            {
                if (!_arrowDictionary.ContainsKey(e))
                    _arrowDictionary[e] = getArrowPoints(p1, p2);
                _bufferedGraphics.Graphics.FillPolygon(PresentationSettings.ArrowBrush, _arrowDictionary[e]);
            }
        }

        /// <summary>
        /// Computes three polygon points for the arrow
        /// </summary>
        /// <returns>An array of three points</returns>
        private static Point[] getArrowPoints(Vector3 posA, Vector3 posB)
        {
            Point[] p = new Point[3];

            
            // length of hypothenuse and opposite side
            double h = Vector3.Distance(posA, posB);
            double a = Math.Abs(posB.Y - posA.Y);

            int vertexSize = PresentationSettings.VertexSize;

            if (h < vertexSize || h == 0)
                return p;

            // compute which the edge forms with a circle around the target node
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
            p[1] = new Point(s.ScaleX(posB.X) + s.ScaleX((int)(Math.Cos(angle - DegToRad(15d)) * vertexSize)) * 2, s.ScaleY(posB.Y) + s.ScaleY((int)(Math.Sin(angle - DegToRad(15d)) * vertexSize)) * 2);
            p[2] = new Point(s.ScaleX(posB.X) + s.ScaleX((int)(Math.Cos(angle + DegToRad(15d)) * vertexSize)) * 2, s.ScaleY(posB.Y) + s.ScaleY((int)(Math.Sin(angle + DegToRad(15d)) * vertexSize)) * 2);

            // done.
            return p;
        }

        private static double DegToRad(double p)
        {
            return (Math.PI / 180d) * p;
        }       
    }
}
