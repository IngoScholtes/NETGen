using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NETGen.Core;

namespace NETGen.NetworkModels.Tree
{
    public class TreeNetwork : Network
    {
        public TreeNetwork(int k, int depth)
        {
            Vertex root = CreateVertex();
            BuildTree(root, k, depth);
        }

        void BuildTree(Vertex root, int k, int depth)
		{
			if(depth == 0)
				return;
			
			for (int i=0; i<k; i++)
			{
				Vertex w = CreateVertex();
				CreateEdge(root, w);
                BuildTree(w, k, depth - 1); 		
			}                 
		}
    }
}
