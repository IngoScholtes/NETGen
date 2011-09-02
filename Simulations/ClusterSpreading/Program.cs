using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NETGen.Core;
using NETGen.NetworkModels.Cluster;
using NETGen.NetworkModels.ErdoesRenyi;
using NETGen.Visualization;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Layout.Positioned;
using NETGen.Layout.RandomLayout;
using NETGen.GUI;
using System.Drawing;
using System.Globalization;
using MathNet.Numerics;

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

            int Nc = Properties.Settings.Default.clusterSize;
            int c = Properties.Settings.Default.clusters;
            double m = Properties.Settings.Default.m * Nc * c;
            // what is the maximum p_i possible for the given parameters?

            // in order to yield a connected network, at least ... 
            double inter_thresh = 1.2d * ((c * Math.Log(c)) / 2d);       // ... inter edges are required

            double intra_edges = m - inter_thresh;      // the maximum number of expected intra edges

            // this yields a maximum value for p_i of ... 
            double max_pi = intra_edges / (c * Combinatorics.Combinations(Nc, 2));

            for (double p_in = Properties.Settings.Default.p_in_from; p_in <= max_pi; p_in += Properties.Settings.Default.p_in_step)
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
                            double p_e = (m - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2) * p_in) / (Combinatorics.Combinations(c * Nc, 2) - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2));
                            if (p_e < 0)
                                throw new Exception("probability out of range");

                            ClusterNetwork net = null;
                            do
                            {
                                if (net != null)
                                    Console.WriteLine("Network (inter = {0}, intra = {1} not connected ... ", net.InterClusterEdges, net.IntraClusterEdges);
                                net = new ClusterNetwork(Properties.Settings.Default.clusters, Properties.Settings.Default.clusterSize, p_in, p_e);
                            }
                            while (!net.Connected);

                            Console.WriteLine("Run {0}, created cluster network for p_in={1:0.00} with modularity={2:0.00}", j, p_in, (net as ClusterNetwork).NewmanModularity);     
                            res = RunSpreading(net, bias);
                            results.Add(res.Iterations);
                            modularity.Add(res.Modularity);
                        });
                        line = string.Format(new CultureInfo("en-US").NumberFormat, "{0:0.000} {1:0.000} {2:0.000} {3:0.000} \t", ResultSet.ComputeMean(modularity.ToArray()), bias, ResultSet.ComputeMean(results.ToArray()), ResultSet.ComputeStandardVariation(results.ToArray()));
                        System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
                        Console.WriteLine("Finished spreading on cluster network for p_in = {0:0.00}, bias = {1:0.00}, Average = {2:0.00}", p_in, bias, ResultSet.ComputeMean(results.ToArray()));
                    }
                    System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
            }
        }

        public static SpreadingResult RunSpreading(ClusterNetwork net, double bias, NetworkVisualizer viz = null, int delay=0)
        {
            SpreadingResult res = new SpreadingResult();

            if (viz != null)
                foreach (Vertex v in net.Vertices)
                    viz.CustomColors[v] = Color.Green;

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
                if (viz != null)
                {
                    viz.CustomColors.Recompute(v =>
                    {
                        if (infectionTime[v]!=int.MinValue)
                            return Color.Red;
                        else
                            return Color.Green;
                    });
                    viz.Draw();
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
