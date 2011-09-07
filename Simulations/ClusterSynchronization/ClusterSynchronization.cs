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

            int Nc = Properties.Settings.Default.clusterSize;
            int c = Properties.Settings.Default.clusters;
            double m = Properties.Settings.Default.m * Nc * c;
            // what is the maximum p_i possible for the given parameters?

            // in order to yield a connected network, at least ... 
            double inter_thresh = 1.2d * ((c * Math.Log(c)) / 2d);       // ... inter edges are required

            double intra_edges = m - inter_thresh;      // the maximum number of expected intra edges

            // this yields a maximum value for p_i of ... 
            double max_pi = intra_edges / (c * Combinatorics.Combinations(Nc, 2));
			
			// Explore parameter space for parameters p_in and bias
            for (double p_in = Properties.Settings.Default.p_in_from; p_in <= max_pi; p_in += Properties.Settings.Default.p_in_step)
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
                        // compute the parameter p_e that will give the desired edge number
                        double p_e = (m - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2) * p_in) / (Combinatorics.Combinations(c * Nc, 2) - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2));
                        if (p_e < 0)
                            throw new Exception("probability out of range");
						
						// Create the network
                        ClusterNetwork net = null;
                        do
                        {
                            if (net != null)
                                Console.WriteLine("Network (inter = {0}, intra = {1} not connected ... ", net.InterClusterEdges, net.IntraClusterEdges);
                            net = new ClusterNetwork(Properties.Settings.Default.clusters, Properties.Settings.Default.clusterSize, p_in, p_e);
                        }
                        while (!net.Connected);						
						
                        Console.WriteLine("Run {0}, created cluster network for p_in={1:0.00} with modularity={2:0.00}", j, p_in, (net as ClusterNetwork).NewmanModularity);
						
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
                    Console.WriteLine("Finished runs for p_in = {0:0.00}, bias = {1:0.00}, Average time = {2:0.000000}", p_in, bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()));
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
