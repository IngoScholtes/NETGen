using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using GASS.CUDA;
using GASS.CUDA.Types;

using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layouts.HuForceDirected
{
	public class HuForceDirectedLayout : NETGen.Visualization.LayoutProvider
	{
		public bool CUDAEnabled = true;
		
		CUdevice dev;
		CUcontext ctx;
		CUmodule mod;
		CUDeviceProperties prop;
		
		int timesteps; 
		
		public HuForceDirectedLayout (int steps)
		{
			
			timesteps = steps;
#if !DEBUG
			try
			{
#endif
				CUDADriver.cuInit(0);
				
				dev = new CUdevice();
				CUDADriver.cuDeviceGet(ref dev, 0);
				
				ctx = new CUcontext();
				CUDADriver.cuCtxCreate(ref ctx, 0, dev);
				
				mod = new CUmodule();
				CUDADriver.cuModuleLoad(ref mod, "BarnesHut.cubin");				
				
				prop = new CUDeviceProperties();
				CUDADriver.cuDeviceGetProperties(ref prop, dev);
				int version = 0;
				CUDADriver.cuDriverGetVersion(ref version);
				
				string caps = "";
				
				GASS.CUDA.CUDARuntime.cudaRuntimeGetVersion(ref version);

				
				caps += "\tClock rate        = " + prop.clockRate/1000000 + " MHz\n";
				caps += "\tMemory size       = " + prop.totalConstantMemory/1024 + " KB\n";
				caps += "\tThreads per block = " + prop.maxThreadsPerBlock + "\n";
				caps += "\tWarp size         = " + prop.SIMDWidth + "\n";
				caps += "\tCUDA version      = " + version + "\n";
				
				Logger.AddMessage(LogEntryType.Info, "Successfully initialized CUDA GPU computation\n"+caps);
#if !DEBUG
			}
			catch(Exception ex)
			{
				Logger.AddMessage(LogEntryType.Warning, "CUDA not available, falling back to CPU. Exception was: " + ex.Message);
				CUDAEnabled = false;
			}			
#endif
		}
		
		public override void DoLayout()
		{
			CUdeviceptr p1 = new CUdeviceptr();
			
			CUDADriver.cuMemAlloc(ref p1, 1 <<10);
			byte[] b = new byte[1<<10];
			CUDADriver.cuMemcpyHtoD(p1, b, (uint) b.Length);
			
			CUfunction func = new CUfunction();	
			CUResult res;
			
			int nnodes = (int) Network.VertexCount*2;
			int blocks = 32;
			
			if (nnodes < 1024*blocks) nnodes = 1024*blocks;
    		while ((nnodes & (prop.SIMDWidth-1)) != 0) nnodes++;
    			nnodes--;
			
			//float dtime = 0.025f;  float dthf = dtime * 0.5f;
    		//float epssq = 0.05f * 0.05f;
    		//float itolsq = 1.0f / (0.5f * 0.5f);
			
			CUDADriver.cuModuleGetFunction(ref func, mod, "dummy");
			
			Float4[] data = new Float4[100];
			CUdeviceptr ptr = new CUdeviceptr();
			//CUDADriver.cuMemAlloc(ref ptr, (uint) 100 * System.Runtime.InteropServices.Marshal.SizeOf(Float4));
			CUDADriver.cuParamSeti(func, 0, (uint) ptr.Pointer);
			CUDADriver.cuParamSetSize(func, 4);
			
			res = CUDADriver.cuLaunch(func);			
			if(res != CUResult.Success)
				Logger.AddMessage(LogEntryType.Warning, "CUDA Error in dummy function: " +res.ToString());
			
			// InitializationKernel<<<1, 1>>>();
			CUDADriver.cuModuleGetFunction(ref func, mod, "InitializationKernel");
			res = CUDADriver.cuLaunch(func);			
			if(res != CUResult.Success)
				Logger.AddMessage(LogEntryType.Warning, "CUDA Error in InitializationKernel: " +res.ToString());
			
			// BoundingBoxKernel<<<blocks * FACTOR1, THREADS1>>>();			
			CUDADriver.cuModuleGetFunction(ref func, mod, "BoundingBoxKernel: "+res.ToString());
			CUDADriver.cuLaunch(func);			
			if(res != CUResult.Success)
				Logger.AddMessage(LogEntryType.Warning, "CUDA Error in BoundingBoxKernel: "+res.ToString());
			
			// TreeBuildingKernel<<<blocks * FACTOR2, THREADS2>>>();
			CUDADriver.cuModuleGetFunction(ref func, mod, "TreeBuildingKernel: "+res.ToString());
			CUDADriver.cuLaunch(func);
			if(res != CUResult.Success)
				Logger.AddMessage(LogEntryType.Warning, "CUDA Error in TreeBuildingKernel: "+res.ToString());
			
			// SummarizationKernel<<<blocks * FACTOR3, THREADS3>>>();			
			CUDADriver.cuModuleGetFunction(ref func, mod, "SummarizationKernel: "+res.ToString());
			CUDADriver.cuLaunch(func);
			if(res != CUResult.Success)
				Logger.AddMessage(LogEntryType.Warning, "CUDA Error in SummarizationKernel: "+res.ToString());
			
			// ForceCalculationKernel<<<blocks * FACTOR5, THREADS5>>>();
			CUDADriver.cuModuleGetFunction(ref func, mod, "ForceCalculationKernel: "+res.ToString());
			CUDADriver.cuLaunch(func);
			if(res != CUResult.Success)
				Logger.AddMessage(LogEntryType.Warning, "CUDA Error in ForceCalculationKernel: "+res.ToString());
			
			// IntegrationKernel<<<blocks * FACTOR6, THREADS6>>>();
			CUDADriver.cuModuleGetFunction(ref func, mod, "IntegrationKernel");
			CUDADriver.cuLaunch(func);
			if(res != CUResult.Success)
				Logger.AddMessage(LogEntryType.Warning, "CUDA Error in IntegrationKernel: "+res.ToString());
		}
		
		public override Vector3 GetPositionOfNode(Vertex v)
        {
            return new Vector3();
        }
	}
}

