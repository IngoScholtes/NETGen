using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Drawing;

using MathNet.Numerics;

using NETGen.Core;
using NETGen.NetworkModels.Cluster;
using NETGen.Visualization;

namespace ClusterSIR
{
    public struct SIRResult
    {
        public double Modularity;
        public double InfectedRatio;
        public int Duration;
    }

    public class ClusterSIR
    {
        static void Main(string[] args)
        {
            List<double> results;
            List<double> modularity;
            string line = "";
            System.IO.File.Delete(Properties.Settings.Default.ResultFile);
               
            for (double mod = Properties.Settings.Default.Modularity_From; mod <=  Properties.Settings.Default.Modularity_To; mod += Properties.Settings.Default.Modularity_Step)
            {
                Console.WriteLine();
                line = "";
                for (double bias = Properties.Settings.Default.bias_from; bias <= Properties.Settings.Default.bias_to; bias += Properties.Settings.Default.bias_step)
                {
                    SIRResult res;
                    results = new List<double>();
                    modularity = new List<double>();
                    System.Threading.Tasks.Parallel.For(0, Properties.Settings.Default.runs, j =>
                    {
                       ClusterNetwork net = new ClusterNetwork(0, 0,0 ,0d);                            

                        Console.WriteLine("Run {0}, created cluster network with modularity={2:0.00}", j, (net as ClusterNetwork).NewmanModularity);
                        res = RunSpreading(net, bias, Properties.Settings.Default.k);
                        results.Add(res.InfectedRatio);
                        modularity.Add(res.Modularity);
                    });
                    line = string.Format(new CultureInfo("en-US").NumberFormat, "{0:0.000} {1:0.000} {2:0.000} {3:0.000} \t", MathNet.Numerics.Statistics.Statistics.Mean(modularity.ToArray()), bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()), MathNet.Numerics.Statistics.Statistics.StandardDeviation(results.ToArray()));
                    System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
                    Console.WriteLine("Finished spreading for bias = {0:0.00}, Average cover = {1:0.00}", bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()));
                }
                System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
            }                           
        }

        public static SIRResult RunSpreading(ClusterNetwork net, double bias, double k, int delay = 0)
        {
            SIRResult res = new SIRResult();

            Dictionary<Vertex, bool> infections = new Dictionary<Vertex, bool>();
            foreach (Vertex v in net.Vertices)
                infections[v] = false;

            List<Vertex> infected = new List<Vertex>();
            List<Vertex> active = new List<Vertex>();

            Vertex seed = net.RandomVertex;
            infected.Add(seed);
            active.Add(seed);

            int i = 0;

            while (active.Count > 0)
            {
                foreach (Vertex v in active.ToArray())
                {
                    // Biasing strategy
                    Vertex neighbor = v.RandomNeighbor;

                    double r = net.NextRandomDouble();

                    // First classify neighbors as intra- or inter-cluster neighbors
                    List<Vertex> intraNeighbors = new List<Vertex>();
                    List<Vertex> interNeighbors = new List<Vertex>();
                    ClassifyNeighbors(net, v, intraNeighbors, interNeighbors);

                    Console.Write("Local choice: {0} intra, {1} inter neighbors, ", intraNeighbors.Count, interNeighbors.Count);

                    // with a probability given by bias select a random inter-cluster neighbor
                    if (r <= bias && interNeighbors.Count > 0)
                        neighbor = interNeighbors.ElementAt(net.NextRandom(interNeighbors.Count));
                    
                    
                    if (net.GetClusterForNode(neighbor) == net.GetClusterForNode(v))
                        Console.WriteLine("intra-cluster neighbor chosen!");
                    else
                        Console.WriteLine("inter-cluster neighbor chosen!");                     

                    if (neighbor != null && !infections[neighbor])
                    {
                        infections[neighbor] = true;
                        infected.Add(neighbor);
                        active.Add(neighbor);
                    }
                    else if (neighbor != null)
                        if (net.NextRandomDouble() <= 1d / (double) k)
                            active.Remove(v);
                }
				if (delay > 0)
                    System.Threading.Thread.Sleep(delay);
                i++;
            }

            res.Duration = i;
            res.InfectedRatio = (double)infected.Count / (double)net.VertexCount;
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
