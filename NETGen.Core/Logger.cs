using System;
using System.Runtime.CompilerServices;

namespace NETGen.Core
{
	public enum LogEntryType { Info = 0, Warning = 1, Error = 2, SimMsg = 3, AppMsg = 4 }; 
	
	public class Logger
	{
		public static bool ShowInfos = true;
		public static bool ShowWarnings = true;
		public static bool ShowErrors = true;
		public static bool ShowSimMsg = true;
		public static bool ShowAppMsg = true;
		
		private static int maxModuleLength = int.MinValue;
		
		private Logger ()
		{
		}		
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void AddMessage(LogEntryType type, string message)
		{
			string msg = "";
			string modulestring = "Unspecified";
			
			if(message!=null)
				msg = message;			
			
			System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
			System.Diagnostics.StackFrame frame = null;
			System.Reflection.MethodBase method = null;
			System.Reflection.Module module = null;
			
			if (trace!=null)
				frame = trace.GetFrame(2);
			if(frame!=null)
				method = frame.GetMethod();
			if(method!=null)
				module = method.Module;
			
			if(module!=null)
				modulestring = module.Name;
			if(method!=null)
				modulestring += "/" + method.Name;
			
			maxModuleLength = Math.Max(maxModuleLength, modulestring.Length);			
			modulestring = modulestring.PadRight(maxModuleLength, ' ');
			
			string output = string.Format("\t{0}\t[ {1} ]\t{2}", DateTime.Now.ToLongTimeString(), modulestring, msg);
			
			if(type == LogEntryType.Info && ShowInfos)
			{
				Console.WriteLine("[INFO]"+output);
			}
			if(type == LogEntryType.Warning && ShowWarnings)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("[WARNING]"+output);
			}
			if(type == LogEntryType.Error && ShowErrors)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("[ERROR]"+output);
			}
			if(type == LogEntryType.SimMsg && ShowSimMsg)
			{
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine("[SIM]"+output);
			}
			if(type == LogEntryType.AppMsg && ShowAppMsg)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("[APP]"+output);
			}
			Console.ForegroundColor = ConsoleColor.Gray;
		}
	}
}

