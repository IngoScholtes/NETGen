using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;
using System.Threading.Tasks;

namespace NETGen.NetworkModels.ErdoesRenyi
{
    public class ERNetwork : Network
    {
        public ERNetwork(int n, int expectedEdges)
        {
            GenerateNetwork(n, expectedEdges / Math.Pow(n, 2d));
        }

        

        public ERNetwork(int n, double p)
        {
            GenerateNetwork(n, p);
        }

        void GenerateNetwork(int n, double p)
        {
            for (int i = 0; i < n; i++)
                CreateVertex();

            List<Edge> toAdd = new List<Edge>();

            foreach (Vertex v in Vertices)
            {
                foreach (Vertex w in Vertices)
                {
                    if (v != w)
                    {
                        double prob = NextRandomDouble();
                        if (prob < p)
                            toAdd.Add(new Edge(v, w, this));
                    }
                }
            }

            foreach (Edge e in toAdd)
                if (!e.Source.IsSuccessor(e.Target))
                    AddEdge(e);
        }
    }
}
