using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;

namespace NETGen.NetworkModels.WattsStrogatz
{
    public class WSNetwork : Network
    {
        public WSNetwork(int n, int m, double p)
        {
            Dictionary<int, Vertex> vertices = new Dictionary<int, Vertex>();
            int k = m / n;
            // vertices
            for (int i = 0; i < n; i++)
                vertices[i] = CreateVertex();            

            // connect vertices
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j <= i + k; j++)
                {
                    int ix = j % n;
                    if (!vertices[ix].IsSuccessor(vertices[i]))
                        CreateEdge(vertices[ix], vertices[i]);
                }
            }

            List<Edge> toRemove = new List<Edge>();
            List<Edge> toAdd = new List<Edge>();

            foreach (Edge e in Edges)
            {
                if (NextRandomDouble() < p)
                {
                    toRemove.Add(e);
                    Vertex source = e.Source;
                    Edge newEdge = null;

                    Vertex target = RandomVertexOtherThan(source);
                    if (target != null)
                        newEdge = new Edge(source, target, this);

                    toAdd.Add(newEdge);
                }
            }
            foreach (Edge e in toRemove)
                RemoveEdge(e);

            foreach (Edge e in toAdd)
                AddEdge(e);
        }
    }
}
