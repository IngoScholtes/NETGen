using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using NETGen.Core;
using NETGen.Visualization;
using NETGen.Layout.FruchtermanReingold;
using NETGen.Layout.Radial;

namespace MonoParser
{
	class MainClass
	{
		
		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name='args'>
		/// The command-line arguments.
		/// </param>
		public static void Main (string[] args)
		{
			int limit = 0;
			// Print usage information
			if (args.Length<3)
			{
				Console.WriteLine("Usage: MonoParser [class_number] [scan_path] [output_file]");
				Console.WriteLine("\t path: \tPath of the directory to scan for assemblies");
				Console.WriteLine("\t output_file: \tPath of the graphML file to produce for the resulting network");
				return;
			}
			
			// Some error handling
			if (!System.IO.Directory.Exists(args[1]))
			{
				Console.WriteLine("Error: The given scan_path '{0}' is not a valid directory", args[1]);
				return;
			}
			
			limit = Int32.Parse(args[0]);
			
			try
			{
				System.IO.File.CreateText(args[2]);				
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
			foreach(string s in System.IO.Directory.GetFiles(args[1], "*.dll", System.IO.SearchOption.AllDirectories))
			{
				Assembly a = Assembly.LoadFile(s);
				assemblies.Add(a);
			}
			
			Console.WriteLine("Found {0} assemblies", assemblies.Count);
			
			// Scan for classes
			foreach(Assembly a in assemblies)
				foreach(Type t in a.GetTypes())
					if(t.FullName!="System.Object")
						if((t.IsClass || t.IsInterface || t.IsAbstract) && n.VertexCount < limit)
							n.CreateVertex(t.FullName);
			
			Console.WriteLine("Found {0} classes", n.VertexCount);
			
			// Scan for class relations
			foreach(Assembly a in assemblies)			
				foreach(Type t in a.GetTypes())
					if(t.IsClass || t.IsInterface || t.IsAbstract)
					{					
						Type baseType = t.BaseType;
						TryAddTypeRelation(n, baseType, t);
					
						foreach(Type i in t.GetInterfaces())
						TryAddTypeRelation(n, i, t);
					}
					/*
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
						*/
					//}
			
			Console.WriteLine("Found {0} types and {1} connections", n.VertexCount, n.EdgeCount);
			
			// n.ReduceToLargestConnectedComponent();
			
			int maxDegree = int.MinValue;
			Vertex maxVertex = null;
			
			foreach(Vertex v in n.Vertices)
			{
				if(v.Degree > maxDegree)
				{
					maxDegree = v.Degree;
					maxVertex = v;
				}
			}
			
			Console.WriteLine(maxVertex.Label);
			
			Console.WriteLine("{0} vertices and {1} connections", n.VertexCount, n.EdgeCount);
			
			Network.SaveToGraphML(args[2], n);				
			
			//NetworkVisualizer.Start(n, new FruchtermanReingoldLayout(10));
			
		}
		
		public static void TryAddTypeRelation(Network n, Type v, Type w)
		{
			if(v == null || w == null || v.FullName == null || w.FullName == null)
				return;
			
			Vertex v_Vert = n.SearchVertex(v.FullName);
			Vertex w_Vert = n.SearchVertex(w.FullName);
			if(v_Vert != null && w_Vert != null)
				n.CreateEdge(v_Vert, w_Vert, EdgeType.DirectedAB);
		}
	}
}
