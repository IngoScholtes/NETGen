using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using NETGen.Visualization;
using NETGen.Core;

namespace NETGen.Layouts.Positioned
{

    /// <summary>
    /// A simple layouting mechanism that can be used to provide application-dependent vertex positions in a plane, e.g. for networks embedded into a eucledian space. Positions can simply be set and changed by indexing the object instance with the vertex. 
    /// </summary>
    public class PositionedLayout : LayoutProvider 
    {		
		ConcurrentDictionary<Vertex, Vector3> _vertexPositions;
		
		public PositionedLayout()
		{
			_vertexPositions = new ConcurrentDictionary<Vertex, Vector3>();
		}
		
        /// <summary>
        /// Since vertex positions for the layout are being provided by the user, there's nothing to do here really :-)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="n"></param>
        public override void DoLayout()
        {
            // nothing to do here in this special case I guess ...  :-)
        }
		
		public void SetPositionOfNode(Vertex v, Vector3 pos)
		{
			_vertexPositions[v] = pos;
		}

        /// <summary>
        /// Returns the layout position of a node v
        /// </summary>
        /// <param name="v">The node to return the position for</param>
        /// <returns></returns>
        public override Vector3 GetPositionOfNode(Vertex v)
        {
            if (!_vertexPositions.ContainsKey(v))
                return new Vector3();
            else
                return _vertexPositions[v];
        }		
    }
}
