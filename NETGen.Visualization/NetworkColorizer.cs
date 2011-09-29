using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using NETGen.Core;
using System.Drawing;

namespace NETGen.Visualization
{
	
	/// <summary>
	/// A thread-safe storage for colors of nodes and edges
	/// </summary>
    public sealed class NetworkColorizer
    {
        private ConcurrentDictionary<Vertex, Color> _customVertexColors;
        private ConcurrentDictionary<Edge, Color> _customEdgeColors;
		
		public Color DefaultVertexColor = Color.DarkSlateGray;
		public Color DefaultEdgeColor = Color.Gray;
		public Color DefaultBackgroundColor = Color.White;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="NETGen.Visualization.CustomColorIndexer"/> class.
		/// </summary>
        public NetworkColorizer()
        {
            _customEdgeColors = new ConcurrentDictionary<Edge, Color>(System.Environment.ProcessorCount, 0);
            _customVertexColors = new ConcurrentDictionary<Vertex, Color>(System.Environment.ProcessorCount, 0);
        }		
		
		/// <summary>
		/// Recomputes the colors of all previously customized vertices according to the specified lambda expression
		/// </summary>
		/// <param name='transform'>
		/// A lambda expression that asigns a Color to a vertex
		/// </param>
        public void RecomputeColors(Func<Vertex, Color> transform)
        {
            foreach (Vertex v in _customVertexColors.Keys.ToArray())
                _customVertexColors[v] = transform(v);
        }
		
		/// <summary>
		/// Recomputes the colors of all previously customized vertices according to the specified lambda expression
		/// </summary>
		/// <param name='transform'>
		/// A lambda expression that asigns a Color to a vertex
		/// </param>
        public void RecomputeColors(Func<Edge, Color> transform)
        {
            foreach (Edge e in _customEdgeColors.Keys.ToArray())
                _customEdgeColors[e] = transform(e);
        }

		/// <summary>
		/// Removes all custom vertex and edge color assignments
		/// </summary>
        public void ClearAllColors()
        {
            _customVertexColors.Clear();
            _customEdgeColors.Clear();
        }
		
		public void ClearEdgeColors()
		{
			_customEdgeColors.Clear();
		}
		
		public void ClearVertexColors()
		{
			_customVertexColors.Clear();
		}
        
		/// <summary>
		/// Gets or sets the Color of the specified vertex
		/// </summary>
		/// <param name='v'>
		/// The vertex for which the color shall be set or returned
		/// </param>
        public Color this[Vertex v]
        {
            get
            {             
				if(_customVertexColors.ContainsKey(v))
                    return _customVertexColors[v];
				else
					return DefaultVertexColor;
            }
            set
            {
                 _customVertexColors[v] = value;
            }
        }
		
		/// <summary>
		/// Gets or sets the Color of the specified edge
		/// </summary>
		/// <param name='e'>
		/// The edge for which the color shall be set or returned
		/// </param>
        public Color this[Edge e]
        {
            get
            {
				if(_customEdgeColors.ContainsKey(e))
                	return _customEdgeColors[e];
				else
					return DefaultEdgeColor;
            }
            set
            {
                _customEdgeColors[e] = value;
            }
        }	
    }
}
