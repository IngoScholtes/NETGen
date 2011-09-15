using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using GASS.CUDA;
using GASS.CUDA.Types;

using NETGen.Core;
using NETGen.Visualization;

namespace NETGen.Layouts.HuForceDirected
{
	public class HuForceDirectedLayout : NETGen.Visualization.ILayoutProvider
	{
		public bool CUDAEnabled = true;
		
		public HuForceDirectedLayout ()
		{
#if !DEBUG
			try
			{
#endif
				CUDADriver.cuInit(0);
				
				CUdevice dev = new CUdevice();
				CUDADriver.cuDeviceGet(ref dev, 0);
				
				CUcontext ctx = new CUcontext();
				CUDADriver.cuCtxCreate(ref ctx, 0, dev);
#if !DEBUG
			}
			catch(Exception)
			{
				Logger.AddMessage(LogEntryType.Warning, "CUDA not available, falling back to CPU");
				CUDAEnabled = false;
			}			
#endif
		}
		
		public void DoLayout(double width, double height, Network n)
		{
			
		}
		
		public Vector3 GetPositionOfNode(Vertex v)
        {
            return new Vector3();
        }
		
		public bool IsLaidout()
		{
			return false;
		}
	}
}

