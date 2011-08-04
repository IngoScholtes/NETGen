using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETGen.Visualization
{
    public interface ILayoutProvider
    {
        Point3D GetPositionFromNode(NETGen.Core.Vertex v);
    }
}
