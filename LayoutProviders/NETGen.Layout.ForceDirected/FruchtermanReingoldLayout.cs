using System;
using System.Linq;
using NETGen.Core;
using NETGen.Visualization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NETGen.Layout.FruchtermanReingold
{

    /// <summary>
    ///  A spring-based model according to (T. Fruchterman and E. Reingold 1991). Edges are thought to be elastic springs that lead to an attractive force between connected vertices. Furthermore, there is an antagonistic, repulsive force between every pair of vertices. Computation is being done in parallel on as many processing cores as available. 
    /// </summary>
	public class FruchtermanReingoldLayout : ILayoutProvider
	{
        /// <summary>
        ///  The number of iterations to be used in the computation of vertex positions
        /// </summary>
		private int _iterations = 0;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<Vertex, Vector3> _vertexPositions;
		
        /// <summary>
        /// Creates a Fruchterman/Reingold layout using a given number of iterations for the computation of forces and positions. A larger iterations value will enhance the layouting quality, but will cost more computational resources. 
        /// </summary>
        /// <param name="iterations"></param>
		public FruchtermanReingoldLayout (int iterations)
		{
			_iterations = iterations;
            _vertexPositions = new Dictionary<Vertex, Vector3>();		                      
		}

        /// <summary>
        /// Computes the layout of a network
        /// </summary>
        /// <param name="width">width of the frame</param>
        /// <param name="height">height of the frame</param>
        /// <param name="n">The network to compute the layout for</param>
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
                    if (!_vertexPositions.ContainsKey(v))
                        _vertexPositions[v] = new Vector3(rand.NextDouble() * width, rand.NextDouble() * height, 1d);
                    disp[v] = new Vector3(0d, 0d, 1d);

                    // Parallel computation of repulsive forces
                    Parallel.ForEach(_vertexPositions.Keys.ToArray(), u =>
                    {
                        if (v != u)
                        {
                            Vector3 delta = _vertexPositions[v] - _vertexPositions[u];
                            disp[v] = disp[v] + (delta / Vector3.Length(delta)) * repulsion(Vector3.Length(delta), _k);
                        }
                    });
                }
		        
                // Calculate attractive forces for pair of connected nodes
				Parallel.ForEach(n.Edges.ToArray(), e => 				
				{
					Vertex v = e.Source;
					Vertex w = e.Target;
                    Vector3 delta = _vertexPositions[v] - _vertexPositions[w];
					disp[v] = disp[v] - (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
					disp[w] = disp[w] + (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
				});

                // Limit to frame and include temperature cooling that reduces displacement step by step
				Parallel.ForEach(n.Vertices.ToArray(), v =>
				{
                    Vector3 vPos = _vertexPositions[v] + (disp[v] / Vector3.Length(disp[v])) * Math.Min(Vector3.Length(disp[v]), t);
                    vPos.X = Math.Min(width-10, Math.Max(10, vPos.X));
                    vPos.Y = Math.Min(height-10, Math.Max(10, vPos.Y));
                    _vertexPositions[v] = vPos;
				});
				t-= tempstep;
			}
		}

        /// <summary>
        /// A simple attractive force between connected vertices
        /// </summary>
        /// <param name="d"></param>
        /// <param name="_k"></param>
        /// <returns></returns>
        private double attraction(double d, double _k)
        {
            return Math.Pow(d, 2d) / _k;
        }

        /// <summary>
        /// A simple repulsive force between every pair of vertices
        /// </summary>
        /// <param name="d"></param>
        /// <param name="_k"></param>
        /// <returns></returns>
		double repulsion(double d, double _k)
		{
			return Math.Pow(_k, 2d) / d;
		}

        /// <summary>
        /// Returns the layout position of a node v
        /// </summary>
        /// <param name="v">The node to return the position for</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
		public Vector3 GetPositionOfNode(NETGen.Core.Vertex v)
		{
            return _vertexPositions[v];
		}
	}
}

