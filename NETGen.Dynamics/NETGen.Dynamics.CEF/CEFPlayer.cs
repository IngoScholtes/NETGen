using System;
using System.IO;
using System.Drawing;

using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Dynamics.CEF
{
	public class CEFPlayer : DiscreteDynamics
	{
		string filename = null;
		string[] lines = null;
		Network network = null;
		NetworkColorizer colorizer = null;
		int filepos = 0;
		
		public CEFPlayer(string cefFile, Network n, NetworkColorizer c = null)
		{
			filename = cefFile; 
			network = n;
			colorizer = c;
			if(cefFile == null || !System.IO.File.Exists(cefFile))
				Logger.AddMessage(LogEntryType.Error, "Given cef-File does not exist.");
		}
		
		protected override void Init ()
		{
			base.Init ();
			lines = System.IO.File.ReadAllLines(filename);
			Logger.AddMessage(LogEntryType.SimMsg, "Succesfully read CEF-File.");
		}
		
		protected override void Finish ()
		{
			base.Finish ();
			Logger.AddMessage(LogEntryType.SimMsg, "Finishing playing CEF-File.");
		}
		
		protected override void TimeStep(long time)
		{
			bool in_step = true;
			while(in_step && filepos < lines.Length)
			{
				string line = lines[filepos];
				if(line.Contains("addNode"))
				{
					Vertex v = network.CreateVertex(ExtractNodeLabel(line));
					if(colorizer != null && extractColor(line)!=Color.Empty)
						colorizer[v] = extractColor(line);
				}
				else if (line.Contains("removeNode"))
				{
					string label = ExtractNodeLabel(line);
					Vertex v  = network.SearchVertex(label);
					network.RemoveVertex(v);
				}
				else if (line.Contains("addEdge"))
				{
					string src = ExtractSourceLabel(line);
					string tgt = ExtractTargetLabel(line);
					Vertex v = network.SearchVertex(src);
					Vertex w = network.SearchVertex(tgt);
					network.CreateEdge(v, w);					
				}
				else if (line.Contains("removeEdge"))
				{
					string src = ExtractSourceLabel(line);
					string tgt = ExtractTargetLabel(line);
					Vertex v = network.SearchVertex(src);
					Vertex w = network.SearchVertex(tgt);
					network.RemoveEdge(v, w);					
				}
				else if (line.Contains("editNode"))
				{
					string label = ExtractNodeLabel(line); 
					Vertex v = network.SearchVertex(label); 
					if(colorizer != null && extractColor(line)!=Color.Empty)
						colorizer[v] = extractColor(line);
				}
				else if (line.Contains("editEdge"))
				{
					string source = ExtractSourceLabel(line); 
					string target = ExtractTargetLabel(line); 
					Edge e = network.SearchVertex(source).GetEdgeToSuccessor(network.SearchVertex(target));
					if(colorizer != null && extractColor(line)!=Color.Empty)
						colorizer[e] = extractColor(line);
				}
				if(line.EndsWith("]"))
					in_step = false;
				filepos++;
			}
			if(filepos >= lines.Length)
				Stop();
		}
		
		static string ExtractNodeLabel (string s)
		{
			return s.Substring(s.IndexOf("(")+1, s.IndexOf(")") - s.IndexOf("(")-1);
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
	}
}

