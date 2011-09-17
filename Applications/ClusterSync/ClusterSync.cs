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
		long timeThres = 3000;
				
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
			
            for (double weight = Properties.Settings.Default.Weight_From; 
				weight <= Properties.Settings.Default.Weight_To; 
				weight += Properties.Settings.Default.Weight_Step)
            {
				
				// Collects results of individual runs to compute mean and stddev
                List<double> results = new List<double>();
                List<double> modularity = new List<double>();
				
				Logger.AddMessage(LogEntryType.AppMsg, string.Format("Starting runs for modularity {0:0.00} and weight {1:0.00}", mod, weight));
				
				// Parallely start runs for this parameter set ... 
                System.Threading.Tasks.Parallel.For(0, runs, j =>
                {                 						
					RunExperiment (nodes, edges, clusters, j, orderThres, timeThres, mod, weight, results, modularity);
                });
				
				// Add mean and stddev to result string
                line = string.Format (new CultureInfo ("en-US").NumberFormat, 
					"{0} {1:0.000} {2:0.000} {3:0.000} \t", 
					Statistics.Mean (modularity.ToArray()), 
					weight, 
					Statistics.Mean (results.ToArray()), 
					Statistics.StandardDeviation (results.ToArray()));
				
				// Append string to file
                System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
				
                Logger.AddMessage(LogEntryType.AppMsg, string.Format("Finished runs for modularity = {0:0.00}, weight = {1:0.00}, Average time = {2:0.000000}", mod, weight, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray())));
            }
			
			// Add a blank line separating the blocks
            System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
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
	static void RunExperiment (int nodes, int edges, int clusters, int run, double orderThres, long timeThres, double mod, double weight, List<double> results, List<double> modularity)
	{
		// Setup the experiment by creating the network and the synchronization module
        ClusterNetwork net = new ClusterNetwork(nodes, edges, clusters, mod);
    	EpidemicSync sync = new EpidemicSync(net);
		
		// Assign randomly distributed distribution parameters to clusters
		Normal avgs_normal = new Normal(300d, 50d);		
					
		foreach(int i in net.ClusterIDs) 
		{
			// draw the distribution parameters from the above distribution ...
			double groupAvg = avgs_normal.Sample();
			double groupStdDev = groupAvg / 5d;
			
			// assign individual values
			foreach(Vertex v in net.GetNodesInCluster(i))
			{
				sync.PeriodMeans[v] = groupAvg;
				sync.PeriodStdDevs[v] = groupStdDev;
			}
		}
    	
    	// Assign coupling strengths
    	foreach(Edge e in net.InterClusterEdges)
    	{
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Source, e.Target)] = weight;
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Target, e.Source)] = weight;
    	}						
    	foreach(Edge e in net.IntraClusterEdges)
    	{
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Source, e.Target)] = weight;
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Target, e.Source)] = weight;
    	}
    	
    	// Set up code that will be called after each simulation step
    	sync.OnStep+= new EpidemicSync.StepHandler( 
    	delegate(long time) { 
    		double order = sync.ComputeOrder(net.Vertices.ToArray());
    		if (order>= orderThres || sync.SimulationStep>timeThres)
    			sync.Stop(); 
    		if(time %100 == 0)
    			Logger.AddMessage(LogEntryType.SimMsg, string.Format("Run {0}, Time {1}, Order = {2:0.00}", run, time, order));
    	
    	}); 
    	
    	// Synchronously run the experiment (blocks execution until experiment is finished)
    	sync.Run();
    	
    	// Write time-series of the order parameters (global and cluster-wise) to a file
    	sync.WriteTimeSeries(string.Format("m_{0:0.00}__w_{1:0.00}_run{2}", mod, weight, run));
    	
    	// Add results of this run to the result lists
	    results.Add((double) sync.SimulationStep);
	    modularity.Add(net.NewmanModularity);
	}
}

