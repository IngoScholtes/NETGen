using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Globalization;

using MathNet.Numerics;

using NETGen.Core;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Visualization;
using NETGen.NetworkModels.Cluster;


namespace ClusterConsensus
{
    struct AggregationResult
    {
        public double FinalVariance;
        public double FinalOffset;
        public double Modularity;
    }

    class Program
    {               
        static void Main(string[] args)
        {
            List<double> results;
            List<double> modularity;
            string line = "";
            System.IO.File.Delete(Properties.Settings.Default.ResultFile);

            int Nc = Properties.Settings.Default.clusterSize;
            int c = Properties.Settings.Default.clusters;
            double m = Properties.Settings.Default.k * Nc * c;
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
                for (double bias = Properties.Settings.Default.bias_from; bias <= Properties.Settings.Default.bias_to; bias += Properties.Settings.Default.bias_step)
                {
                    AggregationResult res;
                    results = new List<double>();
                    modularity = new List<double>();
                    System.Threading.Tasks.Parallel.For(0, Properties.Settings.Default.runs, j =>
                    {
                        // compute the parameter p_e that will give the desired edge number
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
                        res = RunAggregation(net, bias);
                        results.Add(res.FinalVariance);
                        modularity.Add(res.Modularity);
                    });
                    line = string.Format(new CultureInfo("en-US").NumberFormat, "{0} {1:0.000} {2:0.000} {3:0.000} \t", MathNet.Numerics.Statistics.Statistics.Mean(modularity.ToArray()), bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()), MathNet.Numerics.Statistics.Statistics.StandardDeviation(results.ToArray()));
                    System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
                    Console.WriteLine("Finished runs for p_in = {0:0.00}, bias = {1:0.00}, Average var = {2:0.000000}", p_in, bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()));
                }
                System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
            }
            Console.ReadKey();
        }

        private static AggregationResult RunAggregation(ClusterNetwork net, double bias)
        {

            Dictionary<Vertex, double> _attributes = new Dictionary<Vertex, double>();
            Dictionary<Vertex, double> _aggregates = new Dictionary<Vertex, double>();

            MathNet.Numerics.Distributions.Normal normal = new MathNet.Numerics.Distributions.Normal(0d, 5d);

            AggregationResult result = new AggregationResult();

            result.Modularity = net.NewmanModularity;          

            double average = 0d;

            foreach (Vertex v in net.Vertices)
            {
                _attributes[v] = normal.Sample();
                _aggregates[v] = _attributes[v];
                average += _attributes[v];
            }
            average /= (double)net.VertexCount;

            double avgEstimate = double.MaxValue;

            result.FinalVariance = double.MaxValue;
            result.FinalOffset = 0d;

            for (int k = 0; k < Properties.Settings.Default.ConsensusRounds; k++)
            {
                foreach (Vertex v in net.Vertices.ToArray())
                {
                    Vertex w = v.RandomNeighbor;
                    List<Vertex> intraNeighbors = new List<Vertex>();
                    List<Vertex> interNeighbors = new List<Vertex>();
                    ClassifyNeighbors(net, v, intraNeighbors, interNeighbors);

                    double r = net.NextRandomDouble();
                    if (r <= bias && interNeighbors.Count > 0)
                        w = interNeighbors.ElementAt(net.NextRandom(interNeighbors.Count));

                    _aggregates[v] = aggregate(_aggregates[v], _aggregates[w]);
                    _aggregates[w] = aggregate(_aggregates[v], _aggregates[w]);
                }

                avgEstimate = 0d;
                foreach (Vertex v in net.Vertices.ToArray())
                    avgEstimate += _aggregates[v];
                avgEstimate /= (double)net.VertexCount;

                result.FinalVariance = 0d;
                foreach (Vertex v in net.Vertices.ToArray())
                    result.FinalVariance += Math.Pow(_aggregates[v] - avgEstimate, 2d);
                result.FinalVariance /= (double)net.VertexCount;

                double intraVar = 0d;
                foreach (int c in net.ClusterIDs)
                {
                    double localavg = 0d;
                    double localvar = 0d;

                    foreach (Vertex v in net.GetNodesInCluster(c))
                        localavg += _aggregates[v];
                    localavg /= net.GetClusterSize(c);

                    foreach (Vertex v in net.GetNodesInCluster(c))
                        localvar += Math.Pow(_aggregates[v] - localavg, 2d);
                    localvar /= net.GetClusterSize(c);

                    intraVar += localvar;
                }
                intraVar /= 50d;

                //Console.WriteLine("i = {0:0000}, Avg = {1:0.000}, Estimate = {2:0.000}, Intra-Var = {3:0.000}, Total Var = {4:0.000}", result.iterations, average, avgEstimate, intraVar, totalVar);
            }
            result.FinalOffset = average - avgEstimate;

            return result;
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

        static Color ColorFromValue(double value, double average)
        {
            int r = (int)(Math.Tanh(value/20d + average/20d) * 255);
            int g = (int)(Math.Tanh(value/20d + average/20d) * 255);
            int b = (int)(Math.Tanh(value/20d + average/20d) * 255);
            return Color.FromArgb(r, g, b);
        }       

        static double aggregate(double a, double b)
        {
            return (a + b) / 2d;
        }
    }
}
