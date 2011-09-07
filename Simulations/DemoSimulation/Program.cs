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
        static void Main(string[] args)
        {
			
			double bias;
			try{
					// The neighbor selection bias is given as command line argument
					bias = double.Parse(args[0]);
			}
			catch(Exception)
			{
				Console.WriteLine("Usage: mono ./DemoSimulation.exe [bias]");
				return;
			}
			
			
			// The number of clusters (c) and the nodes within a cluster (Nc)
            int c = 20;
            int Nc = 20;
			
			// The number of desired edges
            int m = 5 * c * Nc;

            // In order to yield a connected network, at least ...
            double inter_thresh = 1.2d * ((c * Math.Log(c)) / 2d);       
				// ... edges between communities are required
			
			// So the maximum number of edges within communities we s create is ... 
            double intra_edges = m - inter_thresh;

            // This yields a maximum value for p_i of ...
            double pi =  intra_edges / (c * Combinatorics.Combinations(Nc, 2));
			
			// From this we can compute p_e ...
            double p_e = (m - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2) * pi) / (Combinatorics.Combinations(c * Nc, 2) - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2));
            Console.WriteLine("Generating cluster network with p_i = {0:0.0000}, p_e = {1:0.0000}", pi, p_e);                
            
			// Create the network ... 
            ClusterNetwork network = new NETGen.NetworkModels.Cluster.ClusterNetwork(c, Nc, pi, p_e);
			Console.WriteLine("Created network has {0} vertices and {1} edges. Modularity = {2:0.00}", network.VertexCount, network.EdgeCount, network.NewmanModularity);
			
			
			// ... and reduce it to the GCC
            network.ReduceToLargestConnectedComponent();
                        
			// Run the OopenGL visualization				
			NetworkColorizer colorizer = new NetworkColorizer();			
			NetworkVisualizer.Start(network, new FruchtermanReingoldLayout(15), colorizer);						
			
			// Setup the synchronization simulation, passing the bias strategy as a lambda expression
			EpidemicSynchronization sync = new EpidemicSynchronization(
				network,
				colorizer,
				v => {
					Vertex neighbor = v.RandomNeighbor;
					double r = network.NextRandomDouble();

	                // classify neighbors
	                List<Vertex> intraNeighbors = new List<Vertex>();
	                List<Vertex> interNeighbors = new List<Vertex>();
	                ClassifyNeighbors(network, v, intraNeighbors, interNeighbors);
	
	                // biasing strategy ... 
	                if (r <= bias && interNeighbors.Count > 0)
	                    neighbor = interNeighbors.ElementAt(network.NextRandom(interNeighbors.Count));
					return neighbor;
				},
				0.3d);
			
			// Run the simulation synchronously 
			sync.Run();
			
			// Collect and print the results
            SyncResults res = sync.Collect();			
           	Console.WriteLine("Order {0:0.00} reached after {1} rounds for bias {2:0.00}", res.order, res.time, bias);
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