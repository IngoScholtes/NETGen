using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layout.Radial
{
    public class RadialLayout : Dictionary<Vertex, Vector3>, ILayoutProvider
    {
        Dictionary<int, double> startPosPerLevel = new Dictionary<int, double>();

        public Vector3 GetPositionOfNode(Vertex v)
        {
            return this[v];
        }

        private static Vector3 getPosition(double width, double height, double angle, int level, double radius)
        {
            level = level + 1;
            Vector3 center = new Vector3(width / 2d, height / 2d, 0);
            Vector3 pos = new Vector3(center.X + level * radius * Math.Sin(angle), center.Y - level * radius * Math.Cos(angle), 0d);
            return pos;

        }

        public void DoLayout(double width, double height, Network net)
        {
            Dictionary<int, int> nodesPerLevel = new Dictionary<int, int>();
            Dictionary<int, int> countPerLevel = new Dictionary<int, int>();


            double n = (double)net.VertexCount;
            int maxLevel = 0;

            double basis = 2d;

            //assign levels 
            foreach (Vertex v in net.Vertices)
            {
                int level = (int)Math.Round(Math.Log(n / 2, basis) - Math.Log(Math.Max((double)v.Degree, 7d), basis));
                if (level < 0)
                    level = (int)Math.Round(Math.Log(n / 2, basis) - Math.Log(1d, basis));
                maxLevel = Math.Max(level, maxLevel);
                if (!nodesPerLevel.ContainsKey(level))
                {
                    nodesPerLevel[level] = 1;
                    if (!startPosPerLevel.ContainsKey(level))
                        startPosPerLevel[level] = net.NextRandomDouble() * Math.PI * 2d;
                }
                else
                    nodesPerLevel[level]++;
            }

            // assign positions 
            foreach (Vertex v in net.Vertices)
            {
                int level = (int)Math.Round(Math.Log(n / 2, basis) - Math.Log(Math.Max((double)v.Degree, 7d), basis));
                if (level < 0)
                    level = (int)Math.Round(Math.Log(n / 2, basis) - Math.Log(1d, basis));
                if (!countPerLevel.ContainsKey(level))
                    countPerLevel[level] = 1;
                else
                    countPerLevel[level]++;
                double radius = ((double)width / 2d) / ((double)maxLevel + 1);
                double angle = startPosPerLevel[level] + ((double)countPerLevel[level] / (double)nodesPerLevel[level]) * Math.PI * 2d;

                
               this[v] = getPosition(width, height, angle, level, radius);
            
            //     v.VertexSize = Math.Max((int)(5 + (((double)v.Degree) / (double)(n / 2)) * 20d), 1);
            }
        }
    }
}
