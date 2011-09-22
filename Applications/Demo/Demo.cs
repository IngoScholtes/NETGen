#region MONO/NET System libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
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
	static EpidemicSync sync;
	static string resultfile = null;
	static Dictionary<int, double> _clusterOrder = new Dictionary<int, double>();
	
	static int nodes = 1000; 
	static int edges = 4000;
	static int clusters = 20;		
	
	static double modularity = ClusterNetwork.GetMaxConnectedModularity(nodes, edges, clusters); 

	static Dictionary<int,bool> paceMakerMode = new Dictionary<int, bool>();

	static double IntraClusterStrength = 2d;
	static double InterClusterStrength = 2d;
	
	static bool use_pacemakers = false;
	
    static void Main(string[] args)
    {
		try 
		{
				// The resultfile is given as command line argument
			nodes = Int32.Parse(args[0]);	
			edges = Int32.Parse(args[1]);
			clusters = Int32.Parse(args[2]);
			use_pacemakers = Boolean.Parse(args[3]);
			resultfile = args[4];
		} catch {
			Console.WriteLine("Usage: mono Demo.exe [nodes] [edges] [clusters] [pacemakers=true|false] [resultfile]");
			return;
		}

		// Create a network of the given size and modularity ... 
        network = new ClusterNetwork(nodes, edges, clusters, modularity, true);
		
		Console.WriteLine("Created network with {0} vertices, {1} edges and modularity {2:0.00}", network.VertexCount, network.EdgeCount, network.NewmanModularity);
                    
		// Run the real-time visualization
		NetworkColorizer colorizer = new NetworkColorizer();			
		NetworkVisualizer.Start(network, new NETGen.Layouts.FruchtermanReingold.FruchtermanReingoldLayout(15), colorizer);	
		
		NetworkVisualizer.Layout.DoLayout();
		
		// Setup the synchronization simulation
		sync = new EpidemicSync(network, colorizer);
		
		// Assign randomly distributed distribution parameters to clusters
		Normal avgs_normal = new Normal(300d, 50d);

		// Natural frequencies in different groups are distributed according to normal distribution 
		// with different parameters. Parameters are drawn from superordinate normal distribution
		
		foreach(int i in network.ClusterIDs)
		{
			paceMakerMode[i] = false;
			double groupAvg = avgs_normal.Sample();
			double groupStdDev = groupAvg/5d;
			
			foreach(Vertex v in network.GetNodesInCluster(i))
			{
				sync.PeriodMeans[v] = groupAvg;
				sync.PeriodStdDevs[v] = groupStdDev;
			}
		}
		
		foreach(Edge e in network.Edges)
		{
			sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Source, e.Target)] = 2d;
			sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Target, e.Source)] = 2d;
		}
		
		sync.OnStep+=new EpidemicSync.StepHandler(collectLocalOrder);	
	
		
		Logger.AddMessage(LogEntryType.AppMsg, "Press enter to start synchronization experiment...");
		Console.ReadLine();	
		
		// Run the simulation asynchronously so we can stop it anytime
		sync.RunInBackground();														
					
		// Simulation can be stopped by pressing enter
		Console.ReadLine();							
		sync.Stop();
		
		// Write the time series to the resultfile
		if(resultfile!=null)
			sync.WriteTimeSeries(resultfile);
    }		
	
	private static void collectLocalOrder(long time)
	{		
		string line = "";
		foreach(Vertex v in network.Vertices)
			line += sync.Periods[v].ToString() + "\n";
		System.IO.File.WriteAllText(string.Format("frequ_{0}.dat", time), line);
		
		// Compute and record global order parameter
		double globalOrder = sync.ComputeOrder(network.Vertices.ToArray());			
		sync.AddDataPoint("order_global", globalOrder); 			
		
		double avgLocalOrder = 0d;
		
		// Compute and record cluster order parameters
		foreach(int g in network.ClusterIDs)
		{
			double localOrder = sync.ComputeOrder(network.GetNodesInCluster(g));
			_clusterOrder[g] = localOrder;
			sync.AddDataPoint(string.Format("order_{0}", g), localOrder);		
			avgLocalOrder += localOrder;
			
			
			if(localOrder>0.95d && !paceMakerMode[g])
			{
				paceMakerMode[g] = true;
				
				if(use_pacemakers)
				{
					Logger.AddMessage(LogEntryType.AppMsg, string.Format("Cluster {0} switched to pacemaker mode", g));
					
					// nodes with no links to other clusters become pacemakers, i.e. they are not influenced by nodes in the same cluster
					foreach(Vertex v in network.GetNodesInCluster(g))
					{
						if(network.HasInterClusterConnection(v))
							foreach(Vertex w in v.Neigbors)
								if(!network.HasInterClusterConnection(w))
									sync.CouplingStrengths[new Tuple<Vertex, Vertex>(v, w)] = 0d;
					}
				}
			}
			
		}
		
		if(time %100 == 0)
			Logger.AddMessage(LogEntryType.SimMsg, string.Format("Time = {000000}, Avg. Cluster Order = {1:0.00}, Global Order = {2:0.00}", time, avgLocalOrder/(double) network.ClusterIDs.Length, globalOrder));
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
