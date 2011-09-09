using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;

namespace NETGen.NetworkModels.Cluster
{
    public class ClusterNetwork : NETGen.Core.Network
    {
        private Dictionary<int, List<Vertex>> _clusters;
        private Dictionary<Vertex, int> _clusterAssignment;

        public int InterClusterEdges { get; private set; }
        public int IntraClusterEdges { get; private set; }

        /// <summary>
        /// Generates a clustered network
        /// </summary>
        /// <param name="clusters"></param>
        /// <param name="clusterSize"></param>
        /// <param name="p_in"></param>
        public ClusterNetwork(int clusters, int clusterSize, double intra_p, double inter_p)
        {            
            _clusters = new Dictionary<int, List<Vertex>>();
            _clusterAssignment = new Dictionary<Vertex, int>();

            InterClusterEdges = 0;
            IntraClusterEdges = 0;

            // create a number of modules, each being an erdös/renyi network
            for (int i = 0; i < clusters; i++)
            {
                _clusters[i] = new List<Vertex>();

                for (int j = 0; j < clusterSize; j++)
                {
                    Vertex v = new Vertex(this);
                    v.Tag = i;
                    v.Label = "Node "+i+"/"+j;
                    _clusters[i].Add(v);
                    _clusterAssignment[v] = i;
                    AddVertex(v);
                }                      
            }

			// probe for edges for every distinct pair of vertices
            foreach(Vertex v in Vertices.ToArray())
                foreach (Vertex w in Vertices.ToArray())
                    if(v < w)
                    {
						//probability of edge creation for members of same clusters is intra_p
                        if (_clusterAssignment[v] == _clusterAssignment[w])
						{
                            if (NextRandomDouble() <= intra_p)
                                CreateEdge(v, w);
                        }
                        else // probability for members of different clusters is inter_p
						{
                            if (NextRandomDouble() <= inter_p)
                                CreateEdge(v, w);
						}
                    }                   
        }        
		
		public bool HasInterClusterConnection(Vertex v)
		{
			bool result = false;
			foreach(Vertex w in v.Neigbors)
				if(_clusterAssignment[v]!=_clusterAssignment[w])
					result = true;
			return result;
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

         public double NewmanModularity
         {
             get
             {
                 double Q = 0d;

                 // entry e[i,j] contains the fraction of edges linking vertices of community i to vertices of community j
                 double[,] e = new double[ClusterIDs.Length, ClusterIDs.Length];

                 // entry a[i] contains the fraction of edges that connect to community i
                 double[] a = new double[ClusterIDs.Length];

                 double edges = EdgeCount;

                 foreach (int i in ClusterIDs)
                 {
                     foreach (int j in ClusterIDs)
                     {
                         Vertex[] members2 = GetNodesInCluster(j);

                         double count_ij = 0d;

                         foreach (Vertex v in members2)
                             foreach (Vertex w in v.Neigbors)
                                 if (GetClusterForNode(w) == i)
                                     count_ij++;
                         e[i, j] = count_ij / (2d*edges);
                     }
                 }

                 foreach (int i in ClusterIDs)
                 {
                     a[i] = 0;
                     foreach (int j in ClusterIDs)
                         a[i] += e[i, j];
                 }
                 foreach (int i in ClusterIDs)
                     Q += e[i, i] - Math.Pow(a[i], 2d);
                 return Q;
             }
            
        }

    }
}
