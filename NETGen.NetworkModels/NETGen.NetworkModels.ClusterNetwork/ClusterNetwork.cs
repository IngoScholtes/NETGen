using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;
using MathNet.Numerics;

namespace NETGen.NetworkModels.Cluster
{
	
	/// <summary>
	/// A class representing a random network with given size and modularity
	/// </summary>
    public class ClusterNetwork : NETGen.Core.Network
    {
		
		/// <summary>
		/// The cluster members 
		/// </summary>
        private Dictionary<int, List<Vertex>> _clusters;
		
		/// <summary>
		/// Assigns each Vertex its respective cluster
		/// </summary>
        private Dictionary<Vertex, int> _clusterAssignment;
		
		/// <summary>
		/// Gets the total number of edges between clusters
		/// </summary>
		/// <value>
		/// The number of inter cluster edges.
		/// </value>
        public int InterClusterEdges { get; private set; }
		
		/// <summary>
		/// Gets the total number of edhes within clusters
		/// </summary>
		/// <value>
		/// The number of intra cluster edges.
		/// </value>
        public int IntraClusterEdges { get; private set; }

        /// <summary>
        /// Generates a random network of given size and with given modularity
        /// </summary>
        /// <param name="nodes">The number of nodes in the network</param>
        /// <param name="edges">The number of edges in the network</param>
        /// <param name="clusters">The number of (equal-sized) clusters of the network</param>        
        /// <param name="modularity">The Newman modularity of the resulting network</param>
        public ClusterNetwork(int nodes, int edges, int clusters, double modularity)
        {            
            _clusters = new Dictionary<int, List<Vertex>>();
            _clusterAssignment = new Dictionary<Vertex, int>();

            InterClusterEdges = 0;
            IntraClusterEdges = 0;
			
			// Compute the size of each (equally-sized) cluster
			int clusterSize = nodes / clusters;
			
			// From this we can compute the edge probability for pairs of nodes within the same community ...
			double p_i =  modularity * edges / (clusters * Combinatorics.Combinations(clusterSize, 2)) + edges / Combinatorics.Combinations(nodes, 2); 
			
			// This allows us to compute p_e ...
            double p_e = (edges - clusters * MathNet.Numerics.Combinatorics.Combinations(clusterSize, 2) * p_i) / (Combinatorics.Combinations(clusters * clusterSize, 2) - clusters * MathNet.Numerics.Combinatorics.Combinations(clusterSize, 2));			

            // Create a number of modules, each being an erdös/renyi network
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

			// Probe for edges for every distinct pair of vertices
            foreach(Vertex v in Vertices.ToArray())
                foreach (Vertex w in Vertices.ToArray())
                    if(v < w)
                    {
						//probability of edge creation for members of same clusters is intra_p
                        if (_clusterAssignment[v] == _clusterAssignment[w])
						{
                            if (NextRandomDouble() <= p_i)
                                CreateEdge(v, w);
                        }
                        else // probability for members of different clusters is inter_p
						{
                            if (NextRandomDouble() <= p_e)
                                CreateEdge(v, w);
						}
                    }                   
        }        
		
		/// <summary>
		/// Returns whether a node has a link into another cluster
		/// </summary>
		/// <returns>
		/// <c>true</c> if the specified node has a link into another cluster; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='v'>
		/// The node to test
		/// </param>
		public bool HasInterClusterConnection(Vertex v)
		{
			bool result = false;
			foreach(Vertex w in v.Neigbors)
				if(_clusterAssignment[v]!=_clusterAssignment[w])
					result = true;
			return result;
		}
		
		
		/// <summary>
		/// Gets an array of cluster IDs
		/// </summary>
		/// <value>
		/// The cluster Ids.
		/// </value>
        public int[] ClusterIDs
        {
            get
            {
                return _clusters.Keys.ToArray();
            }
        }
		
		/// <summary>
		/// Gets the cluster ID for a particular node.
		/// </summary>
		/// <returns>
		/// The ID of the cluster this node is member of 
		/// </returns>
		/// <param name='v'>
		/// The node for which the cluster ID shall be returned
		/// </param>
        public int GetClusterForNode(Vertex v)
        {
            return _clusterAssignment[v];
        }
		
		/// <summary>
		/// Gets an array of nodes belonging to a given cluster
		/// </summary>
		/// <returns>
		/// The nodes in the specified cluster.
		/// </returns>
		/// <param name='clusterID'>
		/// The ID of the cluster
		/// </param>
        public Vertex[] GetNodesInCluster(int clusterID)
        {
            return _clusters[clusterID].ToArray();
        }
		
		/// <summary>
		/// Gets the size of a specified cluster.
		/// </summary>
		/// <returns>
		/// The number of nodes in the cluster
		/// </returns>
		/// <param name='clusterID'>
		/// The ID of the cluster
		/// </param>
        public int GetClusterSize(int clusterID)
        {
            return _clusters[clusterID].Count;
        }
		
		/// <summary>
		/// Returns the modularity Q of the network, as defined by Mark Newman et al. 
		/// </summary>
		/// <value>
		/// The modularity of the network between -1 and 1
		/// </value>
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
