using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NETGen.Core
{
	/// <summary>
	/// Represents a dynamical process that runs in discrete steps. Implementors of this class can be run synchronously and asynchronously.
	/// This is the point of extensibility for modules in NETGen.Dynamics.
	/// </summary>
	public abstract class DiscreteDynamics
	{			
		public SimulationRunState State = SimulationRunState.Idle;
		
		private List<string> _dataColumns;
		private ConcurrentDictionary<long, ConcurrentDictionary<string, double>> _timeSeries;
		
		/// <summary>
		/// The 
		/// </summary>
		/// <value>
		/// The time delta.
		/// </value>
		public long SimulationTime { get; private set; }								
		
		protected abstract void TimeStep(long time);	
		protected virtual void Finish() {}		
		protected virtual void Init() {}		
		
		public delegate void StepHandler(long time);
		public delegate void StopHandler();
		public event StepHandler OnStep;				
		public event StopHandler OnStop;	
		
		/// <summary>
		/// Stops a simulation
		/// </summary>
		public void Stop() {
			State = SimulationRunState.Stopped;
			if(OnStop!=null)
				OnStop();
		}
		
		/// <summary>
		/// Synchronously runs the discrete simulation and blocks until it finishes
		/// </summary>
		public void Run() {			
			_dataColumns = new List<string>();
			_timeSeries = new ConcurrentDictionary<long, ConcurrentDictionary<string,double>>();			
			Init();
			State = SimulationRunState.Running;
			Logger.AddMessage(LogEntryType.Info, string.Format("Started dynamics module [{0}]", this.GetType().Name));
			while(State == SimulationRunState.Running)
			{
				#if !DEBUG
				try{
				#endif
						TimeStep(SimulationTime);						
						
						if (OnStep != null)
								OnStep(SimulationTime);
					
						SimulationTime++;
				#if !DEBUG
				}
				catch(Exception ex)
				{
					State = SimulationRunState.Error;
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
			
			if(!_timeSeries.ContainsKey(SimulationTime))
				_timeSeries[SimulationTime] = new ConcurrentDictionary<string, double>();
			
			_timeSeries[SimulationTime][column] = val;
		}		
		
		
		public void WriteTimeSeries(string file)
		{
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");

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
				
				for(long t = 0; t< SimulationTime; t++)
				{
					if (_timeSeries.ContainsKey(t))
					{
						string line = t.ToString();
						foreach(string col in _dataColumns)
							line += string.Format("\t{0:0.0000}", _timeSeries[t][col], ci.NumberFormat);
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

