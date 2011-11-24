using System;

using NETGen.Core;
using NETGen.NetworkModels.Cluster;

namespace ModularityComputation
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if(args.Length<1)
			{
				Console.WriteLine("Usage: ModularityComputation [network_file] [visualize=false]");
				return;
			}
			ClusterNetwork n = ClusterNetwork.LoadNetwork(args[0]);
			Console.WriteLine("Q = {0:0.000}", n.NewmanModularity);
			
			if(args.Length==2 && args[1] == "true")
			{			
				NETGen.Visualization.NetworkVisualizer.Start(n, new NETGen.Layouts.FruchtermanReingold.FruchtermanReingoldLayout(10), new NETGen.Visualization.NetworkColorizer(), 800,600);			
				NETGen.Visualization.NetworkVisualizer.Layout.DoLayout();
			}
		}
	}
}
