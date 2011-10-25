using System;
using System.Drawing;

using Gtk;

using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.Dynamics.Synchronization;

namespace GraphVisualizer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init();	
			NETGenVisualizer.VisualizerControl ctrl = new NETGenVisualizer.VisualizerControl();			
			ctrl.Show();			
			Application.Run();			
		}
	}
}
