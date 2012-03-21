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
			///Qd desired, Qf final
			Logger.AddMessage(LogEntryType.Info, string.Format("Created cluster network with N = {0}, M = {1}, Qd = {2:0.000}, Qf = {3:0.000}", VertexCount, EdgeCount, modularity, NewmanModularityUndirected));
        }     
		
		private ClusterNetwork()
		{
			_clusters = new Dictionary<int, List<Vertex>>();
            _clusterAssignment = new Dictionary<Vertex, int>();
			
			IntraClusterEdges = new List<Edge>();
			InterClusterEdges = new List<Edge>();

            InterClusterEdgeNumber = 0;
            IntraClusterEdgeNumber = 0;
			
			
		}
		
		/// <summary>
		/// Loads a network with known module structure
		/// </summary>
		/// <param name='file'>
		/// The name of the edgelist file with line format "source,target,source_module,target_module"
		/// </param>
		public static ClusterNetwork LoadNetwork(string file, bool directed = false)
		{			
			ClusterNetwork n = new ClusterNetwork();
			string[] lines = System.IO.File.ReadAllLines(file);
			Dictionary<string, int> moduleMapping = new Dictionary<string, int>();
			foreach(string e in lines)
			{
				string[] components = e.Split(',');
				Vertex v = null;
				Vertex w = null;
				// Create vertex v and assign module
				if (!n.ContainsVertex(components[0]))
				{
					v = n.CreateVertex(components[0]);
					if(!moduleMapping.ContainsKey(components[2]))
						moduleMapping[components[2]] = moduleMapping.Count;
					n._clusterAssignment[v] = moduleMapping[components[2]];
					if(!n._clusters.ContainsKey(n._clusterAssignment[v]))
						n._clusters[n._clusterAssignment[v]] = new List<Vertex>();
					n._clusters[n._clusterAssignment[v]].Add(v);
				}
				else
					v = n.SearchVertex(components[0]);

				if (!n.ContainsVertex(components[1]))
				{
					w = n.CreateVertex(components[1]);
					if(!moduleMapping.ContainsKey(components[3]))
						moduleMapping[components[3]] = moduleMapping.Count;
					n._clusterAssignment[w] = moduleMapping[components[3]];
					if(!n._clusters.ContainsKey(n._clusterAssignment[w]))
						n._clusters[n._clusterAssignment[w]] = new List<Vertex>();
					n._clusters[n._clusterAssignment[w]].Add(w);
				}
				else
					w = n.SearchVertex(components[1]);
				Edge edge = n.CreateEdge(v, w, directed?EdgeType.DirectedAB:EdgeType.Undirected);
				if(n._clusterAssignment[v]!=n._clusterAssignment[w])
					n.InterClusterEdges.Add(edge);
				else
					n.IntraClusterEdges.Add(edge);
			}
			return n;
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
		/// Gets a count of cluster IDs
		/// </summary>
		/// <value>
		/// The count of cluster Ids.
		/// </value>
        public int GetClustersCount
        {
            get
            {
                return _clusters.Keys.ToArray().Count();
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
		/// Resets the cluster IDs.
		/// </summary>
		/// <returns>
		/// Clean the list of clusters and respective nodes assigned to it. 
		/// </returns>
		/// <param name='m'>
		/// The dictionary for vertices and respective modules. 
		/// </param>
        public void ResetClusters(Dictionary<Vertex,int> module_assignment)
        {
			//clean cluster assignments
			for(int i=0; i<GetClustersCount; i++) 
				_clusters[i].Clear();
			_clusters.Clear();
			
			//shrink the clusterIDs if necessary
			Dictionary<int, int> moduleMapping = new Dictionary<int, int>();
			int new_module=0;
			foreach(Vertex v in module_assignment.Keys)
			{
				if(!moduleMapping.ContainsKey(module_assignment[v])) 
					moduleMapping[module_assignment[v]]=new_module++;
				SetClusterForNode(v, moduleMapping[module_assignment[v]]);
				
				if(!_clusters.ContainsKey(GetClusterForNode(v)))
					_clusters[GetClusterForNode(v)] = new List<Vertex>();
				_clusters[GetClusterForNode(v)].Add(v);
			}
			moduleMapping.Clear();
		}
		
		/// <summary>
		/// Sets the cluster ID for a particular node.
		/// </summary>
		/// <returns>
		/// The ID of the cluster this node is member of 
		/// </returns>
		/// <param name='v'>
		/// The node for which the cluster ID shall be returned
		/// </param>
        public int SetClusterForNode(Vertex v, int c)
        {
            return _clusterAssignment[v]=c;
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
		/// Gets the average node cluster size.
		/// </summary>
		/// <returns>
		/// The average number of nodes in a cluster of given size
		/// </returns>
        public double GetAverageNodeClusterSize
        {
			get
            {
				
				int N=GetClustersCount;
				double result=0d;
				int n_nodes=0;
				for(int i=0;i<N;i++)
				{
					result+=GetClusterSize(i)*GetClusterSize(i);
					n_nodes+=GetClusterSize(i);
				}
				return result/(double)n_nodes;
			}
        }
		
		/// <summary>
		/// Gets the standard deviation of node cluster size.
		/// </summary>
		/// <returns>
		/// The standard deviation for the number of nodes in a cluster of given size
		/// </returns>
        public double GetStandardDeviationNodeClusterSize
        {
			get
            {
				double av=GetAverageNodeClusterSize;
				int N=GetClustersCount;
				double result=0d;
				int n_nodes=0;
				for(int i=0;i<N;i++)
				{
					result+=Math.Pow((double)GetClusterSize(i)*GetClusterSize(i)-av,2.0d);
					n_nodes+=GetClusterSize(i);
				}
				return Math.Sqrt(result/(double)n_nodes);
			}
        }
		
		/// <summary>
		/// Gets the average cluster size.
		/// </summary>
		/// <returns>
		/// The average number of nodes per cluster
		/// </returns>
        public double GetAverageClusterSize
        {
			get
            {
				int N=GetClustersCount;
				double result=0d;
				for(int i=0;i<N;i++)
				{
					result+=GetClusterSize(i);
				}
				return result/(double)N;
			}
        }
		
		/// <summary>
		/// Gets the standard deviation of cluster size.
		/// </summary>
		/// <returns>
		/// The standard deviation for the number of nodes per cluster
		/// </returns>
        public double GetStandardDeviationClusterSize
        {
			get
            {
				double av=GetAverageClusterSize;
				int N=GetClustersCount;
				double result=0d;
				for(int i=0;i<N;i++)
				{
					result+=Math.Pow((double)GetClusterSize(i)-av,2.0d);
				}
				return Math.Sqrt(result/(double)N);
			}
        }
		
		/// <summary>
		/// Returns the modularity Q of a directed network, as defined by Mark Newman et al. 
		/// </summary>
		/// <value>
		/// The modularity of the network between -1 and 1
		/// </value>
         public double NewmanModularityDirected
         {
             get
             {
                double Q = 0d;
				
				int max_id = int.MinValue;
				
				foreach(int i in ClusterIDs)
					max_id = Math.Max(i, max_id);

                 // entry e[i,j] will contain the fraction of edges linking vertices of community i to vertices of community j
                 double[,] e = new double[max_id+1, max_id+1];

                 // entry a[i] contains the fraction of edges that connect to community i COLUMN
                 double[] a = new double[max_id+1];
				
				 // entry b[i] contains the fraction of edges that connect to community i ROW
                 double[] b = new double[max_id+1];

                 double edges = EdgeCount;
				
				 foreach (int i in ClusterIDs)
                 	foreach (int j in ClusterIDs)
                    	{
                        	e[i,j] = 0d;
                     	}
				
				 foreach(Edge edge in InterClusterEdges)
					{
						e[GetClusterForNode(edge.Source), GetClusterForNode(edge.Target)]++;	
					}
				
				 foreach(Edge edge in IntraClusterEdges)
					{
						e[GetClusterForNode(edge.Source), GetClusterForNode(edge.Target)]++;	
					}
				
                 foreach (int i in ClusterIDs)
                    foreach (int j in ClusterIDs)
                     {
                         e[i,j] /= edges;
                     }
               
				 foreach (int i in ClusterIDs)
	                 {
	                     a[i] = 0;
	                     foreach (int j in ClusterIDs)
	                         a[i] += e[i, j];
	                 }
						
				 foreach (int j in ClusterIDs)
	                 {
	                     b[j] = 0;
	                     foreach (int i in ClusterIDs)
	                         b[j] += e[i, j];
	                 }
				 
				 double sumF = 0.0;	
	             foreach (int i in ClusterIDs)
					 {	
			     		sumF += a[i]*b[i];
	                    Q    += e[i, i]-a[i]*b[i];
                     }	
				
			     if((1-sumF)==0) return 0;
				 else return Q/(1-sumF);
             }  
        }
		/// <summary>
		/// Returns the modularity Q of a undirected network, as defined by Mark Newman et al. 
		/// </summary>
		/// <value>
		/// The modularity of the network between -1 and 1
		/// </value>
         public double NewmanModularityUndirected
         {
             get
             {
                double Q = 0d;
				
				int max_id = int.MinValue;
				
				foreach(int i in ClusterIDs)
					max_id = Math.Max(i, max_id);

                 // entry e[i,j] will contain the fraction of edges linking vertices of community i to vertices of community j
                 double[,] e = new double[max_id+1, max_id+1];

                 // entry a[i] contains the fraction of edges that connect to community i COLUMN
                 double[] a = new double[max_id+1];
				
				 double edges = EdgeCount;
				
				 foreach (int i in ClusterIDs)
                 	foreach (int j in ClusterIDs)
                    	{
                        	e[i,j] = 0d;
                     	}
				
				 foreach(Edge edge in InterClusterEdges)
					{
						e[GetClusterForNode(edge.Source), GetClusterForNode(edge.Target)]++;
						e[GetClusterForNode(edge.Target), GetClusterForNode(edge.Source)]++;
					}
				
				 foreach(Edge edge in IntraClusterEdges)
					{
						e[GetClusterForNode(edge.Source), GetClusterForNode(edge.Target)]+=2;	
					}
				
                 foreach (int i in ClusterIDs)
                    foreach (int j in ClusterIDs)
                     {
                         e[i,j] /= 2d*edges;
                     }
               
				 foreach (int i in ClusterIDs)
	                 {
	                     a[i] = 0;
	                     foreach (int j in ClusterIDs)
	                         a[i] += e[i, j];
	                 }
						
				 double sumF = 0.0;	
	             foreach (int i in ClusterIDs)
					 {	
			     		sumF += a[i]*a[i];
	                    Q    += e[i, i]-a[i]*a[i];
                     }	
				
			     if((1-sumF)==0) return 0;
				 else return Q/(1-sumF);
             }  
        }
	}
}
