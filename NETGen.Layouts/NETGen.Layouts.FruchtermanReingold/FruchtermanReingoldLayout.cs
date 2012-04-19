using System;
using System.Linq;
using NETGen.Core;
using NETGen.Visualization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NETGen.Layouts.FruchtermanReingold
{	
	
    /// <summary>
    ///  A spring-based model according to (T. Fruchterman and E. Reingold 1991). Edges are thought to be elastic springs that lead to an attractive force between connected vertices. Furthermore, there is an antagonistic, repulsive force between every pair of vertices. Computation is being done in parallel on as many processing cores as available. 
    /// </summary>
	public class FruchtermanReingoldLayout : LayoutProvider
	{
		
        /// <summary>
        ///  The number of iterations to be used in the computation of vertex positions
        /// </summary>
		private int _iterations = 0;

        private ConcurrentDictionary<Vertex, Vector3> _vertexPositions;		
		
		private ConcurrentBag<Vertex> _newVertices;
		
        /// <summary>
        /// Creates a Fruchterman/Reingold layout using a given number of iterations for the computation of forces and positions. A larger iterations value will enhance the layouting quality, but will require more computation
        /// </summary>
        /// <param name="iterations"></param>
		public FruchtermanReingoldLayout (int iterations)
		{
			_iterations = iterations;
            _vertexPositions = new ConcurrentDictionary<Vertex, Vector3>();		
			_newVertices = new ConcurrentBag<Vertex>();
		}
		
		public override void Init(double width, double height, Network network)
		{			
			base.Init(width, height, network);
			
			foreach(Vertex v in network.Vertices)
			{
				_vertexPositions[v] = new Vector3(network.NextRandomDouble() * width, network.NextRandomDouble() * height, 1d);
				_newVertices.Add(v);
			}
			
			// Add vertex to _newVertices whenever one is added to the network
			network.OnVertexAdded+=new Network.VertexUpdateHandler( delegate(Vertex v) {
				_vertexPositions[v] = new Vector3(network.NextRandomDouble() * width, network.NextRandomDouble() * height, 1d);
				_newVertices.Add(v);
			});			
			
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void TouchVertex(Vertex v)
		{
			_newVertices.Add(v);
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void TouchEdge (Edge e)
		{
			_newVertices.Add(e.Source);
			_newVertices.Add(e.Target);
		}

        /// <summary>
        /// Computes the position of all vertices of a network
        /// </summary>
        /// <param name="width">width of the frame</param>
        /// <param name="height">height of the frame</param>
        /// <param name="n">The network to compute the layout for</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
		public override void DoLayout()
		{
			Logger.AddMessage(LogEntryType.Info, "Computing Fruchterman-Reingold layout ...");
			DateTime start = DateTime.Now;
            double _area = Width * Height;
            double _k = Math.Sqrt(_area / (double)Network.Vertices.Count());
            _k *= 0.75d;						
			
            // The displacement calculated for each vertex in each step
            ConcurrentDictionary<Vertex, Vector3> disp = new ConcurrentDictionary<Vertex, Vector3>(System.Environment.ProcessorCount, (int) Network.VertexCount);
            	
			double t = Width/10;
            double tempstep = t / (double) _iterations;			
							
			Vertex[] vertices = Network.Vertices.ToArray();
			Edge[] edges = Network.Edges.ToArray();				
			
			for (int i=0; i<_iterations; i++)
			{
                // parallely Calculate repulsive forces of nodes to every new node
                Parallel.ForEach(_newVertices, v =>
                {
                    disp[v] = new Vector3(0d, 0d, 1d);

                    // computation of repulsive forces
                    foreach(Vertex u in vertices)
                    {
                        if (v != u)
                        {
                            Vector3 delta = _vertexPositions[v] - _vertexPositions[u];
                            disp[v] = disp[v] + (delta / Vector3.Length(delta)) * repulsion(Vector3.Length(delta), _k);
                        }
                    }
                });				
		        
                // Parallely calculate attractive forces for all pairs of connected nodes
				Parallel.ForEach(edges, e =>
				{
					Vertex v = e.Source;
					Vertex w = e.Target;
                    if (_vertexPositions.ContainsKey(v) && _vertexPositions.ContainsKey(w))
                    {
                        Vector3 delta = _vertexPositions[v] - _vertexPositions[w];
                        if (_newVertices.Contains(v))
                            disp[v] = disp[v] - (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
                        if (_newVertices.Contains(w))
                            disp[w] = disp[w] + (delta / Vector3.Length(delta)) * attraction(Vector3.Length(delta), _k);
                    }
				});

                // Limit to frame and include temperature cooling that reduces displacement step by step
				Parallel.ForEach(_newVertices, v =>
				{
                    Vector3 vPos = _vertexPositions[v] + (disp[v] / Vector3.Length(disp[v])) * Math.Min(Vector3.Length(disp[v]), t);
                    vPos.X = Math.Min(Width-10, Math.Max(10, vPos.X));
                    vPos.Y = Math.Min(Height-10, Math.Max(10, vPos.Y));
                    _vertexPositions[v] = vPos;
				});
				t-= tempstep;
				
				Logger.AddMessage(LogEntryType.Info, string.Format("Layout step {0} computed in {1} ms", i, (DateTime.Now - start).TotalMilliseconds.ToString()));
				start = DateTime.Now;
			}
			_newVertices = new ConcurrentBag<Vertex>();
			Logger.AddMessage(LogEntryType.Info, "Layout completed");
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
		public override Vector3 GetPositionOfNode(NETGen.Core.Vertex v)
		{
            if (_vertexPositions.ContainsKey(v))
                return _vertexPositions[v];
            else return new Vector3();
		}
	}
}

