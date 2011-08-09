using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;

namespace NETGen.NetworkModels.Cluster
{
    public class ClusterNetwork : NETGen.Core.Network
    {
        private Dictionary<int, List<Vertex>> _clusters = new Dictionary<int, List<Vertex>>();
        private Dictionary<Vertex, int> _clusterAssignment = new Dictionary<Vertex, int>();

        public ClusterNetwork(int clusters, int clusterSize, int intraClusterEdges, int interClusterEdges)
        {            
            Dictionary<int, List<Vertex>> _clusters = new Dictionary<int, List<Vertex>>();
            Dictionary<Vertex, int> _clusterAssignment = new Dictionary<Vertex, int>();           

            // create a number of modules, each being an erdös/renyi network
            for (int i = 0; i < clusters; i++)
            {
                _clusters[i] = new List<Vertex>();

                for (int j = 0; j < clusterSize; j++)
                {
                    Vertex v = new Vertex(this);
                    v.Tag = i;
                    _clusters[i].Add(v);
                    _clusterAssignment[v] = i;
                    AddVertex(v);
                }

                // draw random edges within cluster
                for (int k = 0; k < intraClusterEdges; k++)
                {
                    Vertex v = _clusters[i].ElementAt<Vertex>(this.NextRandom(clusterSize));
                    Vertex w = _clusters[i].ElementAt<Vertex>(this.NextRandom(clusterSize));
                    Edge e = new Edge(v, w, this);                    
                    AddEdge(e);
                }
            }

            // interconnect clusters
            // draw random edges within clusters
            for (int k = 0; k < interClusterEdges; k++)
            {
                Vertex v = _clusters[NextRandom(clusters)].ElementAt<Vertex>(NextRandom(clusterSize));
                Vertex w = _clusters[NextRandom(clusters)].ElementAt<Vertex>(NextRandom(clusterSize));
                Edge e = new Edge(v, w, this);                
                AddEdge(e);
            }
        }

        public int[] ClusterIDs
        {
            get
            {
                return _clusters.Keys.ToArray();
            }
        }

        public int GetClusterForNode(Vertex v)
        {
            return _clusterAssignment[v];
        }

        public Vertex[] GetNodesInCluster(int clusterID)
        {
            return _clusters[clusterID].ToArray();
        }

        public int GetClusterSize(int clusterID)
        {
            return _clusters[clusterID].Count;
        }

    }
}
