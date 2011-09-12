using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;
using System.Threading.Tasks;

namespace NETGen.NetworkModels.ErdoesRenyi
{
	
	/// <summary>
	/// Represents a random network generated according to the G(n,p) or G(n,m) model by Pal Erdoes and Alfred Renyi
	/// </summary>
    public class ERNetwork : Network
    {
		
		/// <summary>
		/// Generates a random network with a given number of nodes and a given expected number of edges
		/// </summary>
		/// <param name='n'>
		/// The number of nodes
		/// </param>
		/// <param name='expectedEdges'>
		/// The expected number of edges.
		/// </param>
        public ERNetwork(int n, int expectedEdges)
        {
            GenerateNetwork(n, expectedEdges / Math.Pow(n, 2d));
        }
        
		/// <summary>
		/// Generates a random network with a given number of nodes and a given edge probability
		/// </summary>
		/// <param name='n'>
		/// The number of nodes
		/// </param>
		/// <param name='p'>
		/// The probability with which an edge is created between a pair of nodes
		/// </param>
        public ERNetwork(int n, double p)
        {
            GenerateNetwork(n, p);
        }

        private void GenerateNetwork(int n, double p)
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
