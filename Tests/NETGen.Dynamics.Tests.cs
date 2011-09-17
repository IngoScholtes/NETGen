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
			EpidemicSync sync = new EpidemicSync(n);
			sync.WriteTimeSeries(null);
			sync.Stop();
		}
	}
}

