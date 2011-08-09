using System;
using System.Collections.Generic;
using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layout.RandomLayout
{
    public class RandomLayout : Dictionary<Vertex, Vector3>, ILayoutProvider 
    {		

		public RandomLayout()
		{
		}

        public void DoLayout(double width, double height, Network n)
        {
            foreach (Vertex v in n.Vertices)
            {
                if (!this.ContainsKey(v))
                {
                    Random r = new Random();
                    this[v] = new Vector3(v.Network.NextRandomDouble() * width, v.Network.NextRandomDouble() * height, 0);
                }
            }
        }
		
        public Vector3 GetPositionOfNode(Vertex v)
        {			
            return this[v];
        }
    }
}

