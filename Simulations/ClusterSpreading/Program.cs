using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NETGen.Core;
using NETGen.NetworkModels.Cluster;
using NETGen.Visualization;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Layout.Positioned;
using NETGen.Layout.RandomLayout;
using NETGen.GUI;
using System.Drawing;

namespace ClusterSpreading
{
    class Program
    {
        static int ClusterNumber = 20;

        static void Main(string[] args)
        {
            List<Vertex> _infected = new List<Vertex>();


            ClusterNetwork net = new ClusterNetwork(ClusterNumber, 20, 80, 60);

            // Visualize and layout the network 
            NetworkVisualizer.Network = net;
            NetworkVisualizer.LayoutProvider = new FruchtermanReingoldLayout(10);
            NetworkVisualizer.PresentationSettings = new PresentationSettings(2000, 1000, 10);
            NetworkVisualizer.PresentationSettings.VertexSize = 10;

            NetDisplay.ShowDisplay(25d);            

            Vertex seed = net.RandomVertex;
            _infected.Add(seed);

            foreach (Vertex v in net.Vertices.ToArray())
                NetworkVisualizer.CustomColors[v] = _infected.Contains(v) ? Color.Red : Color.Green;
            NetworkVisualizer.Draw(true);

            while(_infected.Count<net.VertexCount)
            {
                foreach (Vertex v in _infected.ToArray())
                {
                    Vertex x = v.RandomNeighbor;
                    if (!_infected.Contains(x))
                    {
                        _infected.Add(x);
                        NetworkVisualizer.CustomColors[x] = Color.Red;
                    }
                }
                NETGen.Visualization.NetworkVisualizer.Draw();
                System.Threading.Thread.Sleep(50);
            }

            NetDisplay.LayoutProviders["Fruchterman Reingold (i=10)"] = new FruchtermanReingoldLayout(10);
            NetDisplay.LayoutProviders["Fruchterman Reingold (i=20)"] = new FruchtermanReingoldLayout(20);
            NetDisplay.LayoutProviders["Random"] = new RandomLayout();

            Console.ReadKey();
        }
    }
}
