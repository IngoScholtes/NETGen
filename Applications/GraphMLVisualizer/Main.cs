using System;
using System.Drawing;

using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.Dynamics.Synchronization;

namespace GraphMLVisualizer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if(args.Length!=1 || !System.IO.File.Exists(args[0]))
			{
				Console.WriteLine("Usage: GraphMLVisualizer [graphmlfile]");
				return; 
			}
			
			Network n = Network.LoadFromGraphML(args[0]); 
			
			NetworkColorizer colorizer = new NetworkColorizer();
			colorizer.DefaultBackgroundColor = Color.White;
			colorizer.DefaultVertexColor = Color.DarkBlue;
			colorizer.DefaultEdgeColor = Color.DarkGray;
			
			NetworkVisualizer.Start(n, new FruchtermanReingoldLayout(15), colorizer);
			
			NetworkVisualizer.Layout.DoLayoutAsync();			
		}
	}
}
