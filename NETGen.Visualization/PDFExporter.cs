using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;
using NETGen.Visualization;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace NETGen.Visualization
{
	
	/// <summary>
	/// This class can be used to export network visualizations to PDF files
	/// </summary>
    public class PDFExporter
    {
		
		/// <summary>
		/// Creates a PDF from a network visualization
		/// </summary>
		/// <param name='path'>
		/// The path where the PDF shall be saved to
		/// </param>
		/// <param name='n'>
		/// The network that shall be exported
		/// </param>
		/// <param name='presentationSettings'>
		/// The PresentationSettings that define the zooming, panning, default edge and vertex colors, etc. 
		/// </param>
		/// <param name='layout'>
		/// The layour provider that defines vertex positions
		/// </param>
		/// <param name='customColors'>
		/// Custom colors that change colors of vertices and edges individually
		/// </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void CreatePDF(string path, Network n, LayoutProvider layout, NetworkColorizer colorizer)
        {        
            PdfSharp.Pdf.PdfDocument doc = new PdfDocument();
            doc.Info.Title = "Network";
            doc.Info.Subject = "Created by NETGen";

            PdfPage page = doc.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;           

			// Draw the network to the xgraphics object
            Draw(XGraphics.FromPdfPage(page), n, layout, colorizer);

            // Save the s_document...
			doc.Save(path);
        }

        private static void Draw(XGraphics g, Network n, LayoutProvider layout, NetworkColorizer colorizer)
        {
            lock (n)
            {
                if (g == null)
                    return;
                g.SmoothingMode = PdfSharp.Drawing.XSmoothingMode.HighQuality;
                g.Clear(Color.White);
                foreach (Edge e in n.Edges)
                        DrawEdge(g, e, layout, colorizer);
                foreach (Vertex v in n.Vertices)
                        DrawVertex(g, v, layout, colorizer);
            }

        }

        private static void DrawVertex(XGraphics g, Vertex v, LayoutProvider layout, NetworkColorizer colorizer)
        {
            Vector3 p = layout.GetPositionOfNode(v);

            double size = Math.Min(2f, Math.Max(0.05d, Math.Log10(v.Degree)));

            if (!double.IsNaN(p.X) &&
               !double.IsNaN(p.Y) &&
               !double.IsNaN(p.Z))
                g.DrawEllipse(new SolidBrush(colorizer[v]), p.X - size/2d, p.Y - size/2d, size, size);
        }

        private static void DrawEdge(XGraphics g, Edge e, LayoutProvider layout, NetworkColorizer colorizer)
        {
            Vector3 p1 = layout.GetPositionOfNode(e.Source);
            Vector3 p2 = layout.GetPositionOfNode(e.Target);

            g.DrawLine(new Pen(colorizer[e], 0.05f), p1.X, p1.Y, p2.X, p2.Y);
        }       
    }
}
