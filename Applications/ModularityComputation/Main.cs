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
				Console.WriteLine("Usage: ModularityComputation [network_file] [directed=false] [count_eii=false] [visualize=false] [reduce_to_LCC=false] [q_with_scalling=false]");
				return;
			}
			
			bool directed = false;
			bool q_with_scalling = false;
			
			if(args.Length>=2)
			directed = Boolean.Parse(args[1]);
			ClusterNetwork n = ClusterNetwork.LoadNetwork(args[0], directed);					
			
			if(args.Length>4 && Boolean.Parse(args[4]) == true)
				n.ReduceToLargestConnectedComponent();
			if(args.Length>5 && Boolean.Parse(args[5]) == true)
				q_with_scalling = true;
			
			if(q_with_scalling)
			{
				if(directed)
			{
				if(args.Length>=3 && Boolean.Parse(args[2]) == true)
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityDqws);
			else
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityWithoutEiiDqws);
			}
			else
			{
				if(args.Length>=3 && Boolean.Parse(args[2]) == true)
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityUqws);
			else
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityWithoutEiiUqws);
			}
			}
			else{
			if(directed)
			{
				if(args.Length>=3 && Boolean.Parse(args[2]) == true)
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityD);
			else
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityWithoutEiiD);
			}
			else
			{
				if(args.Length>=3 && Boolean.Parse(args[2]) == true)
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityU);
			else
				Console.WriteLine("Q = {0:0.0000000}", n.NewmanModularityWithoutEiiU);
			}
			}
			
			if(args.Length>3 && Boolean.Parse(args[3]) == true)
			{
				NETGen.Visualization.NetworkVisualizer.Start(n, new NETGen.Layouts.FruchtermanReingold.FruchtermanReingoldLayout(10), new NETGen.Visualization.NetworkColorizer(), 800,600);			
				NETGen.Visualization.NetworkVisualizer.Layout.DoLayout();
			}
		}
	}
}
