using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;

namespace NETGen.NetworkModels.BarabasiAlbert
{
    public class BANetwork : Network
    {
        public BANetwork(int n, int m0, int k)
        {
            // list of all potential neighbors
            List<Vertex> neighbors = new List<Vertex>();

            // Create initial vertex set
            for (int i = 0; i < m0; i++)
                CreateVertex();

            // connect to clique
            foreach (Vertex v in Vertices.ToArray())
                foreach (Vertex w in Vertices.ToArray())
                    if (!v.IsSuccessor(w) && v != w)
                    {
                        CreateEdge(v, w);
                        neighbors.Add(v);
                        neighbors.Add(w);
                    }

            // Preferential Attachment for remaining vertices
            for (int i = m0; i < n; i++)
            {
                Vertex v = CreateVertex();

                List<Edge> new_edges = new List<Edge>();

                for (int j = 0; j < k; j++)
                {
                    Vertex node = neighbors[NextRandom(neighbors.Count)];
                    new_edges.Add(CreateEdge(v, node));
                }

                foreach (Edge e in new_edges)
                {
                    neighbors.Add(e.Source);
                    neighbors.Add(e.Target);
                }
            }
        }
    }
}
