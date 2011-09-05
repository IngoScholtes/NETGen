using System;

namespace NETGen.MongoDB
{
	public class Edges
	{
		public Guid ID { get; set;}
		public Guid Source {get; set; }
		public Guid Target { get; set; }
		
		public Edges (NETGen.Core.Edge e)
		{
			ID = e.ID;
			Source = e.Source.ID;
			Target = e.Target.ID;
		}
	}
}

