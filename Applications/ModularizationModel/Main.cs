using System;
using System.Collections.Generic;

using NETGen.Core;
using NETGen.pyspg;

namespace ModularizationModel
{
	public class ModularityModel : pyspgSimulation<ModularityModel>
	{
		[Parameter(ParameterType.Input, "Temperature", 0)]
		double T;
		
		[Parameter(ParameterType.Input, "Steps", 1000)]
		int Steps;
		
		[Parameter(ParameterType.Input, "Modules", 10)]
		int N;
		
		#pragma	warning disable 0414		
		[Parameter(ParameterType.Output, "Output value")]
		double result;
		#pragma	warning restore 0414
		
		NETGen.Core.Network network;
		
		Dictionary<Vertex,int> module_assignments = new Dictionary<Vertex, int>();
		
		Random r = new Random();
				
		public static void Main (string[] args) { Init(args); }		
		
		void Change()
		{		
			// chose a random class ...
			Vertex v = network.RandomVertex;			
			
			Dictionary<int, int> ModuleDependencies = new Dictionary<int, int>();		
		
			for(int i=0; i<N; i++)
				ModuleDependencies[i] = 0;
			
			// Count the number of dependencies to the same module as well as to other modules
			foreach(Vertex w in v.Neigbors)			
					ModuleDependencies[module_assignments[w]]++;
			
			Dictionary<int, double> module_probs = new Dictionary<int, double>();
			
			double c = 0d;
			
			for(int i=0; i<N; i++)
			{
				module_probs[i] = Math.Exp( (double) ModuleDependencies[i] / T);
				c+= Math.Exp( (double) ModuleDependencies[i] / T);
			}
			
			double rand = r.NextDouble()*c;
			int pos = 0;
			double acc = module_probs[0];
			
			while(acc<rand)
			{
				pos++;
				acc += module_probs[pos];			
			}

			module_assignments[v] = pos;			
		}
		
		public override void RunSimulation ()
		{
			network = Network.LoadFromEdgeFile("network.edges");
			
			foreach(Vertex v in network.Vertices)
				module_assignments[v] = r.Next(0, N);
			
			int time = 0;
			
			while (time < Steps)
			{
				Change();
				time++;
			}
			result = 42d;
		}
	}
}
