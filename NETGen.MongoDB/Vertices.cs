using System;

namespace NETGen.MongoDB
{
	public class Vertices
	{
			public Guid ID { get; set ;}
			public string Label { get; set; }
			
			public Vertices(NETGen.Core.Vertex v)			
			{
				ID = v.ID;	
				Label = v.Label;
			}		
	}
}

