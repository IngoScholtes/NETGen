using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Globalization;

using MathNet.Numerics;

using NETGen.Core;
using NETGen.NetworkModels.Cluster;
using NETGen.NetworkModels.ErdoesRenyi;
using NETGen.Visualization;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Layout.Positioned;
using NETGen.Layout.RandomLayout;

namespace ClusterSpreading
{
    public struct SpreadingResult
    {
        public double Modularity;
        public long Iterations;
    }

    public class ClusterSpreading
    {        
        static void Main(string[] args)
        {
            List<double> results; 
            List<double> modularity;
            string line = "";
            System.IO.File.Delete(Properties.Settings.Default.ResultFile);            

            for (double mod = Properties.Settings.Default.Modularity_From; mod <= Properties.Settings.Default.Modularity_To; mod += Properties.Settings.Default.Modularity_Step)
            {
                    Console.WriteLine();
                    line = "";
                    for(double bias = Properties.Settings.Default.bias_from; bias <= Properties.Settings.Default.bias_to; bias += Properties.Settings.Default.bias_step)
                    {
                        SpreadingResult res;
                        results = new List<double>();
                        modularity = new List<double>();
                        System.Threading.Tasks.Parallel.For(0, Properties.Settings.Default.runs, j =>
                        {
                            ClusterNetwork net = new ClusterNetwork(Properties.Settings.Default.Nodes, Properties.Settings.Default.Edges, Properties.Settings.Default.Clusters, mod);

                            Console.WriteLine("Run {0}, created cluster network with modularity={1:0.00}", j, (net as ClusterNetwork).NewmanModularity);     
                            res = RunSpreading(net, bias);
                            results.Add(res.Iterations);
                            modularity.Add(res.Modularity);
                        });
                        line = string.Format(new CultureInfo("en-US").NumberFormat, "{0:0.000} {1:0.000} {2:0.000} {3:0.000} \t", MathNet.Numerics.Statistics.Statistics.Mean(modularity.ToArray()), bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()), MathNet.Numerics.Statistics.Statistics.StandardDeviation(results.ToArray()));
                        System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
                        Console.WriteLine("Finished spreading on cluster network for modularity = {0:0.00}, bias = {1:0.00}, Average = {2:0.00}", mod, bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()));
                    }
                    System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
            }
        }

        public static SpreadingResult RunSpreading(ClusterNetwork net, double bias, int delay=0)
        {
            SpreadingResult res = new SpreadingResult();

            Dictionary<Vertex, int> infectionTime = new Dictionary<Vertex, int>();
            foreach (Vertex v in net.Vertices)
                infectionTime[v] = int.MinValue;

            List<Vertex> infected = new List<Vertex>();

            Vertex seed = net.RandomVertex;
            infected.Add(seed);

            int i = 0;

            while (infected.Count < net.VertexCount)
            {
                foreach (Vertex v in infected.ToArray())
                {
                    // Biasing strategy
                    Vertex neighbor = v.RandomNeighbor;

                    double r = net.NextRandomDouble();

                    List<Vertex> intraNeighbors = new List<Vertex>();
                    List<Vertex> interNeighbors = new List<Vertex>();
                    ClassifyNeighbors(net, v, intraNeighbors, interNeighbors);                    

                    if (r <= bias && interNeighbors.Count > 0)
                        neighbor = interNeighbors.ElementAt(net.NextRandom(interNeighbors.Count));

                    if (neighbor != null && infectionTime[neighbor] == int.MinValue)
                    {
                        infectionTime[neighbor] = i;
                        infected.Add(neighbor);
                    }
                }                
                if (delay > 0)
                    System.Threading.Thread.Sleep(delay);
                i++;
            }

            res.Iterations = i;
            res.Modularity = (net as ClusterNetwork).NewmanModularity;
            return res;
        }

        private static void ClassifyNeighbors(Network net, Vertex v, List<Vertex> intraNeighbors, List<Vertex> interNeighbors)
        {
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
