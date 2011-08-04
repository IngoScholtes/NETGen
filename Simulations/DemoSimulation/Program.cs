using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NETGen.Core;
using NETGen.GUI;
using NETGen.Visualization;
using System.Windows.Forms;
using NETGen.Layout.ForceDirected;
using NETGen.Layout.RandomLayout;

namespace DemoSimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            Network net = new Network();
            Vertex root = net.CreateVertex();
            BuildTree(root, 2, 7);            

            ForceDirectedLayout fdlayout = new ForceDirectedLayout(1000, 1000, 100, net);
			RandomLayout rndlayout = new RandomLayout(1000, 1000, 0);
            fdlayout.DoLayout();

            NetworkVisualizer.Network = net;
            NetworkVisualizer.LayoutProvider = rndlayout;
            NetworkVisualizer.PresentationSettings = new PresentationSettings(1000d, 1000d, 0d);
            NetworkVisualizer.PresentationSettings.VertexSize = 10;
            NetworkVisualizer.PresentationSettings.Proportional = true;

            Application.Run(new NetDisplay());
        }
		
		static void BuildTree(Vertex root, int k, int depth)
		{
			if(depth == 0)
				return;
			
			for (int i=0; i<k; i++)
			{
				Vertex w = root.Network.CreateVertex();
				root.Network.CreateEdge(root, w);
				BuildTree(w, k, depth-1);
			}			
		}
    }
}
