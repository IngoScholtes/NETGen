using System;
using System.Linq;
using NETGen.Core;
using NETGen.Visualization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NETGen.Layout.FruchtermanReingold
{	
	
    /// <summary>
    ///  A spring-based model according to (T. Fruchterman and E. Reingold 1991). Edges are thought to be elastic springs that lead to an attractive force between connected vertices. Furthermore, there is an antagonistic, repulsive force between every pair of vertices. Computation is being done in parallel on as many processing cores as available. 
    /// </summary>
	public class FruchtermanReingoldLayout : ILayoutProvider
	{
		private bool _laidout = false;
		
        /// <summary>
        ///  The number of iterations to be used in the computation of vertex positions
        /// </summary>
		private int _iterations = 0;

        private ConcurrentDictionary<Vertex, Vector3> _vertexPositions;
		
        /// <summary>
        /// Creates a Fruchterman/Reingold layout using a given number of iterations for the computation of forces and positions. A larger iterations value will enhance the layouting quality, but will require more computation
        /// </summary>
        /// <param name="iterations"></param>
		public FruchtermanReingoldLayout (int iterations)
		{
			_iterations = iterations;
            _vertexPositions = new ConcurrentDictionary<Vertex, Vector3>();		                      
		}

        /// <summary>
        /// Computes the position of all vertices of a network
        /// </summary>
        /// <param name="width">width of the frame</param>
        /// <param name="height">height of the frame</param>
        /// <param name="n">The network to compute the layout for</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
		public void DoLayout(double width, double height, Network n)
		{
			DateTime start = DateTime.Now;
            double _area = width * height;
            double _k = Math.Sqrt(_area / (double)n.Vertices.Count());
            _k *= 0.75d;						
			
            // The displacement calculated for each vertex in each step
            ConcurrentDictionary<Vertex, Vector3> disp = new ConcurrentDictionary<Vertex, Vector3>(System.Environment.ProcessorCount, (int) n.VertexCount);
			
			_vertexPositions = new ConcurrentDictionary<Vertex, Vector3>(System.Environment.ProcessorCount, (int) n.VertexCount);
            	
			double t = width/10;
            double tempstep = t / (double) _iterations;
			
			Parallel.ForEach(n.Vertices.ToArray(), v=>
            {
                _vertexPositions[v] = new Vector3(n.NextRandomDouble() * width, n.NextRandomDouble() * height, 1d);
				disp[v] = new Vector3(0d, 0d, 1d);
			});
			
			_laidout = true;
			
			System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(delegate(object o)	
			{			
				for (int i=0; i<_iterations; i++)
				{
	                // parallely Calculate repulsive forces for every pair of vertices		
	                Parallel.ForEach(n.Vertices.ToArray(), v =>
	                {
	                    disp[v] = new Vector3(0d, 0d, 1d);
	
	                    // computation of repulsive forces
	                    foreach(Vertex u in n.Vertices.ToArray())
	                    {
	                        if (v != u)
	                        {
	                            Vector3 delta = _vertexPositions[v] - _vertexPositions[u];
	                            disp[v] = disp[v] + (delta / Vector3.Length(delta)) * repulsion(Vector3.Length(delta), _k);
	                        }
	                    }
	                });				
			        
	                // Parallely calculate attractive forces for all pairs of connected nodes
					foreach(Edge e in n.Edges)				
					{
						Vertex v = e.Source;
						Vertex w = e.Target;
	                    Vector3 delta = _vertexPositions[v] - _vertexPositions[w];
						disp[v] = disp[v] - (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
						disp[w] = disp[w] + (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
					}
	
	                // Limit to frame and include temperature cooling that reduces displacement step by step
					foreach(Vertex v in n.Vertices)
					{
	                    Vector3 vPos = _vertexPositions[v] + (disp[v] / Vector3.Length(disp[v])) * Math.Min(Vector3.Length(disp[v]), t);
	                    vPos.X = Math.Min(width-10, Math.Max(10, vPos.X));
	                    vPos.Y = Math.Min(height-10, Math.Max(10, vPos.Y));
	                    _vertexPositions[v] = vPos;
					}
					t-= tempstep;
				}
			}));
			Console.WriteLine("Layout took: "+ (DateTime.Now - start).TotalMilliseconds.ToString() + " ms");
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
		public Vector3 GetPositionOfNode(NETGen.Core.Vertex v)
		{
            return _vertexPositions[v];
		}
		
		public bool IsLaidout()
		{
			return _laidout;
		}
	}
}

