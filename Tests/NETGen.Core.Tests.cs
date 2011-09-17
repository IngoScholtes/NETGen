using System;
using NUnit.Framework;

using NETGen.Core;

namespace NETGen.Core.Tests
{
	[TestFixture()]
	public class NetworkTests
	{
		[Test()]
		public void LoggerTest ()
		{
			Logger.AddMessage(LogEntryType.AppMsg, null);
			Logger.AddMessage(LogEntryType.AppMsg, "");			
		}
		
		[Test()]
		public void DiscreteDynamicsTest()
		{
			
		}
	}
}

