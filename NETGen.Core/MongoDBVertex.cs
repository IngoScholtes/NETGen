using System;

namespace NETGen.Core
{
	public class MongoDBVertex
	{
			public Guid ID { get; set ;}
			
			public MongoDBVertex(Guid id)			
			{
				ID = id;		
			}		
	}
}

