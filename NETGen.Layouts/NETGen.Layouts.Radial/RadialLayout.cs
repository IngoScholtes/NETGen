using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layout.Radial
{

    /// <summary>
    /// A radial layout, in which vertices are arranged on different circular levels depending on their degree. High degree vertices will be positioned in the center, low degree vertices will be positioned on the outer rim of the layout.
    /// </summary>
    public class RadialLayout : ConcurrentDictionary<Vertex, Vector3>, ILayoutProvider
    {        
		private bool _laidout = false;
		
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
            ConcurrentDictionary<int, int> nodesPerLevel = new ConcurrentDictionary<int, int>(System.Environment.ProcessorCount, 100);
            ConcurrentDictionary<int, int> countPerLevel = new ConcurrentDictionary<int, int>(System.Environment.ProcessorCount, 100);
			ConcurrentDictionary<int, double> startPosPerLevel = new ConcurrentDictionary<int, double>(System.Environment.ProcessorCount, 0);

            double n = (double)net.VertexCount;
            int maxLevel = 0;

            double basis = 2d;

            //assign levels 
            Parallel.ForEach(net.Vertices.ToArray(), v => 
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
            });

            // assign positions 
            Parallel.ForEach(net.Vertices.ToArray(), v => 
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
            });
			_laidout = true;
        }
		
		public bool IsLaidout()
		{
			return _laidout;
		}
    }
}
