using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;

namespace NETGen.NetworkModels.WattsStrogatz
{
	
	/// <summary>
	/// This class represents a random netwrk generated according to the lattice model introduced by Watts and Strogatz
	/// </summary>
    public class WSNetwork : Network
    {
		
		/// <summary>
		/// Generates a random network according to the Watts/Strogatz model
		/// </summary>
		/// <param name='n'>
		/// The number of nodes in the network
		/// </param>
		/// <param name='m'>
		/// The number of edges per node
		/// </param>
		/// <param name='p'>
		/// THe probability wth which each edge is reconnected to a random node
		/// </param>
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
