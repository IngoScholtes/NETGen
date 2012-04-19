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
		
#pragma	warning disable 0414
		[Parameter(ParameterType.Output, "Some first output parameter", null)]
		double result1;
		
		[Parameter(ParameterType.Output, "Some second output parameter", null)]
		long result2;
#pragma	warning restore 0414
		
		[Parameter(ParameterType.OutputFile, "The name of an output file to write data to", null)]
		string output_file;
		
		public static void Main(string[] args) { Init(args); }
		
		
		public override void RunSimulation ()
		{
			// Do some simulation here and write results to the output parameters nothing else to do :-)
			// If you like you can write time series by using the WriteTimeSeries function of the DiscreteDynamics base class ... 
			
			Logger.AddMessage(LogEntryType.AppMsg, "Simulation was started with:");
			Logger.AddMessage(LogEntryType.AppMsg, "\t param1 \t= " + param1.ToString());
			Logger.AddMessage(LogEntryType.AppMsg, "\t param2 \t= " + param2.ToString());
			Logger.AddMessage(LogEntryType.AppMsg, "\t SomeChoice \t= " + SomeChoice.ToString());
			Logger.AddMessage(LogEntryType.AppMsg, "\t output_file \t= " + output_file);
			
			result1 = 42d;
			result2 = 42;
		}
	}
}

