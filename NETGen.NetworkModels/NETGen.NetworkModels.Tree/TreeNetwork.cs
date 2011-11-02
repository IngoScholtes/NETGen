using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;

namespace NETGen.NetworkModels.Tree
{
	
	/// <summary>
	/// This class represents a simple k-nary tree network.
	/// </summary>
    public class TreeNetwork : Network
    {
	
		public Vertex RootVertex {get; private set; }
		
		/// <summary>
		/// Creates a simple and fully populated k-nary tree network
		/// </summary>
		/// <param name='k'>
		/// The number of children of each node
		/// </param>
		/// <param name='depth'>
		/// The depth of the tree
		/// </param>
        public TreeNetwork(int k, int depth, bool directed = false)
        {
            RootVertex = CreateVertex();
            BuildTree(RootVertex, k, depth, directed);
        }

        private void BuildTree(Vertex root, int k, int depth, bool directed)
		{
			if(depth == 0)
				return;
			
			for (int i=0; i<k; i++)
			{
				Vertex w = CreateVertex();
				CreateEdge(root, w, directed?EdgeType.DirectedAB:EdgeType.Undirected);
                BuildTree(w, k, depth - 1, directed); 		
			}                 
		}
    }
}
