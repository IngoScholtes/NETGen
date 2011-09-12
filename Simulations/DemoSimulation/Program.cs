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
		static string resultfile = null;
		
        static void Main(string[] args)
        {			
			double bias;
			try{
					// The neighbor selection bias is given as command line argument
					bias1 = double.Parse(args[0]);
					bias2 = double.Parse(args[1]);
					resultfile = args[2];
			}
			catch(Exception)
			{
				Console.WriteLine("Usage: mono ./DemoSimulation.exe [initial_bias] [secondary_bias] [resultfile]");
				return;
			}
									
			// Create the network with given size and modularity ... 
            network = new NETGen.NetworkModels.Cluster.ClusterNetwork(1000, 5000, 20, 0.9d);
			
			// Reduce it to the GCC in order to guarantee connectedness
            network.ReduceToLargestConnectedComponent();	
			
			Console.WriteLine("Created network has {0} vertices and {1} edges. Modularity = {2:0.00}", network.VertexCount, network.EdgeCount, network.NewmanModularity);			
                        
			// Run the OopenGL visualization
			NetworkColorizer colorizer = new NetworkColorizer();			
			NetworkVisualizer.Start(network, new FruchtermanReingoldLayout(20), colorizer);						
			
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
			
			for(int i=0; i<network.ClusterIDs.Length; i++)
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
			
			Console.ReadKey();
			
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
			
			System.IO.File.AppendAllText(resultfile, time.ToString());
			
			double globalOrder = sync.ComputeOrder(network.Vertices.ToArray());
			
			Console.WriteLine("--- Step {000000}: Global Order = {1:0.00} Current Bias = {2:0.00} ---", time, globalOrder, currentBias);
			System.IO.File.AppendAllText(resultfile, string.Format("\t{0:0.00}", globalOrder));
			foreach(int g in network.ClusterIDs)
			{
					clusterOrder[g] = sync.ComputeOrder(network.GetNodesInCluster(g));
					System.IO.File.AppendAllText(resultfile, string.Format("\t{0:0.00}", clusterOrder[g]));
					Console.Write("{0:0.00} ", clusterOrder[g]);
			}
			Console.Write("\n");
			System.IO.File.AppendAllText(resultfile, "\n");						
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