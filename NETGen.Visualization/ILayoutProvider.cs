using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace NETGen.Visualization
{
	/// <summary>
	/// An interface that custom network layout mechanisms have to implement. 
	/// This is the point of extensibility for the modules in NETGen.Layouts
	/// </summary>
    public abstract class LayoutProvider
    {
		protected double Width;
		protected double Height; 
		protected NETGen.Core.Network Network;
		
		/// <summary>
		/// Returns the position of a vertex in the network
		/// </summary>
		/// <returns>
		/// The position of vertex v
		/// </returns>
		/// <param name='v'>
		/// The vertex for which the position shall be returned
		/// </param>
        public abstract Vector3 GetPositionOfNode(NETGen.Core.Vertex v);
		
		public virtual void Init(double width, double height, NETGen.Core.Network network)
		{
			Width = width;
			Height = height;
			Network = network;
		}
		
		/// <summary>
		/// Computes all vertex positions of a network. This will be called whenever a layout has to be computed for the first time or whenever the recomputation of the layout is forced.
		/// </summary>
		/// <param name='width'>
		/// Width.
		/// </param>
		/// <param name='height'>
		/// Height.
		/// </param>
		/// <param name='n'>
		/// N.
		/// </param>
        public abstract void DoLayout();
		
		/// <summary>
		/// Asynchronously computes the layout.
		/// </summary>
		/// <param name='layoutCompleted'>
		/// An optional lambda expression that will be executed after the layout has been completed. 
		/// </param>
		public void DoLayoutAsync(Action layoutCompleted = null)
		{
			System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o) {
				DoLayout();
				if(layoutCompleted!=null)
					layoutCompleted();
			});
		}
    }
}
