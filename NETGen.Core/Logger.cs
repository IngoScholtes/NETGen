using System;

namespace NETGen.Core
{
	public enum LogEntryType { Info = 0, Warning = 1, Error = 2, SimMsg =3 }; 
	
	public class Logger
	{
		public static bool ShowInfos = true;
		public static bool ShowWarnings = true;
		public static bool ShowErrors = true;
		public static bool ShowSimMsg = true;
		
		private Logger ()
		{
		}		
		
		public static void AddMessage(LogEntryType type, string message)
		{
			System.Diagnostics.StackFrame frame = new System.Diagnostics.StackTrace().GetFrame(1);
			if(type == LogEntryType.Info && ShowInfos)
				Console.WriteLine("[INFO]\t{0}\t[{1}]\t{2}", DateTime.Now.ToLongTimeString(), frame.GetMethod().Module.Name + "/" + frame.GetMethod().Name, message);
			if(type == LogEntryType.Warning && ShowWarnings)
				Console.WriteLine("[WARNING]\t{0}\t[{1}]\t{2}", DateTime.Now.ToLongTimeString(), frame.GetMethod().Module.Name + "/" + frame.GetMethod().Name, message);
			if(type == LogEntryType.Error && ShowErrors)
				Console.WriteLine("[ERROR]\t{0}\t[{1}]\t{2}", DateTime.Now.ToLongTimeString(), frame.GetMethod().Module.Name + "/" + frame.GetMethod().Name, message);			
			if(type == LogEntryType.SimMsg && ShowSimMsg)
				Console.WriteLine("[SIM]\t{0}\t[{1}]\t{2}", DateTime.Now.ToLongTimeString(), frame.GetMethod().Module.Name + "/" + frame.GetMethod().Name, message);						
		}
	}
}

