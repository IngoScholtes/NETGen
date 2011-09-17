using System;
using System.Collections.Generic;
using NUnit.Framework;

using NETGen.Core;
using NETGen.NetworkModels.Cluster;

namespace NETGen.NetworkModels.Tests
{
	[TestFixture()]
	public class TestNetworkModels
	{
		[Test()]
		public void TestClusterNetwork ()
		{
			ClusterNetwork network = new ClusterNetwork(2000, 5000, 100, 0.9d, false); 
			Assert.LessOrEqual(Math.Abs(network.NewmanModularity - 0.9d), 0.02);
			Assert.LessOrEqual(Math.Abs(network.EdgeCount - 5000), 200);
			Assert.AreEqual(network.VertexCount, 2000);
			Assert.AreEqual(network.ClusterIDs.Length, 100);
			Assert.AreEqual(network.EdgeCount, network.InterClusterEdgeNumber + network.IntraClusterEdgeNumber);
			try{
				foreach(Edge e in network.IntraClusterEdges)
				{
					int id1 = network.GetClusterForNode(e.Source);
					int id2 = network.GetClusterForNode(e.Target);
					Assert.AreEqual(id1, id2);				
				}
				foreach(Edge e in network.InterClusterEdges)
				{
					int id1 = network.GetClusterForNode(e.Source);
					int id2 = network.GetClusterForNode(e.Target);
					Assert.AreNotEqual(id1, id2);		
				}
				foreach(Vertex v in network.Vertices)
				{
					int id = network.GetClusterForNode(v);
					Assert.LessOrEqual(id, network.ClusterIDs.Length);
				}
				List<Vertex> vertices = new List<Vertex>();
				foreach(int id in network.ClusterIDs)
				{
					vertices.AddRange(network.GetNodesInCluster(id)); 
				}
				Assert.AreEqual(vertices.Count, network.VertexCount);				
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}
	}
}

