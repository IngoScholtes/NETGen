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
	/// A thread-safe storage that can be used to manipulate colors of individual nodes and edges
	/// </summary>
    public sealed class CustomColorIndexer
    {
        private ConcurrentDictionary<Vertex, SolidBrush> _customVertexColors;
        private ConcurrentDictionary<Edge, Pen> _customEdgeColors;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="NETGen.Visualization.CustomColorIndexer"/> class.
		/// </summary>
        internal CustomColorIndexer()
        {
            _customEdgeColors = new ConcurrentDictionary<Edge, Pen>(System.Environment.ProcessorCount, 0);
            _customVertexColors = new ConcurrentDictionary<Vertex, SolidBrush>(System.Environment.ProcessorCount, 0);
        }
		
		/// <summary>
		/// Gets the Brush of the specifed vertex
		/// </summary>
		/// <returns>
		/// The Brush of the specified vertex
		/// </returns>
		/// <param name='v'>
		/// The vertex for which the brush shall be returned
		/// </param>
        internal SolidBrush GetVertexBrush(Vertex v)
        {
            return _customVertexColors[v];
        }
		
		/// <summary>
		/// Gets the Pen of the specified 
		/// </summary>
		/// <returns>
		/// The Pen of the specified edge
		/// </returns>
		/// <param name='e'>
		/// The edge for which the Pen shall be returned
		/// </param>
        internal Pen GetEdgePen(Edge e)
        {
            return _customEdgeColors[e];
        }
		
		/// <summary>
		/// Recomputes the colors of all previously customized vertices according to the specified lambda expression
		/// </summary>
		/// <param name='transform'>
		/// A lambda expression that asigns a Color to a vertex
		/// </param>
        public void Recompute(Func<Vertex, Color> transform)
        {
            foreach (Vertex v in _customVertexColors.Keys.ToArray())
                _customVertexColors[v] = new SolidBrush(transform(v));
        }

		/// <summary>
		/// Removes all custom vertex and edge color assignments
		/// </summary>
        public void ClearAll()
        {
            lock (this)
            {
                _customVertexColors.Clear();
                _customEdgeColors.Clear();
            }
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
                    return _customVertexColors[v].Color;
            }
            set
            {
                     _customVertexColors[v] = new SolidBrush(value);
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
                return _customEdgeColors[e].Color;
            }
            set
            {
                _customEdgeColors[e] = new Pen(value);
            }
        }
		
		/// <summary>
		/// Determines whether a custom color has been set for the specified vertex v
		/// </summary>
		/// <returns>
		/// <c>true</c> if a custom color has been set for v; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='v'>
		/// A vertex
		/// </param>
        public bool HasCustomColor(Vertex v)
        {
            return _customVertexColors.ContainsKey(v);
        }
		
		/// <summary>
		/// Determines whether a custom color has been set for the specified edge e
		/// </summary>
		/// <returns>
		/// <c>true</c> if a custom color has been set for e; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='e'>
		/// An edge
		/// </param>
        public bool HasCustomColor(Edge e)
        {
            return _customEdgeColors.ContainsKey(e);
        }		
    }
}
