using System;

namespace NETGen.Core
{
	public enum RunState { Idle = 0, Running = 2, Stopped = 3, Error = 4 };
	
	public abstract class DiscreteDynamics<ResultType>
	{			
		public RunState State = RunState.Idle;
		
		public long SimulationStep = 0;
		
		public abstract void Init();
		public abstract void Step();				
		public abstract ResultType Collect();
		
		public delegate void StepHandler(long step);
		public event StepHandler OnStep;
		
		public virtual void Finish() {}
		
		/// <summary>
		/// Stops a simulation
		/// </summary>
		public void Stop() {
			State = RunState.Stopped;
		}
		
		/// <summary>
		/// Synchronously runs the discrete simulation and blocks until it finishes
		/// </summary>
		public void Run() {
			Init();
			State = RunState.Running;
			while(State == RunState.Running)
			{
				try{
					Step();
					if(OnStep!=null)
						OnStep(SimulationStep);
					SimulationStep++;
				}
				catch(Exception)
				{
					State = RunState.Error;
					return;
				}
			}
			Finish();
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

