#region MONO/NET System libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
#endregion

#region Much appreciated thirs party libraries
using MathNet.Numerics;
#endregion

#region NETGen libraries
using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Layout.RandomLayout;
using NETGen.Layout.Positioned;
using NETGen.Layout.Radial;
using NETGen.NetworkModels.Cluster;
using NETGen.Dynamics.EpidemicSynchronization;
#endregion

namespace DemoSimulation
{
    class Program
    {
		static ClusterNetwork network;
		static EpidemicSynchronization sync;
		static double bias1 = 0d;
		static double bias2 = 0d;
		static double currentBias = 0d;
		
        static void Main(string[] args)
        {			
			double bias;
			try{
					// The neighbor selection bias is given as command line argument
					bias1 = double.Parse(args[0]);
					bias2 = double.Parse(args[1]);
			}
			catch(Exception)
			{
				Console.WriteLine("Usage: mono ./DemoSimulation.exe [initial_bias] [secondary_bias]");
				return;
			}
						
			// The number of clusters (c) and the nodes within a cluster (Nc)
            int c = 20;
            int Nc = 20;
			
			// The number of desired edges
            int m = 6 * c * Nc;			

            // In order to yield a connected network, at least ...
            double inter_thresh = 3d * ((c * Math.Log(c)) / 2d);
				// ... edges between communities are required
			
			// So the maximum number of edges within communities we s create is ... 
            double intra_edges = m - inter_thresh;
			
			Console.WriteLine("Number of intra_edge pairs = " + c * Combinatorics.Combinations(Nc, 2));
			Console.WriteLine("Number of inter_edge pairs = " + (Combinatorics.Combinations(c * Nc, 2) - (c * Combinatorics.Combinations(Nc, 2))));

            // Calculate the p_i necessary to yield the desired number of intra_edges
            double pi =  intra_edges / (c * Combinatorics.Combinations(Nc, 2));
			
			// From this we can compute p_e ...
            double p_e = (m - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2) * pi) / (Combinatorics.Combinations(c * Nc, 2) - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2));
            Console.WriteLine("Generating cluster network with p_i = {0:0.0000}, p_e = {1:0.0000}", pi, p_e);                
            
			// Create the network ... 
            network = new NETGen.NetworkModels.Cluster.ClusterNetwork(c, Nc, pi, p_e);
			
			// ... and reduce it to the GCC
            network.ReduceToLargestConnectedComponent();	
			
			Console.WriteLine("Created network has {0} vertices and {1} edges. Modularity = {2:0.00}", network.VertexCount, network.EdgeCount, network.NewmanModularity);			
                        
			// Run the OopenGL visualization				
			NetworkColorizer colorizer = new NetworkColorizer();			
			NetworkVisualizer.Start(network, new FruchtermanReingoldLayout(15), colorizer);						
			
			currentBias = bias1;
			
			// Setup the synchronization simulation, passing the bias strategy as a lambda expression
			sync = new EpidemicSynchronization(
				network,
				colorizer,
				v => {
					Vertex neighbor = v.RandomNeighbor;
					double r = network.NextRandomDouble();

	                // classify neighbors
	                List<Vertex> intraNeighbors = new List<Vertex>();
	                List<Vertex> interNeighbors = new List<Vertex>();
	                ClassifyNeighbors(network, v, intraNeighbors, interNeighbors);
									
					neighbor = intraNeighbors.ElementAt(network.NextRandom(intraNeighbors.Count));
					
	                // biasing strategy ... 
	                if (r <= currentBias && interNeighbors.Count > 0)
	                    neighbor = interNeighbors.ElementAt(network.NextRandom(interNeighbors.Count));
	                    
					return neighbor;
				},
				0.9d);
			
			Dictionary<int, double> _groupMus = new Dictionary<int, double>();
			Dictionary<int, double> _groupSigmas = new Dictionary<int, double>();
			
			MathNet.Numerics.Distributions.Normal avgs_normal = new MathNet.Numerics.Distributions.Normal(300d, 50d);
			MathNet.Numerics.Distributions.Normal devs_normal = new MathNet.Numerics.Distributions.Normal(20d, 5d);
			
			for(int i=0; i<c; i++)
			{
				double groupAvg = avgs_normal.Sample();
				double groupStdDev = devs_normal.Sample();
				
				foreach(Vertex v in network.GetNodesInCluster(i))
				{
					sync._MuPeriods[v] = groupAvg;
					sync._SigmaPeriods[v] = groupStdDev;
				}
			}
			
			sync.OnStep+=new EpidemicSynchronization.StepHandler(collectLocalOrder);			
			
			// Run the simulation synchronously 
			sync.Run();
			
			Console.ReadKey();			
						
			// Collect and print the results
            SyncResults res = sync.Collect();			
           	Console.WriteLine("Order {0:0.00} reached after {1} rounds", res.order, res.time);
        }
		
		private static void collectLocalOrder(long time)
		{
			Dictionary<int, double> clusterOrder = new Dictionary<int, double>();
			
			bool switchCoupling = true; 
			
			Console.WriteLine("--- Step {000000}: Global Order = {1:0.00} Current Bias = {2:0.00} ---", time, sync.ComputeOrder(network.Vertices.ToArray()), currentBias);
			foreach(int g in network.ClusterIDs)
			{
					clusterOrder[g] = sync.ComputeOrder(network.GetNodesInCluster(g));
					Console.Write("{0:0.00} ", clusterOrder[g]);
					if(clusterOrder[g]<0.95d)
						switchCoupling = false;
			}
			Console.Write("\n");
			
			if(switchCoupling)
			{
				currentBias = bias2;
				foreach(KeyValuePair<Vertex,Vertex> vertexPair in sync._CouplingStrengths.Keys)
				{
					if(network.HasInterClusterConnection(vertexPair.Key) && !network.HasInterClusterConnection(vertexPair.Value))
						sync._CouplingStrengths[vertexPair] = 0d;
					else if(!network.HasInterClusterConnection(vertexPair.Key) && network.HasInterClusterConnection(vertexPair.Value))
						sync._CouplingStrengths[vertexPair] = 2.5d;
				}
			}
		}
		
		private static void ClassifyNeighbors(Network net, Vertex v, List<Vertex> intraNeighbors, List<Vertex> interNeighbors)
        {
            if (! (net is ClusterNetwork))
                return;
            foreach (Vertex x in v.Neigbors)
            {
                if ((net as ClusterNetwork).GetClusterForNode(x) == (net as ClusterNetwork).GetClusterForNode(v))
                    intraNeighbors.Add(x);
                else
                    interNeighbors.Add(x);
            }
        }  
    }
}