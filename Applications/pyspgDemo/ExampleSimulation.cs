using System;

using NETGen.Core;
using NETGen.pyspg;

namespace pyspgDemo
{
	public enum SomeDiscreteChoice { Choice1 = 0, Choice2 = 1};
	
	public class ExampleSimulation : pyspgSimulation<ExampleSimulation>
	{
		[Parameter(ParameterType.Input, "Some first input parameter", 0)]
		int param1;
		
		[Parameter(ParameterType.Input, "Some second input parameter", 42)]
		int param2;
		
		[Parameter(ParameterType.Input, "Some discrete choice", SomeDiscreteChoice.Choice1)]
		SomeDiscreteChoice SomeChoice;
		
		[Parameter(ParameterType.Output, "Some first output parameter")]
		double result1;
		
		[Parameter(ParameterType.Output, "Some second output parameter")]
		long result2;
		
		
		public static void Main(string[] args) { Init(args); }
		
		
		public override void RunSimulation ()
		{
			// Do some simulation here and write results to the output parameters nothing else to do :-)
			// If you like you can write time series by using the WriteTimeSeries function of the DiscreteDynamics base class ... 
			
			Logger.AddMessage(LogEntryType.AppMsg, "Simulation was started with:");
			Logger.AddMessage(LogEntryType.AppMsg, "\t param1 \t= " + param1.ToString());
			Logger.AddMessage(LogEntryType.AppMsg, "\t param2 \t= " + param2.ToString());
			Logger.AddMessage(LogEntryType.AppMsg, "\t SomeChoice \t= " + SomeChoice.ToString());
			
			result1 = 42d;
			result2 = 42;
		}
	}
}

