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
			}
			
			Network n = new Network();
			
			// read the input network
			foreach(string s in cxf)
			{
				string type = s.Substring(0, s.IndexOf(":"));
				if(type=="node")
				{
					string label = ExtractNodeLabel(s);
					n.CreateVertex(label);
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
			NetworkVisualizer.Start(n, new FruchtermanReingoldLayout(1));
			
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
					n.CreateVertex(ExtractNodeLabel(line));
				else if (line.Contains("addEdge"))
				{
					string src = ExtractSourceLabel(line);
					string tgt = ExtractTargetLabel(line);
					Vertex v = n.SearchVertex(src);
					Vertex w = n.SearchVertex(tgt);
					if(v==null)
						v = n.CreateVertex(src);
					if(w==null)
						w = n.CreateVertex(tgt);
					n.CreateEdge(v, w);					
				}
				else if (line.Contains("removeEdge"))
				{
					string src = ExtractSourceLabel(line);
					string tgt = ExtractTargetLabel(line);
					Vertex v = n.SearchVertex(src);
					Vertex w = n.SearchVertex(tgt);
					if(v==null && w != null)
						n.RemoveEdge(v, w);					
				}
			}
		}

		static string ExtractSourceLabel (string s)
		{
			return s.Substring(s.IndexOf("(")+1, s.IndexOf(",")-s.IndexOf("(")-1);;
		}

		static string ExtractTargetLabel (string s)
		{
			return s.Substring(s.IndexOf(",")+1, s.IndexOf(")")-s.IndexOf(",")-1);
		}
				
		static string ExtractNodeLabel (string s)
		{
			return s.Substring(s.IndexOf("label")+5).Replace("{", "").Replace("}", "");;
		}
	}
}
