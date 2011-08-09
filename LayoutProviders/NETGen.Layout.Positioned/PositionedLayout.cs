using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Visualization;
using NETGen.Core;

namespace NETGen.Layout.Positioned
{
    public class PositionedLayout : Dictionary<Vertex, Vector3>, ILayoutProvider 
    {
        public void DoLayout(double width, double height, Network n)
        {
            // nothing to do here I guess ... 
        }

        public Vector3 GetPositionOfNode(Vertex v)
        {
            if (!ContainsKey(v))
                return new Vector3();
            else
                return this[v];
        }
    }
}
