using System;
using System.Drawing;
using System.Collections.Generic;

using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Dynamics.Spreading
{
	
	public class SimpleSpreading : DiscreteDynamics
	{
		Network _network;
		NetworkColorizer _colorizer;
		List<Vertex> _infected;
		Dictionary<Vertex, long> _infectionTime;
		
		public SimpleSpreading(Network n, NetworkColorizer colorizer = null)
		{
			_network = n;
			_colorizer = colorizer;
			_infected = new List<Vertex>();
			_infectionTime = new Dictionary<Vertex, long>();
		}
		
		protected override void Init ()
		{
			_infected.Clear();
			_infected.Add(_network.RandomVertex);
		}
		
		protected override void TimeStep (long time)
		{
			foreach (Vertex v in _infected.ToArray())
                {
                    Vertex neighbor = v.RandomNeighbor;

                    if (neighbor != null && _infectionTime[neighbor] == int.MinValue)
                    {
                        _infectionTime[neighbor] = SimulationTime;
                        _infected.Add(neighbor);
                    }
                }
			
			if(_colorizer!=null)
			{
				_colorizer.RecomputeColors( v => {
					if(_infected.Contains(v))
						return Color.Red;
					else
						return Color.Green;
				});
			}
		}

	}
}

