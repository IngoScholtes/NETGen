using System;
using NUnit.Framework;

using NETGen.Core;
using NETGen.Layouts.HuForceDirected;
using NETGen.Layouts.FruchtermanReingold;
using NETGen.NetworkModels.ErdoesRenyi;

namespace NETGen.Layout.Tests
{
	[TestFixture()]
	public class TestLayoutMechanisms
	{
		ERNetwork network;
		
		[SetUp()]
		public void Init()
		{
			network = new ERNetwork(500, 2000);
		}
		
		[Test()]
		public void TestHuForceDirected()
		{					
			HuForceDirectedLayout layout = new HuForceDirectedLayout(20);
			layout.Init(2000d, 1000d, network);
			Assert.IsTrue(layout.CUDAEnabled);
			layout.DoLayout();
			
			foreach(Vertex v in network.Vertices)
			{
				try {
					NETGen.Visualization.Vector3 pos = layout.GetPositionOfNode(v);
					Assert.AreNotEqual(pos, new NETGen.Visualization.Vector3());
				}
				catch(Exception ex)
				{
					Assert.Fail(ex.Message);
				}
			}
		}
		
		[Test()]
		public void TestFruchtermanReingold()
		{	
			FruchtermanReingoldLayout layout = new FruchtermanReingoldLayout(10);
			layout.Init(2000d, 1000d, network);
			layout.DoLayout();
			foreach(Vertex v in network.Vertices)
			{
				try {
					NETGen.Visualization.Vector3 pos = layout.GetPositionOfNode(v);
					Assert.AreNotEqual(pos, new NETGen.Visualization.Vector3());
				}
				catch(Exception ex)
				{
					Assert.Fail(ex.Message);
				}
			}
		}
	}
}

