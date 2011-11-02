using System;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.Linq;

using Gtk;

using NETGen.Core;
using NETGen.NetworkModels.Tree;
using NETGen.Visualization;
using NETGen.Layouts.FruchtermanReingold;


using MathNet.Numerics.Distributions;

	
class MainClass
{
	enum NodeType { Aggregate = 0, Individual = 1, Root = 2 }
	
	static Random r = new Random();
	static Network hierarchy;
	static Dictionary<Vertex, double> Variance = new Dictionary<Vertex, double>();
	static double trueValue = 42d;
	static int individuals = 200;
	static ConsensusHierarchy.TemperatureWindow slider;
	static Vertex root = null;
	static int iterations = 50;
	static NetworkColorizer c = new NetworkColorizer();
	
	static List<Vertex> Individuals = new List<Vertex>();
	static List<Vertex> Aggregates = new List<Vertex>();
	
	static void Change()
	{
		double stddev_old = ComputeStdDev();
		
		// chose a random individual ...
		Vertex v = Individuals[r.Next(Individuals.Count)];
		
		if(v.InDegree == 0)
		{			
			Logger.AddMessage(LogEntryType.Warning, "Encountered disconnected individual");
			return;
		}
		
		// ... and assign it to a different group		
		Vertex oldGroup = v.Predecessors.ToArray()[0];		
		Edge removedEdge = v.GetEdgeFromPredecessor(oldGroup);
		hierarchy.RemoveEdge(removedEdge);
		Vertex newGroup = Aggregates[r.Next(Aggregates.Count)];
		Edge addedEdge = hierarchy.CreateEdge(newGroup, v, EdgeType.DirectedAB);
		
		double stddev_new = ComputeStdDev();
		
		// decide whether to accept the change
		double temp = slider.Temperature;
		double accept_prob = stddev_new < stddev_old?1d : Math.Exp( - (stddev_new - stddev_old) / temp );		
		if(r.NextDouble() > accept_prob)
		{
			// Revoke change			
			hierarchy.AddEdge(removedEdge);
			hierarchy.RemoveEdge(addedEdge);
		}
		else
		{
			// change is accepted, relayout
			NetworkVisualizer.Layout.TouchEdge(addedEdge);
			NetworkVisualizer.Layout.TouchEdge(removedEdge);
			Logger.AddMessage(LogEntryType.AppMsg, string.Format("Performed change, new avg = {0}", stddev_new));
		}
	}
	
	static double ComputeStdDev()
	{
		double k = 0d;
		double outer_sum = 0d;
		
		foreach(Vertex p in Aggregates)
		{
			double inner_sum = 0d;
			if (p.OutDegree>0)
			{
				k++;
				double Np = 0d;				
				foreach(Vertex j in p.Neigbors)
				{
					Np++;
					inner_sum += Math.Pow(Variance[j], 2d);
				}
				inner_sum *= Math.Pow(Np, -2d);
				outer_sum += inner_sum;
			}			
		}		
		return outer_sum * Math.Pow(k, -2d);
	}	
	
	public static void Main (string[] args)
	{						
		hierarchy = new Network();
		root = hierarchy.CreateVertex();
		root.Tag = NodeType.Root;
		c[root] = Color.Orange;
		
		
		// Assign node types and estimates
		for (int i = 0; i<individuals; i++)
		{
			Vertex aggregate = hierarchy.CreateVertex();
			hierarchy.CreateEdge(root, aggregate, EdgeType.DirectedAB);
			c[aggregate] = Color.Black;
			Aggregates.Add(aggregate);
			aggregate.Tag = NodeType.Aggregate;
			
			Vertex individual = hierarchy.CreateVertex();
			individual.Tag = NodeType.Individual;
			
			Variance[individual] = r.NextDouble();
			c[individual] = Color.FromArgb(255 - (int) (255d  * Variance[individual]), (int) (255d * Variance[individual]), 0);
			
			hierarchy.CreateEdge(aggregate, individual, EdgeType.DirectedAB);
			Individuals.Add(individual);
			
		}		
		
		Application.Init();
		slider = new ConsensusHierarchy.TemperatureWindow();
		slider.Show();	
		System.Threading.ThreadPool.QueueUserWorkItem( delegate(object o) {
			FruchtermanReingoldLayout layout = new FruchtermanReingoldLayout(15);
			NETGen.Visualization.NetworkVisualizer.Start(hierarchy, layout, c, 800, 600);		
			layout.DoLayoutAsync();
			
			Logger.ShowInfos = false;
			
			while(true)	
			{
				Change();		
				c.RecomputeColors(new Func<Vertex,Color>(v => {
					if (((NodeType)v.Tag) == NodeType.Aggregate)
					{						
						if(v.OutDegree==0)
							return Color.White;
						else
							return Color.Black;
					}
					else
						return c[v];
				}));
				
				c.RecomputeColors(new Func<Edge,Color>(e => {
					if (((NodeType)e.Target.Tag) == NodeType.Aggregate && e.Target.OutDegree==0)
							return Color.White;
					else
						return c.DefaultEdgeColor;
				}));
				NetworkVisualizer.Layout.DoLayout();
			}
		});
		Application.Run();
	}
}
