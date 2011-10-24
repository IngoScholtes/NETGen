using System;
using System.Drawing;

using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.Dynamics.Synchronization;

namespace GraphVisualizer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if(args.Length!=3 || !System.IO.File.Exists(args[0]) || (args[1]!="graphml" && args[1]!="edgelist") || !System.IO.File.Exists(args[2]))
			{
				Console.WriteLine("Usage: NETGenVisualizer [file] [format] [highlight_nodes]");
				Console.WriteLine("\t\t format is 'edgelist' | 'graphml'");
				return; 
			}
			Network n;
			if (args[1] == "graphml")
				n = Network.LoadFromGraphML(args[0]); 
			else
				n = Network.LoadFromEdgeFile(args[0]);
			
			NetworkColorizer colorizer = new NetworkColorizer();
			colorizer.DefaultBackgroundColor = Color.White;
			colorizer.DefaultVertexColor = Color.DarkBlue;
			colorizer.DefaultEdgeColor = Color.DarkGray;
			
			string[] highlight_names = System.IO.File.ReadAllLines(args[2]);
			foreach(string name in highlight_names)
				colorizer[n.SearchVertex(name)] = Color.Orange;
			
			NetworkVisualizer.Start(n, new FruchtermanReingoldLayout(10), colorizer);
			
			NetworkVisualizer.Layout.DoLayoutAsync();
			
			Console.WriteLine("Enter name to select node or 'exit' to quit.");
			string command = "";
			do
			{
				command = Console.ReadLine();
				if(command!="exit")
				{
					NetworkVisualizer.SelectedVertex = n.SearchVertex(command);
					if(NetworkVisualizer.SelectedVertex == null)
						Console.WriteLine("Not found.");
				}
			} while (command!="exit");
		}
	}
}
