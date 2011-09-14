using System;
using NUnit.Framework;

using NETGen.Core;
using NETGen.Layout.HuForceDirected;
using NETGen.Layout.FruchtermanReingold;
using NETGen.NetworkModels.ErdoesRenyi;

namespace LayoutTests
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
			HuForceDirectedLayout layout = new HuForceDirectedLayout();
			Assert.IsTrue(layout.CUDAEnabled);
			layout.DoLayout(2000d, 1000d, network);
			
			foreach(Vertex v in network.Vertices)
			{
				try {
					NETGen.Visualization.Vector3 pos = layout.GetPositionOfNode(v);
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
			layout.DoLayout(2000d, 1000d, network);
			System.Threading.Thread.Sleep(5000);
			Assert.IsTrue(layout.IsLaidout());
			foreach(Vertex v in network.Vertices)
			{
				try {
					NETGen.Visualization.Vector3 pos = layout.GetPositionOfNode(v);
				}
				catch(Exception ex)
				{
					Assert.Fail(ex.Message);
				}
			}
		}
	}
}

