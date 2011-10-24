using System;
using System.Drawing;
using System.Collections.Generic;

using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Dynamics.Spreading
{
	
	public class SIRSpreading : DiscreteDynamics
	{
		public double k = 1d;
		
		Network _network;
		NetworkColorizer _colorizer;
		
        List<Vertex> _infected;
        List<Vertex> _active;
		Dictionary<Vertex, bool> _infections;
		
		public SIRSpreading(Network n, NetworkColorizer colorizer = null)
		{
			_network = n;
			_colorizer = colorizer;
			
			_active = new List<Vertex>();			
			_infected = new List<Vertex>();
			_infections = new Dictionary<Vertex, bool>();
			
			
		}
		
		protected override void Init ()
		{
			_infected.Clear();
			_active.Clear();
			_infections.Clear();
			
			Vertex seed = _network.RandomVertex;
            _infected.Add(seed);
            _active.Add(seed);
			
			foreach (Vertex v in _network.Vertices)
                _infections[v] = false;
		}
		
		protected override void TimeStep (long time)
		{
			foreach (Vertex v in _active.ToArray())
            {
                Vertex neighbor = v.RandomNeighbor;
               
                if (neighbor != null && !_infections[neighbor])
                {
                    _infections[neighbor] = true;
                    _infected.Add(neighbor);
                    _active.Add(neighbor);
                }
                else if (neighbor != null)
                    if (_network.NextRandomDouble() <= 1d / k)
                        _active.Remove(v);
            }
		
			if(_colorizer!=null)
			{
				_colorizer.RecomputeColors( v => {
					if(_infections[v])
						return Color.Red;
					else
						return Color.Green;
				});
			}
		}
	}
}

