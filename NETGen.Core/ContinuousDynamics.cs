using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Double;

namespace NETGen.Core
{	
	/// <summary>
	/// Represents a continous dynamical process given by a differential equation. Implementors of this class can be run synchronously and asynchronously.
	/// This is the point of extensibility for modules in NETGen.Dynamics.
	/// </summary>
	public abstract class ContinuousDynamics
	{			
		public SimulationRunState State = SimulationRunState.Idle;
		
		private List<string> _dataColumns;
		private ConcurrentDictionary<double, ConcurrentDictionary<string, double>> _timeSeries;
		
		/// <summary>
		/// The 
		/// </summary>
		/// <value>
		/// The time delta.
		/// </value>
		public double TimeDelta { get; protected set; }
		public double Time { get; private set; }		
		
		public DenseVector CurrentValues;
		
		protected ContinuousDynamics(double t0, DenseVector InitialValues)
		{ 
			Time = t0;
			CurrentValues = InitialValues;
		}
		
		protected abstract DenseVector ComputeDelta(double time, DenseVector val);
		protected virtual void Finish() {}		
		
		public delegate void StepHandler(double time);
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
		/// Synchronously runs the simulation and blocks until it finishes
		/// </summary>
		public void Run() {			
			_dataColumns = new List<string>();
			_timeSeries = new ConcurrentDictionary<double, ConcurrentDictionary<string,double>>();			
			State = SimulationRunState.Running;
			Logger.AddMessage(LogEntryType.Info, string.Format("Started continuous dynamics module [{0}]", this.GetType().Name));
			
			while(State == SimulationRunState.Running)
			{
				#if !DEBUG
				try{
				#endif
						// Compute change based on 4th-order Runge-Kutta and user-provided dynamics						
						DenseVector k1 = TimeDelta * ComputeDelta(Time, CurrentValues);
						DenseVector k2 = TimeDelta * ComputeDelta(Time+TimeDelta/2d, CurrentValues + k1/2d);
						DenseVector k3 = TimeDelta * ComputeDelta(Time+TimeDelta/2d, CurrentValues + k2/2d);
						DenseVector k4 = TimeDelta * ComputeDelta(Time+TimeDelta, CurrentValues + k3);
						CurrentValues += (k1 + 2d * k2 + 2d * k3 + k4) / 6d;
					
						// Advance time
						Time += TimeDelta;
						
						// Trigger update
						if (OnStep != null)
								OnStep(Time);
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
			
			if(!_timeSeries.ContainsKey(Time))
				_timeSeries[Time] = new ConcurrentDictionary<string, double>();
			
			_timeSeries[Time][column] = val;
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
				
				for(double t = 0d; t< Time; t+=TimeDelta)
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

