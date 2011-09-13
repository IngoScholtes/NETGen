using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

using MathNet.Numerics;

using NETGen.Core;
using NETGen.Dynamics;
using NETGen.Visualization;

namespace NETGen.Dynamics.EpidemicSynchronization
{
	public struct SyncResults
    {
        public double order;
        public long time;
    }
	
	public class EpidemicSynchronization : DiscreteDynamics<SyncResults>
	{
		Network _network;
		NetworkColorizer _colorizer;
		
		ConcurrentDictionary<Edge, double> _avgoffsets;
		ConcurrentDictionary<Vertex, long> _localClock;
		ConcurrentDictionary<Vertex, double> _period;
		ConcurrentDictionary<Vertex, double> _SineSignal;
		ConcurrentDictionary<Vertex, double> _CosineSignal;
		ConcurrentDictionary<Vertex, double> _phase;
		
		ConcurrentDictionary<Edge, double> _relativeCouplingFrequency;
		
		public ConcurrentDictionary<Vertex, double> _MuPeriods;
		public ConcurrentDictionary<Vertex, double> _SigmaPeriods;	
		
		MathNet.Numerics.Distributions.Normal _normal;
		
		double avgDeg;
		double order;
		
		//sync settings
        public bool Compensate_cp = false;
        public double MuPeriod = 100;
        public long SigmaPeriod = 20;
        public double PeriodCouplingStrength = 2d;
        public bool DegreeWeight = true;
        public bool DoubleDegreeWeight = false;
        public double CouplingProbability = 1d;		
		
		public Func<Vertex, Vertex> NeighborSelector;
		
		public EpidemicSynchronization(Network n, NetworkColorizer colorizer = null, Func<Vertex, Vertex> selectNeighbor = null)
		{
			_network = n;
			_colorizer = colorizer;
			
			_MuPeriods = new ConcurrentDictionary<Vertex, double>();
			_SigmaPeriods = new ConcurrentDictionary<Vertex, double>();
			
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
            avgDeg = 0d;
			order = 0d;

            _avgoffsets = new ConcurrentDictionary<Edge, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            _localClock = new ConcurrentDictionary<Vertex, long>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            _period = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);			
            _SineSignal = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            _CosineSignal = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
            _phase = new ConcurrentDictionary<Vertex, double>(System.Environment.ProcessorCount, (int) _network.VertexCount);
			_relativeCouplingFrequency = new ConcurrentDictionary<Edge, double>();			
			
			_normal = new MathNet.Numerics.Distributions.Normal(MuPeriod, SigmaPeriod);                      

            foreach (Edge e in _network.Edges)
			{
                _avgoffsets[e] = 0d;
				_relativeCouplingFrequency[e] = 0d;
			}

            foreach (Vertex v in _network.Vertices)
            {
				if(!_MuPeriods.ContainsKey(v))
                	_normal.Mean = MuPeriod;
				else
					_normal.Mean = _MuPeriods[v];
				
				if(!_SigmaPeriods.ContainsKey(v))
                	_normal.StdDev = SigmaPeriod;
				else
					_normal.StdDev = _SigmaPeriods[v];

                _period[v] = _normal.Sample();

                // random clock skews
                _localClock[v] = _network.NextRandom(0, (int) _normal.Mean);
                _phase[v] = getPhase(v, _localClock, _period);
                _SineSignal[v] = Math.Sin(_phase[v]);
				
				if(_colorizer != null)
					_colorizer[v] = ColorFromSignal(v, _SineSignal);			
            }
			Logger.AddMessage(LogEntryType.Info, "Sychchronization module initialized");
		}
	
		protected override void Step()
		{			
			// Simply reset all edge colors to the default
			if(_colorizer!= null)
				_colorizer.RecomputeColors((Edge e) => { return _colorizer.DefaultEdgeColor; });
			
			// perform coupling to the user supplied neighbor
            foreach (Vertex v in _network.Vertices)
            {
				if(_network.NextRandomDouble() <= CouplingProbability)
				{
	                // neighbor selection either up to the user-supplied lambda expression or defaulting to random neighbor
	                Vertex neighbor = NeighborSelector(v);								
					
					if(_colorizer!=null)
						_colorizer[v.GetEdgeToSuccessor(neighbor)] = Color.Red;
					
					_relativeCouplingFrequency[v.GetEdgeToSuccessor(neighbor)]++;
	
	                // actually perform coupling
	                AdjustPeriods(v, neighbor, _phase, _period, avgDeg);
				}
            }

            // Parallely advance clock, compute signal and phase
            Parallel.ForEach(_network.Vertices.ToArray(), v =>
            {
                _localClock[v]++;
                _phase[v] = getPhase(v, _localClock, _period);
                _SineSignal[v] = Math.Sin(_phase[v]);
                _CosineSignal[v] = Math.Cos(_phase[v]);
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
			double min = MathNet.Numerics.Statistics.Statistics.Minimum(_relativeCouplingFrequency.Values);
			double max = MathNet.Numerics.Statistics.Statistics.Maximum(_relativeCouplingFrequency.Values);
			
			if(_colorizer != null)
				_colorizer.RecomputeColors((Edge e) => {
					int color = (int)((_relativeCouplingFrequency[e]-min)/(max - min) * 255f);
            		return Color.FromArgb(Color.White.R, Color.White.G-color, Color.White.B-color);
				});
			Logger.AddMessage(LogEntryType.Info, "Sychchronization module finished");
		}
		
		private void AdjustPeriods(Vertex v, Vertex w, ConcurrentDictionary<Vertex, double> _phase, ConcurrentDictionary<Vertex, double> _period, double avgDeg)
        {
            if (v == null || w == null)
                return;
			
            // exchange phases
            // interpret phase as position on a circle and compute the angle between the nodes

            // unweighted coupling strength
            double couplingStrengthV = PeriodCouplingStrength;
            double couplingStrengthW = PeriodCouplingStrength;

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
			res.order = order;
			res.time = SimulationStep;
			return res;
		}
	}
}

