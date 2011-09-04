using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;
using NETGen.GUI;
using NETGen.Visualization;
using System.Windows.Forms;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Layout.RandomLayout;
using NETGen.Layout.Positioned;
using NETGen.Layout.Radial;
using System.Drawing;
using NETGen.NetworkModels.Cluster;
using MathNet.Numerics;

namespace DemoSimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            // load network
            Network network;
            do
            {
                int c = 20;
                int Nc = 20;

                int m = 20 * c * Nc;

                // in order to yield a connected network, at least ... 
                double inter_thresh = 1.2d * ((c * Math.Log(c)) / 2d);       // ... inter edges are required

                double intra_edges = m - inter_thresh;      // the maximum number of expected intra edges

                // this yields a maximum value for p_i of ... 
                double pi =  intra_edges / (c * Combinatorics.Combinations(Nc, 2)) - 0.05d;

                double p_e = (m - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2) * pi) / (Combinatorics.Combinations(c * Nc, 2) - c * MathNet.Numerics.Combinatorics.Combinations(Nc, 2));
                Console.WriteLine("Generating cluster network with p_i = {0:0.0000}, p_e = {1:0.0000}", pi, p_e);                
                
                network = new NETGen.NetworkModels.Cluster.ClusterNetwork(c, Nc, pi, p_e);
                network.ReduceToLargestConnectedComponent();
            } while (!network.Connected);            
            
            // create some layouting options that should be added to the visualization frontend
            LayoutOptions options = new LayoutOptions();
            options["Radial"] = new NETGen.Layout.Radial.RadialLayout();
            options["Fruchterman-Reingold (10)"] = new FruchtermanReingoldLayout(10);
            options["Fruchterman-Reingold (20)"] = new FruchtermanReingoldLayout(20);
            options["Fruchterman-Reingold (50)"] = new FruchtermanReingoldLayout(50);
            options["Random"] = new RandomLayout();
			options["Radial"] = new RadialLayout();

            NetworkVisualizer visualizer = new NetworkVisualizer(network, new RandomLayout(), new PresentationSettings(2000d, 1000d, 0d));

            // fire up the visualization frontend
            NetworkDisplay display = NetworkDisplay.CreateDisplay(visualizer, 25, options);
            // Console.WriteLine("Modularity = {0:0.00}, Nodes = {1}, Edges = {2}", network.NewmanModularity, network.VertexCount, network.EdgeCount);

            string text = "";
            while (text!="exit")
            {
                Console.Write("Enter bias value to start spreading or enter 'exit': ");
            
                text = Console.ReadLine();

                if(text != "exit")                
                {
                    double bias = double.Parse(text);
                    EpidemicSynchronization.SyncResults res = EpidemicSynchronization.ClusterSynchronization.RunSynchronization(network, bias, display);
                    Console.WriteLine("Order {0:0.00} reached after {1} rounds", res.order, res.time);
                }
            }            
        }
    }
}