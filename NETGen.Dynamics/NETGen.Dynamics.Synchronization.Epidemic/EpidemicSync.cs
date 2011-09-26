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
#endregion

#region NETGen libraries
using NETGen.Core;
using NETGen.Visualization;
#endregion

namespace NETGen.Dynamics.Synchronization
{
	public struct SyncResults
    {
        public double order;
        public long time;
    }
	
	// TODO: Change to resemble the frequency perspective of the original Kuramoto model
	public class EpidemicSync : DiscreteDynamics<SyncResults>
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
		/// The normal distribution used to assign initial periods
		/// </summary>
		MathNet.Numerics.Distributions.Normal _normal;
		
		/// <summary>
		/// The average degree of the network
		/// </summary>
		double _avgDeg;

		// Some dictionaries that speed up computation
		ConcurrentDictionary<Edge, double> _avgoffsets;
		ConcurrentDictionary<Vertex, long> _localClock;
		public ConcurrentDictionary<Vertex, double> Periods;
		ConcurrentDictionary<Vertex, double> _SineSignal;
		ConcurrentDictionary<Vertex, double> _CosineSignal;
		ConcurrentDictionary<Vertex, double> _phase;					
		
		/// <summary>
		/// Individual means of the normal distribution assigning the initial oscillator periods for multi-modal initial frequency distributions
		/// </summary>
		public ConcurrentDictionary<Vertex, double> PeriodMeans;
		
		/// <summary>
		/// Individual deviation of the normal distribution assigning the initial oscillator periods for multi-modal initial frequency distributions
		/// </summary>
		public ConcurrentDictionary<Vertex, double> PeriodStdDevs;	
		
		/// <summary>
		/// Default mean of the normal distribution assigning the initial oscillator periods (used for those vertices for which no individual means are given)
		/// </summary>
        public double PeriodMean = 100;
		
		/// <summary>
		/// Default standard deviation of the normal distribution assigning the initial oscillator periods (used for those vertices for which no individual standard deviation is given)
		/// </summary>
        public long PeriodStdDev = 20;
				
		/// <summary>
		/// Individual (directed) coupling strengths for each bidirectional edge in the network
		/// </summary>
		public Dictionary<Tuple<Vertex, Vertex>, double> CouplingStrengths;
		
		/// <summary>
		/// The constant coupling strength K like in the original Kuramoto model
		/// </summary>
        public double CouplingStrength = 2d;
		
		/// <summary>
		/// Whether or not to weight the coupling strength by the degree of vertices
		/// </summary>
        public bool DegreeWeight = true;
		
		/// <summary>
		/// Whether or not to weight the coupling strength by the degree of the coupling target
		/// </summary>
        public bool DoubleDegreeWeight = false;
		
		/// <summary>
		/// The probability with which a coupling takes place
		/// </summary>
        public double CouplingProbability = 1d;				
		
		/// <summary>
		/// Whether or not to compensate smaller coupling probability by a proportionate increase in coupling strength
		/// </summary>
		public bool Compensate_cp = false;
		
		/// <summary>
		/// This function can be used to assign custom neighbor selection strategies via a lambda expression. Unbiased random neighbor selection will be used by default. 
		/// </summary>
		public Func<Vertex, Vertex> NeighborSelector = null;
		
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
		public EpidemicSync(Network n, NetworkColorizer colorizer = null, Func<Vertex, Vertex> selectNeighbor = null)
		{
			_network = n;
			_colorizer = colorizer;
			
			PeriodMeans = new ConcurrentDictionary<Vertex, double>();
			PeriodStdDevs = new ConcurrentDictionary<Vertex, double>();
			CouplingStrengths = new Dictionary<Tuple<Vertex, Vertex>, double>();
			
			if(selectNeighbor==null)
				NeighborSelector = new Func<Vertex, Vertex>( v => {
					return v.RandomNeighbor;
				});
			else
				NeighborSelector = selectNeighbor;			
		}
		
		protected override void Init()
		{
			// Initialize all necessary values and dictionaries ... 
            _avgoffsets = new ConcurrentDictionary<Edge, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            _localClock = new ConcurrentDictionary<Vertex, long>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            Periods = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);			
            _SineSignal = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            _CosineSignal = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            _phase = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
			
			_normal = new MathNet.Numerics.Distributions.Normal(PeriodMean, PeriodStdDev);                      
			
			// Initialize coupling strengths with 
            foreach (Edge e in _network.Edges)
			{
                _avgoffsets[e] = 0d;
				Tuple<Vertex,Vertex> t1 = new Tuple<Vertex, Vertex>(e.Source, e.Target);
				Tuple<Vertex,Vertex> t2 = new Tuple<Vertex, Vertex>(e.Target, e.Source);
				
				if(!CouplingStrengths.ContainsKey(t1) && (e.EdgeType == EdgeType.Undirected || e.EdgeType == EdgeType.DirectedAB) )
					CouplingStrengths[t1] = CouplingStrength;
				
				if(!CouplingStrengths.ContainsKey(t2) && (e.EdgeType == EdgeType.Undirected || e.EdgeType == EdgeType.DirectedBA))
					CouplingStrengths[t2] = CouplingStrength;
			}

            foreach (Vertex v in _network.Vertices)
            {
				_avgDeg += v.Degree;
				
				// Assign distribution parameters for this vertex
				if(!PeriodMeans.ContainsKey(v))
                	_normal.Mean = PeriodMean;
				else
					_normal.Mean = PeriodMeans[v];				
				if(!PeriodStdDevs.ContainsKey(v))
                	_normal.StdDev = PeriodStdDev;
				else
					_normal.StdDev = PeriodStdDevs[v];
				
				// Assign the initial period/frequency of the vertex
                Periods[v] = _normal.Sample();

                // randomly skew the local clocks of nodes
                _localClock[v] = _network.NextRandom(0, (int) _normal.Mean);
                _phase[v] = getPhase(v, _localClock, Periods);
                _SineSignal[v] = Math.Sin(_phase[v]);
				_CosineSignal[v] = Math.Cos(_phase[v]);
				
				if(_colorizer != null)
					_colorizer[v] = ColorFromSignal(v, _SineSignal);			
            }
			
			_avgDeg /= (double) _network.VertexCount;
			
			Logger.AddMessage(LogEntryType.Info, string.Format("Sychchronization module initialized. Initial order = {0:0.000}", ComputeOrder(_network.Vertices.ToArray())));
		}
	
		protected override void Step()
		{			
			// Reset all edge colors to the default
			if(_colorizer!= null)
				_colorizer.RecomputeColors((Edge e) => { return _colorizer.DefaultEdgeColor; });
			
			// perform coupling to the user supplied neighbor
            foreach (Vertex v in _network.Vertices)
            {
				if(_network.NextRandomDouble() <= CouplingProbability)
				{
	                // neighbor selection either up to the user-supplied lambda expression or defaulting to random neighbor
	                Vertex neighbor = NeighborSelector(v);								
					
					// Color used edges
					if(_colorizer!=null)
						_colorizer[v.GetEdgeToSuccessor(neighbor)] = Color.Red;					
	
	                // actually perform coupling
	                AdjustPeriods(v, neighbor, _phase, Periods, _avgDeg);
				}
            }

            // Parallely advance clock, compute signal and phase
            Parallel.ForEach(_network.Vertices.ToArray(), v =>
            {
                _localClock[v]++;
                _phase[v] = getPhase(v, _localClock, Periods);
                _SineSignal[v] = Math.Sin(_phase[v]);
                _CosineSignal[v] = Math.Cos(_phase[v]);
				
				// Recolor nodes
				if (_colorizer!=null)
					_colorizer[v] = ColorFromSignal(v, _SineSignal);
            });
		}
		
		/// <summary>
		/// Computes the order parameter for a set of vertices
		/// </summary>
		/// <returns>
		/// The order parameter between 0 and 1
		/// </returns>
		/// <param name='vertices'>
		/// An array of vertices for ahich the order paramater shall be computed
		/// </param>
		public double ComputeOrder(Vertex[] vertices)
		{
			double avgSine = 0d;
			double avgCosine = 0d;			
			double count = 0;
			
			foreach(Vertex v in vertices)
			{
				if(v!=null)
				{
					avgSine += _SineSignal[v];
					avgCosine += _CosineSignal[v];
					count ++;
				}
			}
			
			avgSine /= count;
			avgCosine /= count;
			
			return Math.Sqrt(avgSine * avgSine + avgCosine * avgCosine);
		}
		
		protected override void Finish()
		{					
			Logger.AddMessage(LogEntryType.Info, "Sychchronization module finished");
		}
		
		private void AdjustPeriods(Vertex v, Vertex w, ConcurrentDictionary<Vertex, double> _phase, ConcurrentDictionary<Vertex, double> _period, double avgDeg)
        {
            if (v == null || w == null)
                return;
			
            // exchange phases
            // interpret phase as position on a circle and compute the angle between the nodes

            // unweighted coupling strength
            double couplingStrengthV = CouplingStrengths[new Tuple<Vertex,Vertex>(v,w)];
            double couplingStrengthW = CouplingStrengths[new Tuple<Vertex,Vertex>(w,v)];

            double f = 1d;
            if (Compensate_cp)
                f = 1d / CouplingProbability;

            // divide by degree of node
            if (DegreeWeight)
            {
                couplingStrengthV = (couplingStrengthV * f) / (double)v.Degree;
                couplingStrengthW = (couplingStrengthW * f) / (double)w.Degree;
            }
            // perform weighting based on the source's degree
            else if (DoubleDegreeWeight)
            {
                couplingStrengthV = (couplingStrengthV * f * ((double)w.Degree / avgDeg)) / (double)v.Degree;
                couplingStrengthW = (couplingStrengthW * f * ((double)v.Degree / avgDeg)) / (double)w.Degree;
            }

            // adjust local clock speed based on oscillator angle and coupling strength
            double adjV = Math.Sin(_phase[v] - _phase[w]) * couplingStrengthV;
            double adjW = Math.Sin(_phase[w] - _phase[v]) * couplingStrengthW;

            // if the resulting periods are greater zero
            if (_period[v] + adjV > 0)
                _period[v] += adjV;

            if (_period[w] + adjW > 0)
                _period[w] += adjW;
        }

        static double getPhase(Vertex v, ConcurrentDictionary<Vertex, long> _localClock, ConcurrentDictionary<Vertex, double> _period)
        {
            // position in local period (between 0 and 1)
            double noise = 0d;
            double cyclePos = ((double)(_localClock[v]) % _period[v]) / _period[v] + noise;
            cyclePos = cyclePos % 1d;
            return 2d * Math.PI * cyclePos;
        }

        static Color ColorFromSignal(Vertex v, ConcurrentDictionary<Vertex, double> _SineSignal)
        {
            int color = (int)(_SineSignal[v] * 127f + 128f);
            return Color.FromArgb(255 - color, color, 0);
        }        
		
		public override SyncResults Collect()
		{
			SyncResults res = new SyncResults();
			res.order = ComputeOrder(_network.Vertices.ToArray());
			res.time = SimulationStep;
			return res;
		}
	}
}

