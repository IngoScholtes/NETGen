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
                results = new List<double>();
                modularity = new List<double>();
                System.Threading.Tasks.Parallel.For(0, Properties.Settings.Default.runs, j =>
                {
                   ClusterNetwork net = new ClusterNetwork(0, 0,0 ,0d);                            

                    Console.WriteLine("Run {0}, created cluster network with modularity={2:0.00}", j, (net as ClusterNetwork).NewmanModularity);
                    /// TODO: Run experiment
                });
                line = string.Format(new CultureInfo("en-US").NumberFormat, "{0:0.000} {1:0.000} {2:0.000} {3:0.000} \t", MathNet.Numerics.Statistics.Statistics.Mean(modularity.ToArray()), bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()), MathNet.Numerics.Statistics.Statistics.StandardDeviation(results.ToArray()));
                System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, line + "\n");
                Console.WriteLine("Finished spreading for bias = {0:0.00}, Average cover = {1:0.00}", bias, MathNet.Numerics.Statistics.Statistics.Mean(results.ToArray()));
            }
            System.IO.File.AppendAllText(Properties.Settings.Default.ResultFile, "\n");
        }                           
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

