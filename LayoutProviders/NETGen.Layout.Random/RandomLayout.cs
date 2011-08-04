using System;
using System.Collections.Generic;
using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layout.RandomLayout
{
    public class RandomLayout : Dictionary<Vertex, Point3D>, ILayoutProvider 
    {		
		int width, height, depth;
		public RandomLayout(int width, int height, int depth)
		{
			this.width = width;
			this.height = height;
			this.depth = depth;
		}
		
        public Point3D GetPositionFromNode(Vertex v)
        {
			if (!this.ContainsKey(v))
				{
					Random r = new Random();
					this[v] = new Point3D(r.Next(width), r.Next(height), r.Next(depth));
				}
            return this[v];
        }
    }
}

