using System;
using System.Collections.Generic;
using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layout.RandomLayout
{

    /// <summary>
    /// A simple random layout that assigns a random position to each vertex
    /// </summary>
    public class RandomLayout : ILayoutProvider 
    {

        Dictionary<Vertex, Vector3> _vertexPositions;

        /// <summary>
        /// Initializes a simple random layout that assigns random positions to vertices
        /// </summary>
		public RandomLayout()
		{
            _vertexPositions = new Dictionary<Vertex, Vector3>();
		}

        /// <summary>
        /// Assigns the random positions if they have not been assigned before ... 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="n"></param>
        public void DoLayout(double width, double height, Network n)
        {
            foreach (Vertex v in n.Vertices)
            {
                if (!_vertexPositions.ContainsKey(v))
                    _vertexPositions[v] = new Vector3(v.Network.NextRandomDouble() * width, v.Network.NextRandomDouble() * height, 0);
            }
        }
		
        /// <summary>
        /// Returns the vertex position of node v
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector3 GetPositionOfNode(Vertex v)
        {
            return _vertexPositions[v];
        }
    }
}

