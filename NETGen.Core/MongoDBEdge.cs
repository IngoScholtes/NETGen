using System;

namespace NETGen.Core
{
	public class MongoDBEdge
	{
		public Guid ID { get; set;}
		public Guid Source {get; set; }
		public Guid Target { get; set; }
		
		public MongoDBEdge (Guid id, Guid source, Guid target)
		{
			ID = id;
			Source = source;
			Target = target;
		}
	}
}

