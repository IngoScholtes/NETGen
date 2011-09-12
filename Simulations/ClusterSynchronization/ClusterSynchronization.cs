using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;

using MathNet.Numerics;

using NETGen.Core;
using NETGen.NetworkModels.Cluster;
using NETGen.Visualization;

namespace EpidemicSynchronization
{
    public struct SyncResults
    {
        public double order;
        public int time;
        public double Modularity;
    }

    public class ClusterSynchronization
    {        
        public static void Main()
        {
            List<double> results;
            List<double> modularity;
            string line = "";
            System.IO.File.Delete(Properties.Settings.Default.ResultFile);            
			
			// Explore parameter space for parameters p_in and bias
            for (double mod = Properties.Settings.Default.Modularity_From; mod <= Properties.Settings.Default.Modularity_To; mod += Properties.Settings.Default.Modularity_Step)
            {
                Console.WriteLine();
                line = "";
                for (double bias = Properties.Settings.Default.bias_from; bias <= Properties.Settings.Default.bias_to; bias += Properties.Settings.Default.bias_step)
                {
                    results = new List<double>();
                    modularity = new List<double>();
					
					// Parallely start runs for this parameter set ... 
                    System.Threading.Tasks.Parallel.For(0, Properties.Settings.Default.runs, j =>
                    {                       
                        ClusterNetwork net = new ClusterNetwork(Properties.Settings.Default.Nodes, Properties.Settings.Default.Edges, Properties.Settings.Default.Clusters, mod);
                        
                        Console.WriteLine("Run {0}, created cluster network with modularity={1:0.00}", j, (net as ClusterNetwork).NewmanModularity);
						
						NETGen.Dynamics.EpidemicSynchronization.EpidemicSynchronization sync = new NETGen.Dynamics.EpidemicSynchronization.EpidemicSynchronization(
							net,
							null,
							v => {
								Vertex neighbor = v.RandomNeighbor;
								double r = net.NextRandomDouble();

				                // classify neighbors
				                List<Vertex> intraNeighbors = new List<Vertex>();
				                List<Vertex> interNeighbors = new List<Vertex>();
				                ClassifyNeighbors(net, v, intraNeighbors, interNeighbors);
				
				                // biasing strategy ... 
				                if (r <= bias && interNeighbors.Count > 0)
				                    neighbor = interNeighbors.ElementAt(net.NextRandom(interNeighbors.Count));
								return neighbor;
							}
						);
						
						sync.Run();
						
                        NETGen.Dynamics.EpidemicSynchronization.SyncResults res = sync.Collect();
                        results.Add(res.time);
                        modularity.Add(net.NewmanModularity);
                    });
                    line = string.Format(new CultureInfo("en-US").NumberFormat, "{0} {1:0.000} {2:0.000} {3:0.000} \t", MathNet.Numerics.Statistics.Statistics.Mean(modularity.ToArray()), bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()), MathNet.Numerics.Statistics.Statistics.StandardDeviation(results.ToArray()));
                    System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
                    Console.WriteLine("Finished runs for modularity = {0:0.00}, bias = {1:0.00}, Average time = {2:0.000000}", mod, bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()));
                }
                System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
            }
            Console.ReadKey();  
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
