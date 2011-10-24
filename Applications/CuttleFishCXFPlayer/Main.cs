using System;
using System.Drawing;

using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.Layouts.Radial;
using NETGen.Layouts.RandomLayout;

namespace CuttleFishPlayer
{
	class MainClass
	{		
		public static void Main (string[] args)
		{			
			// Basic arg checking
			if (args.Length!=2 || !System.IO.File.Exists(args[0]) || !System.IO.File.Exists(args[1]))
			{
				Console.WriteLine("Usage: CuttleFishPlayer [cxf-File] [cef-File]");
				return;
			}
			
			// Used to color the network
			NetworkColorizer colorizer = new NetworkColorizer();			
			colorizer.DefaultBackgroundColor = Color.White;
			colorizer.DefaultVertexColor = Color.Black; 
			colorizer.DefaultEdgeColor = Color.Black;
			
			// Load network from cuttlefish CXF-File
			Network n = Network.LoadFromCXF(args[0]);			
			Logger.AddMessage(LogEntryType.AppMsg, string.Format("Initial network: {0} Nodes, {1} Edges", n.VertexCount, n.EdgeCount));
			
			// Start the visualizer with Fruchterman-Reingold layout
			NetworkVisualizer.Start(n, new RandomLayout(), colorizer);
			
			// Compute layout and save initial frame
			NetworkVisualizer.Layout.DoLayout();
			NetworkVisualizer.SaveCurrentImage("frame_0000.bmp");
			
			// Load network evolution from cuttlefish cef file
			NETGen.Dynamics.CEF.CEFPlayer player = new NETGen.Dynamics.CEF.CEFPlayer(args[1], n, colorizer);
			
			// On each evolution step, recompute layout and save current image
			player.OnStep+= new DiscreteDynamics.StepHandler( delegate(long time) {
				NetworkVisualizer.Layout.DoLayout();
				NetworkVisualizer.SaveCurrentImage(string.Format("frame_{0000}.bmp", time));
				Logger.AddMessage(LogEntryType.AppMsg, string.Format("Time {0000}: {1} Nodes, {2} Edges", time, n.VertexCount, n.EdgeCount));
			});
			
			// Ready, set, go ... 
			Logger.AddMessage(LogEntryType.AppMsg, "Press enter to step through network evolution ...");			
			Console.ReadLine();			
			player.Run();
			
			Network.SaveToGraphML("mono_network.graphml", n);
		}
	}
}
