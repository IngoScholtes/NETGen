using System;
using System.Reflection;
using System.Collections.Generic;
using NETGen.Core;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Layout.Radial;
using NETGen.GUI;
using NETGen.Visualization;

namespace MonoParser
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// Print usage information
			if (args.Length<2)
			{
				Console.WriteLine("Usage: MonoParser [scan_path] [output_file]");
				Console.WriteLine("\t path: \tPath of the directory to scan for assemblies");
				Console.WriteLine("\t output_file: \tPath of the graphML file to produce for the resulting network");
				return;
			}
			
			// Some error handling
			if (!System.IO.Directory.Exists(args[0]))
			{
				Console.WriteLine("Error: The given scan_path '{0}' is not a valid directory", args[0]);
				return;
			}
			
			try
			{
				System.IO.File.CreateText(args[1]);				
			}
			catch(System.IO.IOException)
			{
				Console.WriteLine("Error: Cannot write to the specified output_file");
				return;
			}
			
			// The network
			Network n = new Network();
			
			// Scan the assemblies
			List<Assembly> assemblies = new List<Assembly>();
			foreach(string s in System.IO.Directory.GetFiles(args[0], "*.dll"))
			{
				Assembly a = Assembly.LoadFile(s);
				assemblies.Add(a);
			}
			
			Console.WriteLine("Found {0} assemblies", assemblies.Count);
			
			// Scan for classes
			foreach(Assembly a in assemblies)			
				foreach(Type t in a.GetTypes())
					if(t.IsClass && n.VertexCount<2000)
						n.CreateVertex(t.FullName);
			
			Console.WriteLine("Found {0} classes", n.VertexCount);
			
			// Scan for class relations
			foreach(Assembly a in assemblies)			
				foreach(Type t in a.GetTypes())
					if(t.IsClass)
					{
						foreach(PropertyInfo p in t.GetProperties())
							TryAddTypeRelation(n, t, p.PropertyType);
					
						foreach(FieldInfo f in t.GetFields())
							TryAddTypeRelation(n, t, f.FieldType);
							
						foreach(MethodInfo m in t.GetMethods())
						{
							MethodBody mb = m.GetMethodBody();
							if(mb != null)
								foreach(LocalVariableInfo i in m.GetMethodBody().LocalVariables)
									TryAddTypeRelation(n, t, i.LocalType);
						}
					}
			
			Console.WriteLine("Found {0} types and {1} connections", n.VertexCount, n.EdgeCount);
			
			n.ReduceToLargestConnectedComponent();
			
			Network.SaveToGraphML(args[1], n);
			
			NetworkVisualizer vis = new NetworkVisualizer(n, new RadialLayout(), new PresentationSettings(2000d, 1000d, 0d));
			vis.PresentationSettings.Proportional = true;
			NetworkDisplay disp = NetworkDisplay.CreateDisplay(vis);
			
			disp.LayoutOptions["FR (10)"] = new FruchtermanReingoldLayout(10);
			disp.LayoutOptions["FR (20)"] = new FruchtermanReingoldLayout(20);
			disp.LayoutOptions["Radial"] = new RadialLayout();
		}
		
		public static void TryAddTypeRelation(Network n, Type v, Type w)
		{
			if(v == null || w == null || v.FullName == null || w.FullName == null || !v.IsClass || !w.IsClass)
				return;
			
			Vertex v_Vert = n.SearchVertex(v.FullName);
			Vertex w_Vert = n.SearchVertex(w.FullName);
			if(v_Vert != null && w_Vert != null)
				n.CreateEdge(v_Vert, w_Vert);
		}
	}
}
