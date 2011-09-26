using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using NETGen.Core;

namespace Scientometrics
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Dictionary<int, Vertex> _idMappings = new Dictionary<int, Vertex>(20000);
			
			if(args.Length<1)
			{
				Logger.AddMessage(LogEntryType.AppMsg, "Usage: Scientometrics [sample_file]");
				return;
			}
			DateTime start = DateTime.Now;
			string[] data = System.IO.File.ReadAllLines(args[0]);
			Logger.AddMessage(LogEntryType.AppMsg, string.Format("Read source file in {0} milliseconds.", (DateTime.Now-start).TotalMilliseconds));
			int count = 0;
			start = DateTime.Now;
			Network network = new Network();
			int currentid = 0;
			int referenceid = 0;
			foreach(string s in data) {
				
					if(s.StartsWith("T9"))
					{
						currentid = Int32.Parse(s.Substring(3));
						_idMappings[currentid] = network.CreateVertex();
						count++;
					}
					if(s.StartsWith("R9"))
					{
						referenceid = Int32.Parse(s.Substring(3));
						if(_idMappings.ContainsKey(referenceid))
						{
							//_idMappings[referenceid] = network.CreateVertex();							
							network.CreateEdge(_idMappings[currentid], _idMappings[referenceid]);
						}
					}
			}			
			Logger.AddMessage(LogEntryType.AppMsg, string.Format("Parsed data in {0} milliseconds.", (DateTime.Now-start).TotalMilliseconds));
			Logger.AddMessage(LogEntryType.AppMsg, string.Format("Found {0} articles and {1} citations in sample.", count, network.EdgeCount));
		}
	}
}
