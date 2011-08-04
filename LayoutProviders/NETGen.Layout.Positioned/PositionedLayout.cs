using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Visualization;
using NETGen.Core;

namespace NETGen.Layout.Positioned
{
    public class PositionedLayout : Dictionary<Vertex, Point3D>, ILayoutProvider 
    {
        public Point3D GetPositionFromNode(Vertex v)
        {
            return this[v];
        }
    }
}
