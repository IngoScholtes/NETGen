#region MONO/NET System libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
#endregion

#region Much appreciated thirs party libraries
using MathNet.Numerics.Distributions;
#endregion

#region NETGen libraries
using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.NetworkModels.Cluster;
using NETGen.Dynamics.Synchronization;
#endregion

class Demo
{
	static ClusterNetwork network;
	static Kuramoto sync;
	static string resultfile = null;
	static Dictionary<int, double> _clusterOrder = new Dictionary<int, double>();
	
	static int nodes = 500; 
	static int edges = 4000;
	static int clusters = 10;
	static double K = 10d;
	
	// Keeps track whether clusters have already switched to pacemaker mode
	static Dictionary<int,bool> pacemaker_mode = new Dictionary<int, bool>();
		
    static void Main(string[] args)
    {
		try 
		{
			// The resultfile is given as command line argument
			//nodes = Int32.Parse(args[0]);	
			//edges = Int32.Parse(args[1]);
			//clusters = Int32.Parse(args[2]);
			resultfile = args[3];
		} catch {
			Console.WriteLine("Usage: mono Demo.exe [nodes] [edges] [clusters] [resultfile]");
			return;
		}

		// Create a network of the given size and modularity ... 
        network = new ClusterNetwork(nodes, edges, clusters, ClusterNetwork.GetMaxConnectedModularity(nodes, edges, clusters)-0.05d, true);
		
		Console.WriteLine("Created network with {0} vertices, {1} edges and modularity {2:0.00}", network.VertexCount, network.EdgeCount, network.NewmanModularity);

        // Network.SaveToEdgeFile(network, "network.edges");
                    
		// Run the real-time visualization
		NetworkColorizer colorizer = new NetworkColorizer();
		NetworkVisualizer.Start(network, new NETGen.Layouts.FruchtermanReingold.FruchtermanReingoldLayout(15), colorizer);
		NetworkVisualizer.Layout.DoLayoutAsync();
		
		// Distribution of natural frequencies
		double mean_frequency = 1d;
		Normal normal = new Normal(mean_frequency, mean_frequency/5d);
		
		sync = new Kuramoto(	network, 
								K, 
								colorizer,
								new Func<Vertex, Vertex[]>(v => { return new Vertex[] {v.RandomNeighbor}; })
								);
		
		double min  = double.MaxValue;
		double max = double.MinValue;
		
		foreach(Vertex v in network.Vertices)
		{
			sync.NaturalFrequencies[v] = normal.Sample();
			max = Math.Max(max, sync.NaturalFrequencies[v]);
			min = Math.Min(min, sync.NaturalFrequencies[v]);
		}
		Console.WriteLine(max-min);
		
		foreach(int g in network.ClusterIDs)
			pacemaker_mode[g] = false;
		
		
		sync.OnStep += new Kuramoto.StepHandler(recordOrder);
	
		Logger.AddMessage(LogEntryType.AppMsg, "Press enter to start synchronization experiment...");
		Console.ReadLine();
		
		// Run the simulation
		sync.Run();
		
		// Write the time series to the resultfile
		if(resultfile!=null)
			sync.WriteTimeSeries(resultfile);
    }		
	
	private static void recordOrder(double time)
	{
		// Compute and record global order parameter
		double globalOrder = sync.GetOrder(network.Vertices.ToArray());
		sync.AddDataPoint("order_global", globalOrder);
		
		double avgLocalOrder = 0d;		
		
		// Compute and record cluster order parameters
		foreach(int g in network.ClusterIDs)
		{
			double localOrder = sync.GetOrder(network.GetNodesInCluster(g));
			_clusterOrder[g] = localOrder;
			sync.AddDataPoint(string.Format("order_{0}", g), localOrder);		
			avgLocalOrder += localOrder;
			
			// Switch to pacemaker mode if cluster order exceeds threshold
			if(localOrder>0.95d && !pacemaker_mode[g])
			{
				pacemaker_mode[g] = true;
				
				// Probabilistically switch border nodes to pacemaker mode
				// Note: CouplingStrengths[... v, w ... ] is the strength by which phase advance of v is influenced when coupling to node w
				foreach(Vertex v in network.GetNodesInCluster(g))
				{
					if(network.HasInterClusterConnection(v))
						foreach(Vertex w in v.Neigbors)
							if(!network.HasInterClusterConnection(w))
								{
									Logger.AddMessage(LogEntryType.AppMsg, string.Format("Vertex switched to pacemaker mode", g));
									sync.CouplingStrengths[new Tuple<Vertex, Vertex>(v, w)] = 0d;
								}
				}
			}
			
		}
		avgLocalOrder /= (double) network.ClusterIDs.Length;
		
		Logger.AddMessage(LogEntryType.SimMsg, string.Format("Time = {000000}, Avg. Cluster Order = {1:0.00}, Global Order = {2:0.00}", time, avgLocalOrder, globalOrder));
	}
}
