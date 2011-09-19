using System;
using System.Collections.Concurrent;
using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layouts.RandomLayout
{

    /// <summary>
    /// A simple random layout that assigns a random position to each vertex
    /// </summary>
    public class RandomLayout : LayoutProvider 
    {
        ConcurrentDictionary<Vertex, Vector3> _vertexPositions;

        /// <summary>
        /// Initializes a simple random layout that assigns random positions to vertices
        /// </summary>
		public RandomLayout()
		{
            _vertexPositions = new ConcurrentDictionary<Vertex, Vector3>();
		}
      
        public override void DoLayout()
        {
			// Nothing to do here ... 
        }
		
        /// <summary>
        /// Returns the vertex position of node v
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public override Vector3 GetPositionOfNode(Vertex v)
        {
			if(!_vertexPositions.ContainsKey(v))
				_vertexPositions[v] = new Vector3(v.Network.NextRandomDouble() * Width, v.Network.NextRandomDouble() * Height, 0);
            return _vertexPositions[v];
        }		
    }
}

