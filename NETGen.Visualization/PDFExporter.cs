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
        public static void CreatePDF(string path, Network n, PresentationSettings presentationSettings, ILayoutProvider layout)
        {
            if (presentationSettings ==null)
                presentationSettings = new Visualization.PresentationSettings(2000d, 1000d, 0d);
            PdfSharp.Pdf.PdfDocument doc = new PdfDocument();
            doc.Info.Title = "Network";
            doc.Info.Subject = "Created by NETGen, the cross-platform network simulation framework";

            PdfPage page = doc.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;		
			
			PresentationSettings newSettings = presentationSettings.Clone();
			
            newSettings.WorldWidth = (int) page.Width.Point;			
            newSettings.WorldHeight = (int) page.Height.Point;
			newSettings.VertexSize *= (int) (newSettings.WorldWidth/presentationSettings.WorldWidth);

			// Draw the network to the xgraphics object
            Draw(XGraphics.FromPdfPage(page), n, presentationSettings, layout);

            // Save the s_document...
			doc.Save(path);
        }

        private static void Draw(XGraphics g, Network n, PresentationSettings presentationSettings, ILayoutProvider layout)
        {
            lock (n)
            {
                if (g == null)
                    return;
                g.SmoothingMode = PdfSharp.Drawing.XSmoothingMode.HighQuality;
                g.Clear(Color.White);
                foreach (Edge e in n.Edges)
                        DrawEdge(g, e, presentationSettings, layout);                
                foreach (Vertex v in n.Vertices)
                        DrawVertex(g, v, presentationSettings, layout);
            }

        }

        private static void DrawVertex(XGraphics g, Vertex v, PresentationSettings presentationSettings, ILayoutProvider layout)
        {
            Vector3 p = layout.GetPositionOfNode(v);

            if (!double.IsNaN(p.X) &&
               !double.IsNaN(p.Y) &&
               !double.IsNaN(p.Z))
                g.DrawEllipse(presentationSettings.CustomColors.HasCustomColor(v) ? presentationSettings.CustomColors.GetVertexBrush(v) : presentationSettings.DefaultVertexBrush,
                   presentationSettings.ScaleX(p.X) - presentationSettings.VertexSize / 2,
                   presentationSettings.ScaleY(p.Y) - presentationSettings.VertexSize / 2,
                   presentationSettings.VertexSize,
                   presentationSettings.VertexSize);
        }

        private static void DrawEdge(XGraphics g, Edge e, PresentationSettings presentationSettings, ILayoutProvider layout)
        {
            Vector3 p1 = layout.GetPositionOfNode(e.Source);
            Vector3 p2 = layout.GetPositionOfNode(e.Target);

            g.DrawLine(presentationSettings.CustomColors.HasCustomColor(e) ? presentationSettings.CustomColors.GetEdgePen(e) : presentationSettings.DefaultEdgePen,
                    presentationSettings.ScaleX(p1.X),
                    presentationSettings.ScaleY(p1.Y),
                    presentationSettings.ScaleX(p2.X),
                    presentationSettings.ScaleY(p2.Y));
        }       
    }
}
