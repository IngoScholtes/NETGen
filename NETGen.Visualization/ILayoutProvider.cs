using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETGen.Visualization
{
    public interface ILayoutProvider
    {
        Vector3 GetPositionOfNode(NETGen.Core.Vertex v);
        void DoLayout(double width, double height, NETGen.Core.Network n);
    }
}
