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
using NETGen.pyspg;
#endregion

public enum SimulationMode { WithoutPacemaker = 0, WithPacemakers = 1 }

public class ClusterSync : NETGen.pyspg.pyspgSimulation<ClusterSync>
{	
	[Parameter(ParameterType.Input, "Number of nodes", 1000)]
	int nodes;
	
	[Parameter(ParameterType.Input, "When to consider the network synchronized", 0.99d)]
	double orderThres;
	
	[Parameter(ParameterType.Input, "coupling strength", 2d)]
	double K;
	
	[Parameter(ParameterType.Input, "Maximum number of steps to simulate", 10000)]
	long timeThres;
	
	[Parameter(ParameterType.Input, "Number of edges", 4000)]
	int edges;
	
	[Parameter(ParameterType.Input, "Number of clusters", 20)]
	int clusters;
	
	[Parameter(ParameterType.Input, "Whether to use pacemakers", SimulationMode.WithoutPacemaker)]
	SimulationMode simMode;
	
	[Parameter(ParameterType.Input, "Desired Newman modularity of the network")]
	double modularity_tgt;
	
	[Parameter(ParameterType.Output, "Final order")]
	double order;
	
	[Parameter(ParameterType.Output, "Measured Newman modularity of the network")]
	double modularity_real;
	
	[Parameter(ParameterType.Output, "Time taken to synchronize")]
	int time;
	
	
	public static void Main(string[] args)
    {	
		Init(args);
	}
	
	
	
	/// <summary>
	/// Runs the cluster synchronization experiments, reading simulation parameters from the .config file
	/// </summary>
    public override void RunSimulation ()
	{
		// Setup the experiment by creating the network and the synchronization module
        ClusterNetwork net = new ClusterNetwork(nodes, edges, clusters, modularity_tgt);
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
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Source, e.Target)] = K;
    		sync.CouplingStrengths[new Tuple<Vertex, Vertex>(e.Target, e.Source)] = K;
    	}						    	
    	
    	// Set up code that will be called after each simulation step
    	sync.OnStep+= new EpidemicSync.StepHandler( 
    	delegate(long timestep) 
		{
    		double o = sync.ComputeOrder(net.Vertices.ToArray());			
			// Record order evolution
			/*
			lock(orderEvolution)
			{
				if(!orderEvolution.ContainsKey((int)timestep))
						orderEvolution[(int) timestep] = new List<double>();
				orderEvolution[(int)timestep].Add(order);
			}
			*/
			
    		if (order>= orderThres || sync.SimulationStep>timeThres)
    			sync.Stop(); 
			
    		if(time %100 == 0)
    			Logger.AddMessage(LogEntryType.SimMsg, string.Format("Run {0}, Time {1}, Order = {2:0.00}", timestep, o));
			
    		if(simMode == SimulationMode.WithPacemakers)
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
    	// sync.WriteTimeSeries(string.Format("m_{0:0.00}_run{1}", mod, run));
    	
    	// Add results of this run to the result lists
	    time = (int) sync.SimulationStep;
		order = sync.ComputeOrder(net.Vertices.ToArray());
	    modularity_real = net.NewmanModularity;
	}
    	
	
				
			/*
				
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
				*/                
}


