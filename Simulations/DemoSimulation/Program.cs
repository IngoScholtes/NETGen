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
		static string resultfile = null;
		
        static void Main(string[] args)
        {
			try 
			{
					// The neighbor selection bias is given as command line argument
					resultfile = args[0];
			}
			catch(Exception) 
			{
				Console.WriteLine("Usage: mono ./DemoSimulation.exe [resultfile]");
				return;
			}
									
			// Create a network of given size and modularity ... 
            network = new ClusterNetwork(1000, 5000, 20, -0.9, true);
			
			Console.WriteLine("Created network with {0} vertices, {1} edges and modularity {2:0.00}", network.VertexCount, network.EdgeCount, network.NewmanModularity);
                        
			// Run the real-time visualization
			NetworkColorizer colorizer = new NetworkColorizer();			
			NetworkVisualizer.Start(network, new FruchtermanReingoldLayout(15), colorizer);									
			
			// Setup the synchronization simulation
			sync = new EpidemicSynchronization(network, colorizer);
			
			// Assign randomly distributed distribution parameters to clusters
			Normal avgs_normal = new Normal(300d, 50d);
			Normal devs_normal = new Normal(20d, 5d);
						
			foreach(int i in network.ClusterIDs) 
			{
				// draw the distribution parameters from the above distribution ...
				double groupAvg = avgs_normal.Sample();
				double groupStdDev = devs_normal.Sample();
				
				// assign individual values
				foreach(Vertex v in network.GetNodesInCluster(i))
				{
					sync._MuPeriods[v] = groupAvg;
					sync._SigmaPeriods[v] = groupStdDev;
				}
			}
			
			sync.OnStep+=new EpidemicSynchronization.StepHandler(collectLocalOrder);						
			sync.OnStop+=new EpidemicSynchronization.StopHandler( delegate() { sync.WriteTimeSeries(resultfile); });
			
			Console.Write("Press enter to start synchronization experiment...");
			Console.ReadLine();		
			
			// Run the simulation asynchronously so we can stop it anytime
			sync.RunInBackground();																	
						
			// Simulation can be stopped by pressing enter
			Console.ReadLine();							
			sync.Stop();
			
			// Write the time series to the resultfile
			sync.WriteTimeSeries(resultfile);
        }		
		
		private static void collectLocalOrder(long time)
		{					
			// Compute and record global order parameter
			double globalOrder = sync.ComputeOrder(network.Vertices.ToArray());			
			sync.AddDataPoint("order_global", globalOrder); 			
			if(time %100 == 0)
				Logger.AddMessage(LogEntryType.SimMsg, string.Format("Time = {000000}, Global Order = {1:0.00}", time, globalOrder));
			
			// Compute and record cluster order parameters
			foreach(int g in network.ClusterIDs)
			{
				double localOrder = sync.ComputeOrder(network.GetNodesInCluster(g));
				sync.AddDataPoint(string.Format("order_{0}", g), localOrder);					
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