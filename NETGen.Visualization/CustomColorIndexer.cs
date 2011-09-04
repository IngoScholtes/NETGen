using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using NETGen.Core;
using System.Drawing;

namespace NETGen.Visualization
{
    public sealed class CustomColorIndexer
    {
        private ConcurrentDictionary<Vertex, SolidBrush> _customVertexColors;
        private ConcurrentDictionary<Edge, Pen> _customEdgeColors;

        internal CustomColorIndexer()
        {
            _customEdgeColors = new ConcurrentDictionary<Edge, Pen>(System.Environment.ProcessorCount, 0);
            _customVertexColors = new ConcurrentDictionary<Vertex, SolidBrush>(System.Environment.ProcessorCount, 0);
        }

        internal SolidBrush GetVertexBrush(Vertex v)
        {
            lock(this)
                return _customVertexColors[v];
        }

        internal Pen GetEdgePen(Edge e)
        {
            lock(this)
                return _customEdgeColors[e];
        }

        public void Recompute(Func<Vertex, Color> transform)
        {
            foreach (Vertex v in _customVertexColors.Keys.ToArray())
                _customVertexColors[v] = new SolidBrush(transform(v));
        }

        public void ClearAll()
        {
            lock (this)
            {
                _customVertexColors.Clear();
                _customEdgeColors.Clear();
            }
        }
        
        public Color this[Vertex v]
        {
            get
            {
                lock (this)                
                    return _customVertexColors[v].Color;
            }
            set
            {
                lock(this)
                     _customVertexColors[v] = new SolidBrush(value);
            }
        }

        public bool HasCustomColor(Vertex v)
        {
            lock (this)
                return _customVertexColors.ContainsKey(v);
        }

        public bool HasCustomColor(Edge e)
        {
            lock (this)
                return _customEdgeColors.ContainsKey(e);
        }

        public Color this[Edge e]
        {
            get
            {
                    lock(this)
                        return _customEdgeColors[e].Color;
            }
            set
            {
                lock(this)
                    _customEdgeColors[e] = new Pen(value);
            }
        }
    }
}
