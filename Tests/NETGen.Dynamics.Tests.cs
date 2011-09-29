using System;
using NUnit.Framework;

using NETGen.Core;
using NETGen.Dynamics.Synchronization;

namespace NETGen.Dynamics.Tests
{
	[TestFixture()]
	public class NETGen
	{
		[Test()]
		public void EpidemicSyncTest ()
		{
			Network n = new Network();
			Vertex a = n.CreateVertex();
			Vertex b = n.CreateVertex();
			n.CreateEdge(a, b);
			Kuramoto sync = new Kuramoto(n, 2d);
			sync.WriteTimeSeries(null);
			sync.Stop();
		}
	}
}

