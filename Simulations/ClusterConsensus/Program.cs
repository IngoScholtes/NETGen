using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;
using NETGen.Layout.FruchtermanReingold;
using NETGen.GUI;
using NETGen.Visualization;
using System.Windows.Forms;
using System.Drawing;
using NETGen.NetworkModels.Cluster;

namespace ClusterConsensus
{
    class Program
    {       
        static int Iterations = 10000;
        static int ClusterNumber = 20;

        static void Main(string[] args)
        {
            Dictionary<Vertex, double> _attributes = new Dictionary<Vertex, double>();
            Dictionary<Vertex, double> _aggregates = new Dictionary<Vertex, double>();
            

            ClusterNetwork net = new ClusterNetwork(ClusterNumber, 20, 80, 40);

            // Visualize and layout the network 
            NetworkVisualizer.Network = net;
            NetworkVisualizer.LayoutProvider = new FruchtermanReingoldLayout(10);
            NetworkVisualizer.PresentationSettings = new PresentationSettings(2000, 1000, 10);

            NetDisplay.ShowDisplay(25d);

            NetworkVisualizer.Draw(true);

            double average = 0d;

            foreach (Vertex v in net.Vertices)
            {
                _attributes[v] = net.NextRandomDouble() * 20;
                average += _attributes[v];
                _aggregates[v] = _attributes[v];
                NetworkVisualizer.CustomColors[v] = ColorFromValue(_aggregates[v], average);
            }
            average /= (double) net.VertexCount;

            for (int i = 0; i < Iterations; i++)
            {
                
                foreach (Vertex v in net.Vertices.ToArray())
                {
                    Vertex w = v.RandomNeighbor;
                    _aggregates[v] = aggregate(_aggregates[v], _aggregates[w]);
                    _aggregates[w] = aggregate(_aggregates[v], _aggregates[w]);
                }

                double avgEstimate = 0d;
                foreach(Vertex v in net.Vertices.ToArray())
                    avgEstimate += _aggregates[v];
                avgEstimate /= net.VertexCount;

                double totalVar = 0d;
                foreach (Vertex v in net.Vertices.ToArray())
                    totalVar += Math.Pow(_aggregates[v]-avgEstimate, 2d);
                totalVar /= net.VertexCount;

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
                intraVar /= ClusterNumber;
                
                foreach (Vertex v in net.Vertices)
                    NetworkVisualizer.CustomColors[v] = ColorFromValue(_aggregates[v], average);
                NetworkVisualizer.Draw(false);
                Console.WriteLine("i = {0:0000}, Avg = {1:0.000}, Estimate = {2:0.000}, Intra-Var = {3:0.000}, Total Var = {4:0.000}", i, average, avgEstimate, intraVar, totalVar);
                System.Threading.Thread.Sleep(50);
            }


            Console.ReadKey();
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
