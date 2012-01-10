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
				Console.WriteLine("Usage: ModularityComputation [network_file] [directed=false] [count_eii=false] [visualize=false] [reduce_to_LCC=false]");
				return;
			}
			bool directed = false;
			if(args.Length>=2)
				directed = Boolean.Parse(args[1]);
			ClusterNetwork n = ClusterNetwork.LoadNetwork(args[0], directed);					
			
			if(args.Length>4 && Boolean.Parse(args[4]) == true)
				n.ReduceToLargestConnectedComponent();
			
			if(args.Length>=3 && Boolean.Parse(args[2]) == true)
				Console.WriteLine("Q = {0:0.000000}", n.NewmanModularity);
			else
				Console.WriteLine("Q = {0:0.000000}", n.NewmanModularityWithoutEii);
			
			if(args.Length>3 && Boolean.Parse(args[3]) == true)
			{
				NETGen.Visualization.NetworkVisualizer.Start(n, new NETGen.Layouts.FruchtermanReingold.FruchtermanReingoldLayout(10), new NETGen.Visualization.NetworkColorizer(), 800,600);			
				NETGen.Visualization.NetworkVisualizer.Layout.DoLayout();
			}
		}
	}
}
