#region .NET/MONO System Libraries
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
#endregion

#region Much appreciated third-party Libraries
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
#endregion

#region NETGen Libraries
using NETGen.Core;
using NETGen.NetworkModels.Cluster;
using NETGen.Visualization;
using NETGen.Dynamics.Synchronization;
#endregion

public class ClusterSync
{
	
	/// <summary>
	/// Runs the cluster synchronization experiments, reading simulation parameters from the .config file
	/// </summary>
    public static void Main()
    {
		int nodes = Properties.Settings.Default.Nodes;
		int edges = Properties.Settings.Default.Edges;
		int clusters = Properties.Settings.Default.Clusters;
		int runs = Properties.Settings.Default.Runs;
		
		/// When to consider a network synchronized
		double orderThres = 0.99d;
		
		// Maximum time to wait for synchronization
		long timeThres = 10000;
				
		Logger.AddMessage(LogEntryType.AppMsg, 
			string.Format("Parameters: N={0}, M={1}, C={2}, Runs={3}, Weight_From={4}, Weight_To={5}, Weight_Step={6}", 
				nodes, 
				edges, 
				clusters, 
				runs, 
				Properties.Settings.Default.Weight_From, 
				Properties.Settings.Default.Weight_To, 
				Properties.Settings.Default.Weight_Step)
		);
		
		// Remove any previous result file
        System.IO.File.Delete(Properties.Settings.Default.ResultFile);
		
		// Explore parameter space for modularity and coupling strength weight
        for (double mod = ClusterNetwork.GetRandomModularity(nodes,clusters); 
			mod <= ClusterNetwork.GetMaxConnectedModularity(nodes, edges, clusters); 
			mod += Properties.Settings.Default.Modularity_Step)
        {
            string line = "";
			
    /*        for (double weight = Properties.Settings.Default.Weight_From; 
				weight <= Properties.Settings.Default.Weight_To; 
				weight += Properties.Settings.Default.Weight_Step)
            { */
				
				// Collects results of individual runs to compute mean and stddev
                List<double> results = new List<double>();
                List<double> modularity = new List<double>();
			
				ConcurrentDictionary<int, List<double>> order = new ConcurrentDictionary<int,List<double>>();
				
				Logger.AddMessage(LogEntryType.AppMsg, string.Format("Starting runs for modularity {0:0.00}", mod));
				
				// Parallely start runs for this parameter set ... 
                System.Threading.Tasks.Parallel.For(0, runs, j =>
                {                 						
					RunExperiment (nodes, edges, clusters, j, orderThres, timeThres, mod, 0d, results, modularity, order);
                });
			
				
				foreach(int time in (from o in order.Keys orderby o ascending select o))
				{			
					// Add mean and stddev to result string
                	line = string.Format (new CultureInfo ("en-US").NumberFormat, 
					"{0} {1} {2:0.000} {3:0.000}\t", 
					Statistics.Mean (modularity.ToArray()),
					time,
					Statistics.Mean (order[time].ToArray()), 
					Statistics.StandardDeviation (order[time].ToArray()));
				
					// Append string to file
	                System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
				}
				// Add a blank line separating the blocks
				System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
				
                Logger.AddMessage(LogEntryType.AppMsg, string.Format("Finished runs for modularity = {0:0.00}, Average time = {1:0.000000}", mod, Statistics.Mean(results.ToArray())));
      //      }      
        }
		Logger.AddMessage(LogEntryType.AppMsg, "Successfuly completed all experiments");
    }
	
	/// <summary>
	/// Runs an individual experiment.
	/// </summary>
	/// <param name='nodes'>
	/// The number of nodes of the cluster network
	/// </param>
	/// <param name='edges'>
	/// The number of edges of the cluster network
	/// </param>
	/// <param name='clusters'>
	/// The number of clusters in the cluster network
	/// </param>
	/// <param name='run'>
	/// The number of this particular experimental run
	/// </param>
	/// <param name='orderThres'>
	/// The order threshold
	/// </param>
	/// <param name='timeThres'>
	/// The time threshold
	/// </param>
	/// <param name='mod'>
	/// The modularity of the cluster network
	/// </param>
	/// <param name='weight'>
	/// The coupling strength weight
	/// </param>
	/// <param name='results'>
	/// The list that collects the synchronization time results
	/// </param>
	/// <param name='modularity'>
	/// The list that carries the modularity results
	/// </param>
	static void RunExperiment (int nodes, int edges, int clusters, int run, double orderThres, long timeThres, double mod, double weight, List<double> results, List<double> modularity, ConcurrentDictionary<int,List<double>> orderEvolution)
	{
		// Setup the experiment by creating the network and the synchronization module
        ClusterNetwork net = new ClusterNetwork(nodes, edges, clusters, mod);
    	EpidemicSync sync = new EpidemicSync(net);
		
		Dictionary<int,bool> pacemaker_mode = new Dictionary<int, bool>();
		
		// Assign randomly distributed distribution parameters to clusters
		Normal avgs_normal = new Normal(300d, 100d);		
					
		foreach(int i in net.ClusterIDs) 
		{
			// draw the distribution parameters from the above distribution ...
			double groupAvg = avgs_normal.Sample();
			double groupStdDev = groupAvg/5d;
			
			// assign individual values
			foreach(Vertex v in net.GetNodesInCluster(i))
			{
				sync.PeriodMeans[v] = groupAvg;
				sync.PeriodStdDevs[v] = groupStdDev;
			}
			pacemaker_mode[i] = false;
		}
    	
    	// Assign coupling strengths
    	foreach(Edge e in net.Edges)
    	{
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Source, e.Target)] = 2d;
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Target, e.Source)] = 2d;
    	}						    	
    	
    	// Set up code that will be called after each simulation step
    	sync.OnStep+= new EpidemicSync.StepHandler( 
    	delegate(long time) 
		{
    		double order = sync.ComputeOrder(net.Vertices.ToArray());			
			// Record order evolution
			
			lock(orderEvolution)
			{
				if(!orderEvolution.ContainsKey((int)time))
						orderEvolution[(int) time] = new List<double>();
				orderEvolution[(int)time].Add(order);
			}
			
    		if (order>= orderThres || sync.SimulationStep>timeThres)
    			sync.Stop(); 
			
    		if(time %100 == 0)
    			Logger.AddMessage(LogEntryType.SimMsg, string.Format("Run {0}, Time {1}, Order = {2:0.00}", run, time, order));
			
    		if(Properties.Settings.Default.Use_Pacemakers)
			{
				foreach(int g in net.ClusterIDs)
				{
					double localOrder = sync.ComputeOrder(net.GetNodesInCluster(g));
					
					if(localOrder>orderThres && !pacemaker_mode[g])
					{
						pacemaker_mode[g] = true;
						Logger.AddMessage(LogEntryType.AppMsg, string.Format("Cluster {0} switched to pacemaker mode", g));
						
						// nodes with inter-cluster links are not influenced by nodes having only intra-cluster links
						foreach(Vertex v in net.GetNodesInCluster(g))
						{
							if(net.HasInterClusterConnection(v))
								foreach(Vertex w in v.Neigbors)
									if(!net.HasInterClusterConnection(w))
										sync.CouplingStrengths[new Tuple<Vertex, Vertex>(v, w)] = 0d;
						}
					}
				}
			}
    	}); 
    	
    	// Synchronously run the experiment (blocks execution until experiment is finished)
    	sync.Run();
    	
    	// Write time-series of the order parameters (global and cluster-wise) to a file
    	sync.WriteTimeSeries(string.Format("m_{0:0.00}_run{1}", mod, run));
    	
    	// Add results of this run to the result lists
	    results.Add((double) sync.SimulationStep);
	    modularity.Add(net.NewmanModularity);
	}
}

