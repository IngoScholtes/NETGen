using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NETGen.Core
{
	public enum RunState { Idle = 0, Running = 2, Stopped = 3, Error = 4 };

	/// <summary>
	/// Represents a dynamical process that runs in discrete steps. Implementors of this class can be run synchronously and asynchronously.
	/// This is the point of extensibility for modules in NETGen.Dynamics.
	/// </summary>
	public abstract class DiscreteDynamics<ResultType>
	{			
		public RunState State = RunState.Idle;
		
		private List<string> _dataColumns;
		private ConcurrentDictionary<long, ConcurrentDictionary<string, double>> _timeSeries;
		
		public long SimulationStep { get; private set; }
		
		protected virtual void Init() {}
		protected abstract void Step();	
		protected virtual void Finish() {}
		
		public abstract ResultType Collect();
		
		public delegate void StepHandler(long step);
		public delegate void StopHandler();
		public event StepHandler OnStep;				
		public event StopHandler OnStop;				
		
		/// <summary>
		/// Stops a simulation
		/// </summary>
		public void Stop() {
			State = RunState.Stopped;
			if(OnStop!=null)
				OnStop();
		}
		
		/// <summary>
		/// Synchronously runs the discrete simulation and blocks until it finishes
		/// </summary>
		public void Run() {			
			_dataColumns = new List<string>();
			_timeSeries = new ConcurrentDictionary<long, ConcurrentDictionary<string,double>>();
			SimulationStep = 0;
			Init();
			State = RunState.Running;
			Logger.AddMessage(LogEntryType.Info, string.Format("Started dynamics module [{0}]", this.GetType().Name));
			while(State == RunState.Running)
			{
				#if !DEBUG
				try{
				#endif
					Step();
					if(OnStep!=null)
						OnStep(SimulationStep);
					SimulationStep++;
				#if !DEBUG
				}
				catch(Exception ex)
				{
					State = RunState.Error;
					Logger.AddMessage(LogEntryType.Error, "Exception in dynamics module at " + ex.Source + ", message = " + ex.Message + "\n" + ex.StackTrace);
					return;
				}
				#endif
			}
			Finish();
		}
		
		public void AddDataPoint(string column, double val)
		{
			if(!_dataColumns.Contains(column))
				_dataColumns.Add(column);
			
			if(!_timeSeries.ContainsKey(SimulationStep))
				_timeSeries[SimulationStep] = new ConcurrentDictionary<string, double>();
			
			_timeSeries[SimulationStep][column] = val;
		}		
		
		
		public void WriteTimeSeries(string file)
		{
			if(file==null)
			{
				Logger.AddMessage(LogEntryType.Error, "Could not write time-series: 'null' given as filename.");
				return;
			}
			if(_dataColumns==null)
			{
				Logger.AddMessage(LogEntryType.Error, "Could not write time-series: datacolumns not initialized.");
				return;
			}
			
			try{
				string header = "# Time"; 
				foreach(string col in _dataColumns)
					header+= "\t" + col;
				header += "\n";
				System.IO.File.WriteAllText(file, header);
				
				for(long t = 0; t< SimulationStep; t++)
				{
					if (_timeSeries.ContainsKey(t))
					{
						string line = t.ToString();
						foreach(string col in _dataColumns)
							line += string.Format("\t{0:0.0000}", _timeSeries[t][col]);
						line += "\n";
						System.IO.File.AppendAllText(file, line);
					}
				}
				Logger.AddMessage(LogEntryType.Info, "Time series written successfully");
			} catch(System.IO.IOException ex)
			{
				Logger.AddMessage(LogEntryType.Error, "Could not write time-series:" + ex.Message);
			}
			
			
		}
		
		/// <summary>
		/// Asynchronously runs the discrete simulation in the background
		/// </summary>
		public void RunInBackground() {
			System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback( delegate(object o) {
				Run();
			}));
		}
			
	}
}

