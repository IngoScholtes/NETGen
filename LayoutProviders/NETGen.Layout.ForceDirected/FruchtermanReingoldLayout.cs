using System;
using System.Linq;
using NETGen.Core;
using NETGen.Visualization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NETGen.Layout.FruchtermanReingold
{
	public class FruchtermanReingoldLayout : Dictionary<Vertex,Vector3>, ILayoutProvider
	{
		int _iterations = 0;		
		
		public FruchtermanReingoldLayout (int iterations)
		{
			_iterations = iterations;							                      
		}

        [MethodImpl(MethodImplOptions.Synchronized)]
		public void DoLayout(double width, double height, Network n)
		{
            double _area = width * height;
            double _k = Math.Sqrt(_area / (double)n.Vertices.Count());
            _k *= 0.75d;
            Random rand = new Random();
            // The displacement calculated for each vertex in each step
            Dictionary<Vertex, Vector3> disp = new Dictionary<Vertex, Vector3>();

            	
			double t = width/10;
            double tempstep = t / (double) _iterations;

			for (int i=0; i<_iterations; i++)
			{
                // Calculate repulsive forces for every pair of vertices		
                foreach (Vertex v in n.Vertices)
                {
                    if (!this.ContainsKey(v))
                        this[v] = new Vector3(rand.NextDouble() * width, rand.NextDouble() * height, 1d);
                    disp[v] = new Vector3(0d, 0d, 1d);

                    // Parallel computation of repulsive forces
                    Parallel.ForEach(Keys, u =>
                    {
                        if (v != u)
                        {
                            Vector3 delta = this[v] - this[u];
                            disp[v] = disp[v] + (delta / Vector3.Length(delta)) * repulsion(Vector3.Length(delta), _k);
                        }
                    });
                }
		        
                // Calculate attractive forces for pair of connected nodes
				Parallel.ForEach(n.Edges.ToArray(), e => 				
				{
					Vertex v = e.Source;
					Vertex w = e.Target;
					Vector3 delta = this[v] - this[w];
					disp[v] = disp[v] - (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
					disp[w] = disp[w] + (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
				});

                // Limit to frame and include temperature cooling that reduces displacement step by step
				Parallel.ForEach(n.Vertices.ToArray(), v =>
				{
					Vector3 vPos = this[v] + (disp[v] / Vector3.Length(disp[v])) * Math.Min(Vector3.Length(disp[v]), t);
                    vPos.X = Math.Min(width-10, Math.Max(10, vPos.X));
                    vPos.Y = Math.Min(height-10, Math.Max(10, vPos.Y));
					this[v] = vPos;
				});
				t-= tempstep;
			}
		}

        double attraction(double d, double _k)
        {
            return Math.Pow(d, 2d) / _k;
        }

		double repulsion(double d, double _k)
		{
			return Math.Pow(_k, 2d) / d;
		}

        [MethodImpl(MethodImplOptions.Synchronized)]
		public Vector3 GetPositionOfNode(NETGen.Core.Vertex v)
		{
			return this[v];
		}
	}
}

