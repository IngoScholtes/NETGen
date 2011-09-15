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
        public int InterClusterEdgeNumber { get; private set; }
		
		/// <summary>
		/// Gets the total number of edhes within clusters
		/// </summary>
		/// <value>
		/// The number of intra cluster edges.
		/// </value>
        public int IntraClusterEdgeNumber { get; private set; }
		
		public List<Edge> InterClusterEdges;
		public List<Edge> IntraClusterEdges;

        /// <summary>
        /// Generates a random network of given size and with given modularity
        /// </summary>
        /// <param name="nodes">The number of nodes in the network</param>
        /// <param name="edges">The number of edges in the network</param>
        /// <param name="clusters">The number of (equal-sized) clusters of the network</param>        
        /// <param name="modularity">The Newman modularity of the resulting network</param>
        public ClusterNetwork(int nodes, int edges, int clusters, double modularity, bool reduce_to_LCC = false)
        {            
            _clusters = new Dictionary<int, List<Vertex>>();
            _clusterAssignment = new Dictionary<Vertex, int>();
			
			IntraClusterEdges = new List<Edge>(edges);
			InterClusterEdges = new List<Edge>(edges);

            InterClusterEdgeNumber = 0;
            IntraClusterEdgeNumber = 0;
			
			// Compute the size of each (equally-sized) cluster
			int clusterSize = nodes / clusters;
			
			double p_i = 0;
			
			// From this we can compute the edge probability for pairs of nodes within the same community ...
			if(clusterSize >1)
				p_i =  modularity * edges / (clusters * Combinatorics.Combinations(clusterSize, 2)) + edges / Combinatorics.Combinations(nodes, 2); 
			
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
							{
	                                Edge e = CreateEdge(v, w);
									IntraClusterEdgeNumber++;
									IntraClusterEdges.Add(e);
							}
                        }
                        else // probability for members of different clusters is inter_p
						{
                            if (NextRandomDouble() <= p_e)
							{
                                Edge e = CreateEdge(v, w);
								InterClusterEdgeNumber++;
								InterClusterEdges.Add(e);
							}
						}
                    }
			
			if(reduce_to_LCC)
			{
				ReduceToLargestConnectedComponent();
				
				foreach(Vertex v in _clusterAssignment.Keys.ToArray())
					if(!ContainsVertex(v.ID))
					{
						_clusters.Remove(_clusterAssignment[v]);
						_clusterAssignment.Remove(v);
					}
			}
			
			Logger.AddMessage(LogEntryType.Info, string.Format("Created cluster network with N = {0}, M = {1}, Q = {2:0.00}", VertexCount, EdgeCount, NewmanModularity));
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
		/// Returns the expected modularity of a random network of given size
		/// </summary>
		/// <returns>
		/// The Newman modularity 
		/// </returns>
		/// <param name='nodes'>
		/// The number of nodes in the network
		/// </param>
		/// <param name='edges'>
		/// The number of edges in the network
		/// </param>
		/// <param name='clusters'>
		/// The number of clusters in the network
		/// </param>
		public static double GetRandomModularity(int nodes, int clusters)
		{
			int clustersize = nodes / clusters;
			return ((double)(clusters * Combinatorics.Combinations(clustersize, 2))) / (double) Combinatorics.Combinations(nodes, 2);
		}
		
		/// <summary>
		/// Returns the maximums modularity of a network that is expected to remain connected
		/// </summary>
		/// <returns>
		/// The Newman modularity 
		/// </returns>
		/// <param name='nodes'>
		/// The number of nodes in the network
		/// </param>
		/// <param name='edges'>
		/// The number of edges in the network
		/// </param>
		/// <param name='clusters'>
		/// The number of clusters in the network
		/// </param>
		public static double GetMaxConnectedModularity(int nodes, int edges, int clusters)
		{
			int clusterSize = nodes / clusters;
			
			// Critical number of inter-cluster edges that need to be added for the network to remain connected
			double minInterEdges = clusters * Math.Log(clusters) / 2d;
			double maxIntraEdges = edges - minInterEdges;
			
			double p_i = maxIntraEdges / (double) (clusters * Combinatorics.Combinations(clusterSize, 2));
				
			return p_i * clusters * Combinatorics.Combinations(clusterSize, 2) / (double) edges - GetRandomModularity(nodes, clusters); 			
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
				
				int max_id = int.MinValue;
				
				foreach(int i in ClusterIDs)
					max_id = Math.Max(i, max_id);

                 // entry e[i,j] will contain the fraction of edges linking vertices of community i to vertices of community j
                 double[,] e = new double[max_id+1, max_id+1];

                 // entry a[i] contains the fraction of edges that connect to community i
                 double[] a = new double[max_id+1];

                 double edges = EdgeCount;

                 foreach (int i in ClusterIDs)
                     foreach (int j in ClusterIDs)
                     {
                         Vertex[] members = GetNodesInCluster(j);
                         double count_ij = 0d;
                         foreach (Vertex v in members)
                             foreach (Vertex w in v.Neigbors)
                                 if (GetClusterForNode(w) == i)
                                     count_ij++;
                         e[i, j] = count_ij / (2d*edges);
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
