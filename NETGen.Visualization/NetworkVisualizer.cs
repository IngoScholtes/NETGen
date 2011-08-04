using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using NETGen.Core;
using System.Runtime.CompilerServices;

namespace NETGen.Visualization
{
    public static class NetworkVisualizer
    {

        public static ILayoutProvider LayoutProvider
        {
            get;
            set;
        }

        public static Graphics Graphics
        {
            get; 
            set;
        }

        public static PresentationSettings PresentationSettings
        {
            get;
            set;
        }

        public static Network Network
        {
            get;
            set;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Draw()
        {
            Graphics.Clear(Color.White);            
            if (PresentationSettings.DrawEdges)
                foreach(Edge e in Network.Edges)
                    DrawEdge(e);
            if (PresentationSettings.DrawVertices)
                foreach (Vertex v in Network.Vertices)
                    DrawVertex(v);
        }

        private static void DrawVertex(Vertex v)
        {
            PresentationSettings s = PresentationSettings;
            Point3D p = LayoutProvider.GetPositionFromNode(v);

            if (!double.IsNaN(p.X) &&
               !double.IsNaN(p.Y) &&
               !double.IsNaN(p.Z))
                Graphics.FillEllipse(s.VertexBrush,
                  s.ScaleX(p.X) - s.VertexSize / 2,
                  s.ScaleY(p.Y) - s.VertexSize / 2,
                  s.VertexSize,
                  s.VertexSize);
        }

        private static void DrawEdge(Edge e)
        {
            Point3D p1 = LayoutProvider.GetPositionFromNode(e.Source);
            Point3D p2 = LayoutProvider.GetPositionFromNode(e.Target);

            Graphics.DrawLine(PresentationSettings.EdgePen,
                    PresentationSettings.ScaleX(p1.X),
                    PresentationSettings.ScaleY(p1.Y),
                    PresentationSettings.ScaleX(p2.X),
                    PresentationSettings.ScaleY(p2.Y));

            if (e.EdgeType != EdgeType.Undirected)
            {
                Point[] p = getArrowPoints(p1, p2);

                Graphics.FillPolygon(PresentationSettings.ArrowBrush, p);
            }
        }

        /// <summary>
        /// Computes three polygon points for the arrow
        /// </summary>
        /// <returns>An array of three points</returns>
        private static Point[] getArrowPoints(Point3D posA, Point3D posB)
        {
            Point[] p = new Point[3];

            
            // length of hypothenuse and opposite side
            double h = Point3D.Distance(posA, posB);
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

        public static void StartRendering(double fps)
        {
            if(timer == null)
            timer = new System.Threading.Timer(new System.Threading.TimerCallback(Render), null, 0, (int) (1000d / fps));
        }

        private static void Render(object o)
        {
            Draw();
        }

        public static void StopRendering()
        {
            timer.Dispose();
            timer = null;
        }       

        private static System.Threading.Timer timer;
    }
}
