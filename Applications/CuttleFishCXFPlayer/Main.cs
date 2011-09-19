using System;
using System.Drawing;

using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.Layouts.Radial;
using NETGen.Layouts.RandomLayout;

namespace CuttleFishCXFPlayer
{
	class MainClass
	{		
		public static void Main (string[] args)
		{
			string[] cxf = null;
			string[] cef = null;
			
			try{
				cxf = System.IO.File.ReadAllLines(args[0]);
				cef = System.IO.File.ReadAllLines(args[1]);
			}
			catch
			{
				Console.WriteLine("Usage: CuttleFishCXFPlayer [cxfFile] [cefFile]");
				// --------------
			}
			
			Network n = new Network();
			NetworkColorizer colorizer = new NetworkColorizer();
			
			colorizer.DefaultBackgroundColor = Color.White;
			colorizer.DefaultVertexColor = Color.Black; 
			colorizer.DefaultEdgeColor = Color.Black;
			
			// read the input network
			foreach(string s in cxf)
			{
				string type = s.Substring(0, s.IndexOf(":"));
				if(type=="node")
				{
					string label = ExtractNodeLabel(s);
					Vertex v = n.CreateVertex(label);
					if(extractColor(s)!=Color.Empty)
						colorizer[v] = extractColor(s);
				}
				else if (type=="edge")
				{
					string sourceLabel = ExtractSourceLabel (s);
					string targetLabel = ExtractTargetLabel (s);
					n.CreateEdge(n.SearchVertex(sourceLabel), n.SearchVertex(targetLabel));
				}
			}
			
			Console.WriteLine("Initial network: {0} Nodes, {1} Edges", n.VertexCount, n.EdgeCount);
			
			// Start the visualizer and compute the layout
			
			NetworkVisualizer.Start(n, new FruchtermanReingoldLayout(15), colorizer);
			
			int iteration = 0;
			
			NetworkVisualizer.ComputeLayout();
			NetworkVisualizer.SaveCurrentImage(string.Format("frame_{0}.bmp", iteration));
			
			Logger.AddMessage(LogEntryType.AppMsg, "Press enter to step through network evolution ...");
			Console.ReadLine();
			
			// iterate through the evolution file
			foreach(string line in cef)
			{
				if(line.StartsWith("["))	
				{	
					iteration++;
					Console.WriteLine("Iteration {0}: {1} Nodes, {2} Edges", iteration, n.VertexCount, n.EdgeCount);
					NetworkVisualizer.ComputeLayout();
					NetworkVisualizer.SaveCurrentImage(string.Format("frame_{0}.bmp", iteration));					
				}
				if(line.Contains("addNode"))
				{
					Vertex v = n.CreateVertex(ExtractNodeLabel(line));
					if(extractColor(line)!=Color.Empty)
						colorizer[v] = extractColor(line);
				}
				else if (line.Contains("removeNode"))
				{
					string label = ExtractNodeLabel(line);
					Vertex v  = n.SearchVertex(label);
					n.RemoveVertex(v);
				}
				else if (line.Contains("addEdge"))
				{
					string src = ExtractSourceLabel(line);
					string tgt = ExtractTargetLabel(line);
					Vertex v = n.SearchVertex(src);
					Vertex w = n.SearchVertex(tgt);
					n.CreateEdge(v, w);					
				}
				else if (line.Contains("removeEdge"))
				{
					string src = ExtractSourceLabel(line);
					string tgt = ExtractTargetLabel(line);
					Vertex v = n.SearchVertex(src);
					Vertex w = n.SearchVertex(tgt);
					n.RemoveEdge(v, w);					
				}
				else if (line.Contains("editNode"))
				{
					string label = ExtractNodeLabel(line); 
					Vertex v = n.SearchVertex(label); 
					if(extractColor(line)!=Color.Empty)
						colorizer[v] = extractColor(line);
				}
				else if (line.Contains("editEdge"))
				{
					string source = ExtractSourceLabel(line); 
					string target = ExtractTargetLabel(line); 
					Edge e = n.SearchVertex(source).GetEdgeToSuccessor(n.SearchVertex(target));
					if(extractColor(line)!=Color.Empty)
						colorizer[e] = extractColor(line);
				}
			}
		}
		
		static Color extractColor(string s)
		{
			if( !s.Contains("color"))
				return Color.Empty;
			
			float r, g, b;
			string colors = s.Substring(s.IndexOf("color{")+6, s.IndexOf("}", s.IndexOf("color{")) - s.IndexOf("color{")-6);
			string[] colorComponents = colors.Split(',');
			r = float.Parse(colorComponents[0]);
			g = float.Parse(colorComponents[1]);
			b = float.Parse(colorComponents[2]);
			return Color.FromArgb((int) (r * 255f), (int) (g * 255f), (int) (b * 255f));
		}

		static string ExtractSourceLabel (string s)
		{
			return s.Substring(s.IndexOf("(")+1, s.IndexOf(",")-s.IndexOf("(")-1);
		}

		static string ExtractTargetLabel (string s)
		{
			return s.Substring(s.IndexOf(",")+1, s.IndexOf(")")-s.IndexOf(",")-1);
		}
				
		static string ExtractNodeLabel (string s)
		{
			return s.Substring(s.IndexOf("(")+1, s.IndexOf(")") - s.IndexOf("(")-1);
		}
	}
}
