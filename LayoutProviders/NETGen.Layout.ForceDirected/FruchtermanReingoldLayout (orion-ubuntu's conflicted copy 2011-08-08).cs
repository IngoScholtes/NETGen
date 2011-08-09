using System;
using System.Linq;
using NETGen.Core;
using NETGen.Visualization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NETGen.Layout.FruchtermanReingold
{
	public class FruchtermanReingoldLayout : Dictionary<Vertex,Vector3>, ILayoutProvider
	{
		double _area = 0d;
		double _k = 0d;
		double _width = 0d;
		double _height = 0d;
		int _iterations = 0;		
		Network _n = null;
		
		public FruchtermanReingoldLayout (int width, int height, int iterations, Network n)
		{
			_iterations = iterations;			
			_area = width * height;
			_width = width;
			_height = height;
			_k = Math.Sqrt(_area/(double) n.Vertices.Count());
            _k /= 3d;
			_n = n;
            // assign random positions
            Random rand = new Random();
            foreach (Vertex v in _n.Vertices)
                this[v] = new Vector3(rand.NextDouble() * _width, rand.NextDouble() * _height, 1d);
		}
		
		public void DoLayout()
		{
            _k = Math.Sqrt(_area / (double)_n.Vertices.Count());
            _k /= 3d;
            Random rand = new Random();
            // The displacement calculated for each vertex in each step
            Dictionary<Vertex, Vector3> disp = new Dictionary<Vertex, Vector3>();

            // Calculate repulsive forces for every pair of vertices			
			double t = _width/10;
            double tempstep = t / (double) _iterations;				
			
			for (int i=0; i<_iterations; i++)
			{
				Parallel.ForEach(_n.Vertices, v => 
				{
                    if(!this.ContainsKey(v))
                        this[v] = new Vector3(rand.NextDouble() * _width, rand.NextDouble() * _height, 1d);
					disp[v] = new Vector3(0d, 0d, 1d);
					foreach(Vertex u in Keys)
						if(v!=u)
						{
							Vector3 delta = this[v] - this[u];
							disp[v] = disp[v] + (delta / Vector3.Length(delta)) * repulsion(Vector3.Length(delta));							
						}
				});
		        
                // Calculate attractive forces for pair of connected nodes
				Parallel.ForEach(_n.Edges, e => 				
				{
					Vertex v = e.Source;
					Vertex w = e.Target;
					Vector3 delta = this[v] - this[w];
					disp[v] = disp[v] - (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta));
					disp[w] = disp[w] + (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta));
				});

                // Limit to frame and include temperature cooling that reduces displacement step by step
				foreach(Vertex v in _n.Vertices)
				{
					Vector3 vPos = this[v] + (disp[v] / Vector3.Length(disp[v])) * Math.Min(Vector3.Length(disp[v]), t);
                    vPos.X = Math.Min(_width-10, Math.Max(10, vPos.X));
                    vPos.Y = Math.Min(_height-10, Math.Max(10, vPos.Y));
					this[v] = vPos;
				}
				t-= tempstep;
			}
		}

        double attraction(double d)
        {
            return Math.Pow(d, 2d) / _k;
        }

		double repulsion(double d)
		{
			return Math.Pow(_k, 2d) / d;
		}				
		
		public Vector3 GetPositionFromNode(NETGen.Core.Vertex v)
		{
			return this[v];
		}
	}
}

