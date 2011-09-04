using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETGen.Visualization
{
	/// <summary>
	/// An interface that custom network layout mechanisms have to implement
	/// </summary>
    public interface ILayoutProvider
    {
		/// <summary>
		/// Returns the position of a vertex in the network
		/// </summary>
		/// <returns>
		/// The position of vertex v
		/// </returns>
		/// <param name='v'>
		/// The vertex for which the position shall be returned
		/// </param>
        Vector3 GetPositionOfNode(NETGen.Core.Vertex v);
		
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
        void DoLayout(double width, double height, NETGen.Core.Network n);
    }
}
