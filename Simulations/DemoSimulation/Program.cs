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
using System.Drawing;

namespace DemoSimulation
{
    class Program
    {

        static void Main(string[] args)
        {
            // create network
            Network net = new NETGen.NetworkModels.WattsStrogatz.WSNetwork(200, 600, 0.1d);

            // setup visualization and layouting options
            NetworkVisualizer.Network = net;
            NetworkVisualizer.LayoutProvider = new NETGen.Layout.Radial.RadialLayout();
            NetworkVisualizer.PresentationSettings = new PresentationSettings(2000d, 1000d, 0d);
            NetworkVisualizer.PresentationSettings.Proportional = true;
           
            // fire up the visualization frontend
            NetDisplay.ShowDisplay(25d);

            // draw and layout the network
            NetworkVisualizer.Draw(true);



            System.Threading.Thread.Sleep(5000);

            // add some layouting options to the visualization frontend
            NetDisplay.LayoutProviders["Radial"] = new NETGen.Layout.Radial.RadialLayout();
            NetDisplay.LayoutProviders["Fruchterman-Reingold (10)"] = new FruchtermanReingoldLayout(10);
            NetDisplay.LayoutProviders["Fruchterman-Reingold (20)"] = new FruchtermanReingoldLayout(20);
            NetDisplay.LayoutProviders["Fruchterman-Reingold (50)"] = new FruchtermanReingoldLayout(50);
            NetDisplay.LayoutProviders["Fruchterman-Reingold (150)"] = new FruchtermanReingoldLayout(150);
            NetDisplay.LayoutProviders["Random"] = new RandomLayout();
			
			Console.ReadLine();
        }		
    }
}
