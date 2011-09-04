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
    public class PDFExporter
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void CreatePDF(string path, Network n, PresentationSettings presentationSettings, ILayoutProvider layout, CustomColorIndexer customColors)
        {
            if (presentationSettings ==null)
                presentationSettings = new Visualization.PresentationSettings(2000d, 1000d, 0d);
            PdfSharp.Pdf.PdfDocument doc = new PdfDocument();
            doc.Info.Title = "Network";
            doc.Info.Subject = "Created by NETGen, the cross-platform network simulation framework";

            PdfPage page = doc.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            presentationSettings.DrawWidth = (int) page.Width.Point;
            presentationSettings.DrawHeight = (int) page.Height.Point;
            Draw(XGraphics.FromPdfPage(page), n, presentationSettings, layout, customColors);

            // Save the s_document...
			doc.Save(path);
        }

        private static void Draw(XGraphics g, Network n, PresentationSettings presentationSettings, ILayoutProvider layout, CustomColorIndexer customColors)
        {
            lock (n)
            {
                layout.DoLayout(presentationSettings.ActualWidth, presentationSettings.ActualHeight, n);

                if (g == null)
                    return;
                g.SmoothingMode = PdfSharp.Drawing.XSmoothingMode.HighQuality;
                g.Clear(Color.White);
                if (presentationSettings.DrawEdges)
                    foreach (Edge e in n.Edges)
                        DrawEdge(g, e, presentationSettings, layout, customColors);
                if (presentationSettings.DrawVertices)
                    foreach (Vertex v in n.Vertices)
                        DrawVertex(g, v, presentationSettings, layout, customColors);
            }

        }

        private static void DrawVertex(XGraphics g, Vertex v, PresentationSettings presentationSettings, ILayoutProvider layout, CustomColorIndexer customColors)
        {
            Vector3 p = layout.GetPositionOfNode(v);

            if (!double.IsNaN(p.X) &&
               !double.IsNaN(p.Y) &&
               !double.IsNaN(p.Z))
                g.DrawEllipse(customColors.HasCustomColor(v) ? customColors.GetVertexBrush(v) : presentationSettings.VertexBrush,
                   presentationSettings.ScaleX(p.X) - presentationSettings.VertexSize / 2,
                   presentationSettings.ScaleY(p.Y) - presentationSettings.VertexSize / 2,
                   presentationSettings.VertexSize,
                   presentationSettings.VertexSize);
        }

        private static void DrawEdge(XGraphics g, Edge e, PresentationSettings presentationSettings, ILayoutProvider layout, CustomColorIndexer customColors)
        {
            Vector3 p1 = layout.GetPositionOfNode(e.Source);
            Vector3 p2 = layout.GetPositionOfNode(e.Target);

            g.DrawLine(customColors.HasCustomColor(e) ? customColors.GetEdgePen(e) : presentationSettings.EdgePen,
                    presentationSettings.ScaleX(p1.X),
                    presentationSettings.ScaleY(p1.Y),
                    presentationSettings.ScaleX(p2.X),
                    presentationSettings.ScaleY(p2.Y));
        }       
    }
}
