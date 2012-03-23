using System;
using System.Collections.Generic;

using NETGen.NetworkModels.Cluster;

using NETGen.Core;
using NETGen.pyspg;

using System.Linq;

namespace ModularizationModel
{ 
	public class ModularityModel : pyspgSimulation<ModularityModel>
	{
		[Parameter(ParameterType.Input, "Project Name", "")]
		string PrjName;
		
		[Parameter(ParameterType.Input, "Temperature", 0.001)]
		double T;
		
		[Parameter(ParameterType.Input, "Steps", 50000)]
		int Steps;
		
		#pragma	warning disable 0414	
		[Parameter(ParameterType.Output, "Number of Nodes")]
		long n_nodes;
		
		[Parameter(ParameterType.Output, "Number of Edges")]
		long n_edges;
		
		[Parameter(ParameterType.Output, "Number of Intermodule Edges Empirical")]
		long n_inter_edges_e;
		
		[Parameter(ParameterType.Output, "Number of Intermodule Edges Simulated")]
		long n_inter_edges_s;
		
		[Parameter(ParameterType.Output, "Number of Intramodule Edges Empirical")]
		long n_intra_edges_e;
		
		[Parameter(ParameterType.Output, "Number of Intramodule Edges Simulated")]
		long n_intra_edges_s;
		
		[Parameter(ParameterType.Output, "Number of Modules Empirical")]
		long n_modules_e;
		
		[Parameter(ParameterType.Output, "Number of Modules Simulated")]
		long n_modules_s;
		
		[Parameter(ParameterType.Output, "Q Empirical")]
		double q_e;

		[Parameter(ParameterType.Output, "Q Simulated")]
		double q_s;
		
		[Parameter(ParameterType.Output, "O Plus")]
		double o_p;

		[Parameter(ParameterType.Output, "O Minus")]
		double o_m;
		
		[Parameter(ParameterType.Output, "Average Module Size Empirical")]
		double av_module_s_e;
		
		[Parameter(ParameterType.Output, "Average Module Size Simulated")]
		double av_module_s_s;
		
		[Parameter(ParameterType.Output, "Sd Module Size Empirical")]
		double sd_module_s_e;
		
		[Parameter(ParameterType.Output, "Sd Module Size Simulated")]
		double sd_module_s_s;
		
		[Parameter(ParameterType.Output, "Average Node Module Size Empirical")]
		double av_n_module_s_e;
		
		[Parameter(ParameterType.Output, "Average Node Module Size Simulated")]
		double av_n_module_s_s;
		
		[Parameter(ParameterType.Output, "Sd Node Module Size Empirical")]
		double sd_n_module_s_e;
		
		[Parameter(ParameterType.Output, "Sd Node Module Size Simulated")]
		double sd_n_module_s_s;
		#pragma	warning restore 0414
		
		ClusterNetwork simulated_network;
		
		ClusterNetwork empirical_network;
		
		Dictionary<Vertex,int> simulated_module_assignments = new Dictionary<Vertex, int>();
		
		Random r = new Random();
				
		public static void Main (string[] args) { Init(args); }		
				
		void Change()
		{		
			// choose a random class ...
			Vertex v = simulated_network.RandomVertex;			
			
			Dictionary<int, int> ModuleDependencies = new Dictionary<int, int>();		
		
			for(int i=0; i<simulated_network.GetClustersCount; i++)
				ModuleDependencies[i] = 0;
			
			// Count the number of dependencies to the same module as well as to other modules
			foreach(Vertex w in v.Neigbors)			
					ModuleDependencies[simulated_module_assignments[w]]++;
			
			Dictionary<int, double> module_probs = new Dictionary<int, double>();
			
			double c = 0d;
			
			for(int i=0; i<simulated_network.GetClustersCount; i++)
			{
				module_probs[i] = Math.Exp( (double) ModuleDependencies[i] / T);
				c+= Math.Exp( (double) ModuleDependencies[i] / T);
			}
			
			double rand = r.NextDouble()*c;
			int pos = 0;
			double acc = module_probs[0];
			
			while(acc<rand)
			{
				pos++;
				acc += module_probs[pos];			
			}

			simulated_module_assignments[v] = pos;	
		}
		
		public override void RunSimulation ()
		{
			empirical_network = ClusterNetwork.LoadNetwork(PrjName + "_network.edges", true);
			
			simulated_network = ClusterNetwork.LoadNetwork(PrjName + "_network.edges", true);
			
			//assign module membership deterministically for the simulated network
			int my_V=0;
			int NClusters = simulated_network.GetClustersCount;
			foreach(Vertex v in simulated_network.Vertices)
			{
				simulated_module_assignments[v] = my_V++ % NClusters;
			}
			
			//shuffle the dictionary to provide a randomized assignment making sure all modules are present
			Random rand = new Random();
			simulated_module_assignments = simulated_module_assignments.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value);
			
			//run the simulation
			int time = 0;
			
			//System.IO.StreamWriter file = new System.IO.StreamWriter(PrjName + "out.txt");
			while (time < Steps)
			{
				Change();
				time++;
				//simulated_network.ResetClusters(simulated_module_assignments);
				//file.WriteLine("{0} {1} {2} {3} {4}",T, simulated_network.NewmanModularityDirected, simulated_network.EdgeCount, simulated_network.InterClusterEdgeNumber, simulated_network.IntraClusterEdgeNumber);
				//foreach(Vertex v in simulated_network.Vertices)
				//	simulated_module_assignments[v] = simulated_network.GetClusterForNode(v);
				
			}
			//file.Close();
			
			//shrink the clusterIDs to account for the possibility of the existance of empty modules
			simulated_network.ResetClusters(simulated_module_assignments);
			
			//results
			n_nodes=empirical_network.VertexCount;
			n_edges=empirical_network.EdgeCount;
			
			n_modules_e=empirical_network.GetClustersCount;
			n_modules_s=simulated_network.GetClustersCount;
			
			q_e=empirical_network.NewmanModularityDirected;
			q_s=simulated_network.NewmanModularityDirected;
			
			o_p=0d;
			o_m=0d;
			Vertex s_v;
			Vertex s_w;
			foreach(Vertex e_v in empirical_network.Vertices)
				foreach(Vertex e_w in empirical_network.Vertices)
				{
					if(e_v!=e_w)
					{
						s_v=simulated_network.SearchVertex(e_v.Label);
						s_w=simulated_network.SearchVertex(e_w.Label);
					
						if((simulated_network.GetClusterForNode(s_v)==simulated_network.GetClusterForNode(s_w))&&(empirical_network.GetClusterForNode(e_v)==empirical_network.GetClusterForNode(e_w)))
						{
							o_p++;
						}
						else
						{
							if((simulated_network.GetClusterForNode(s_v)!=simulated_network.GetClusterForNode(s_w))&&(empirical_network.GetClusterForNode(e_v)!=empirical_network.GetClusterForNode(e_w)))
							{
								o_m++;
							}
						}
					}
				}
			o_p /= ((n_nodes*n_nodes)-n_nodes);
			o_m /= ((n_nodes*n_nodes)-n_nodes);
			
			av_module_s_e = empirical_network.GetAverageClusterSize;
			av_module_s_s = simulated_network.GetAverageClusterSize;
			
			sd_module_s_e = empirical_network.GetStandardDeviationClusterSize;
			sd_module_s_s = simulated_network.GetStandardDeviationClusterSize;
			
			av_n_module_s_e = empirical_network.GetAverageNodeClusterSize;
			av_n_module_s_s = simulated_network.GetAverageNodeClusterSize;
			
			sd_n_module_s_e = empirical_network.GetStandardDeviationNodeClusterSize;
			sd_n_module_s_s = simulated_network.GetStandardDeviationNodeClusterSize;
			
			n_inter_edges_e = empirical_network.InterClusterEdgeNumber;
			n_intra_edges_e = empirical_network.IntraClusterEdgeNumber;
			
			n_inter_edges_s = simulated_network.InterClusterEdgeNumber;
			n_intra_edges_s = simulated_network.IntraClusterEdgeNumber;
		}
	}
}
