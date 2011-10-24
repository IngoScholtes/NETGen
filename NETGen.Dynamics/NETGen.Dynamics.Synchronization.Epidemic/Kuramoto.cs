#region .NET/MONO System libraries
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
#endregion

#region Much appreciated third-party libraries
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
#endregion

#region NETGen libraries
using NETGen.Core;
using NETGen.Visualization;
#endregion

namespace NETGen.Dynamics.Synchronization
{
	
	public class Kuramoto : ContinuousDynamics
	{
		/// <summary>
		/// The network for which the synchronization shall be run
		/// </summary>
		Network _network;
		
		/// <summary>
		/// Used to color vertices and edges if not null
		/// </summary>
		NetworkColorizer _colorizer = null; 	
		
		/// <summary>
		/// The average degree of the network
		/// </summary>
		double _avgDeg;	
		
		/// <summary>
		/// The natural frequencies of oscillators
		/// </summary>
		public ConcurrentDictionary<Vertex, double> NaturalFrequencies;
		
		/// <summary>
		/// Individual (directed) coupling strengths for each bidirectional edge in the network
		/// </summary>
		public Dictionary<Tuple<Vertex, Vertex>, double> CouplingStrengths;	
		
		/// <summary>
		/// Whether or not to weight the coupling strength by the degree of vertices
		/// </summary>
        public bool DegreeWeight = true;
		
		/// <summary>
		/// Whether or not to weight the coupling strength by the degree of the coupling target
		/// </summary>
        public bool DoubleDegreeWeight = false;
		
		/// <summary>
		/// The probability of a node coupling in each time step
		/// </summary>
        public double CouplingProbability = 1d;
		
		/// <summary>
		/// Whether or not to compensate smaller coupling probability by a proportionate increase in coupling strength
		/// </summary>
		public bool Compensate_cp = false;
		
		/// <summary>
		/// This function can be used to assign custom neighbor selection strategies via a lambda expression. Unbiased random neighbor selection will be used by default. 
		/// </summary>
		public Func<Vertex, Vertex[]> CouplingSelector = null;
		
		Dictionary<Vertex, int> _mapping;
		
		/// <summary>
		/// Initializes a new synchronization experiment
		/// </summary>
		/// <param name='n'>
		/// The network to run the experiment for
		/// </param>
		/// <param name='colorizer'>
		/// Will be used to color vertices and edges as the experiment runs. Can be null or omitted altogether if no visualization is used. 
		/// </param>
		/// <param name='selectNeighbor'>
		/// A lambda expression that will be invoked whenever a neighbor is chosen to couple to. If null or not given, an unbiased random neighbor selection will be used.
		/// </param>
		public Kuramoto(Network n, double K, NetworkColorizer colorizer = null, Func<Vertex, Vertex[]> couplingSelector = null) : base(0d, new DenseVector((int) n.VertexCount))
		{
			_network = n;
			_colorizer = colorizer;

			CouplingStrengths = new Dictionary<Tuple<Vertex, Vertex>, double>();
			NaturalFrequencies = new ConcurrentDictionary<Vertex, double>();
			
            foreach (Edge e in _network.Edges)
			{
				Tuple<Vertex,Vertex> t1 = new Tuple<Vertex, Vertex>(e.Source, e.Target);
				Tuple<Vertex,Vertex> t2 = new Tuple<Vertex, Vertex>(e.Target, e.Source);
				
				if(e.EdgeType == EdgeType.Undirected || e.EdgeType == EdgeType.DirectedAB)
					CouplingStrengths[t1] = K;
				
				if(e.EdgeType == EdgeType.Undirected || e.EdgeType == EdgeType.DirectedBA)
					CouplingStrengths[t2] = K;
			}
			
			_mapping = new Dictionary<Vertex, int>();
			
			int i= 0;
			foreach(Vertex v in _network.Vertices)
			{
				NaturalFrequencies[v] = 0.1d;
				_mapping[v] = i++;
			}
			
			// if no neighbor selector is given, just couple to all nearest neighbors
			if(couplingSelector==null)
				CouplingSelector = new Func<Vertex, Vertex[]>( v => {
					return v.Neigbors.ToArray();
				});
			else
				CouplingSelector = couplingSelector;				
			
			// Initialize phases, colors and average degree
            foreach (Vertex v in _network.Vertices) 
			{				
				CurrentValues[_mapping[v]] = _network.NextRandomDouble() * Math.PI * 2d;
				if(_colorizer != null)
					_colorizer[v] = ColorFromPhase(CurrentValues[_mapping[v]]);
				_avgDeg += v.Degree;
            }
			_avgDeg /= (double) _network.VertexCount;
			
			Logger.AddMessage(LogEntryType.Info, string.Format("Sychchronization module initialized. Initial global order = {0:0.000}", GetOrder(_network.Vertices.ToArray())));
			
			TimeDelta = Math.PI / 100d;
			
			OnStep+= new StepHandler(recolor);
		}
		
		/// <summary>
		/// Recolors the vertices
		/// </summary>
		/// <param name='time'>
		/// Time.
		/// </param>
		void recolor(double time)
		{
			if(_colorizer != null)
			{
				_colorizer.RecomputeColors(new Func<Edge, Color>(e => {  return _colorizer.DefaultEdgeColor; } ));
				_colorizer.RecomputeColors(new Func<Vertex, Color>(v => {  return ColorFromPhase(CurrentValues[_mapping[v]]); } ));
			}
		}

		/// <summary>
		/// Describes the system dynamics by giving the instantaneous change at a given time and for a given current state
		/// </summary>
		/// <returns>
		/// The delta.
		/// </returns>
		/// <param name='time'>
		/// Time.
		/// </param>
		/// <param name='currentValues'>
		/// Current values.
		/// </param>
		protected override DenseVector ComputeDelta(double time, DenseVector currentState)
		{					
			DenseVector delta = new DenseVector(currentState.Count);
			
			foreach(Vertex v in _network.Vertices)
			{
				delta[_mapping[v]] = NaturalFrequencies[v];
				if( _network.NextRandomDouble() < CouplingProbability)
				{
					foreach(Vertex w in CouplingSelector(v))
					{
	                	double couplingStrength = GetCouplingStrength(v, w);
						if(_colorizer!=null && couplingStrength > 0d)
							_colorizer[v.GetEdgeToSuccessor(w)] = Color.Red;
						delta[_mapping[v]] += couplingStrength * Math.Sin(currentState[_mapping[w]] - currentState[_mapping[v]]);
					}
				}
			}
			return delta;
		}
		
		private double GetCouplingStrength(Vertex v, Vertex w)
        {
            if (v == null || w == null)
			{
				Logger.AddMessage(LogEntryType.Warning, "GetCoupling(v,w) called for null vertex.");
				return 0d;
			}

            // Unweighted coupling strength from user-supplied coupling strengths
            double couplingStrength = CouplingStrengths[new Tuple<Vertex,Vertex>(v,w)];
			
			// Compensation factor to be used for sporadic couplings
            double f = 1d;
            if (Compensate_cp)
                f = 1d / CouplingProbability;
			
            if (DegreeWeight)
                couplingStrength = (couplingStrength * f) / (double)v.Degree;
            else if (DoubleDegreeWeight)
                couplingStrength = (couplingStrength * f * ((double)w.Degree / _avgDeg)) / (double)v.Degree;

            return couplingStrength;
        }
		
		/// <summary>
		/// Computes the order parameter for a set of vertices
		/// </summary>
		/// <returns>
		/// The order parameter between 0 and 1
		/// </returns>
		/// <param name='vertices'>
		/// An array of vertices for which the order paramater shall be computed
		/// </param>
		public double GetOrder(Vertex[] vertices)
		{
			double sines = 0d;
			double cosines = 0d;
			double n = vertices.Length;
			foreach(Vertex v in vertices)
			{
				sines += Math.Sin(CurrentValues[_mapping[v]]);
				cosines += Math.Cos(CurrentValues[_mapping[v]]);
			}			
			return Math.Sqrt((sines * sines + cosines * cosines) / (n * n));
		}
		
		protected override void Finish()
		{					
			Logger.AddMessage(LogEntryType.Info, "Sychchronization module finished.");
		}		

        private Color ColorFromPhase(double phase)
        {
            int color = (int)(Math.Sin(phase) * 127f + 128f);
            return Color.FromArgb(255 - color, color, 0);
        }        
	}
}

