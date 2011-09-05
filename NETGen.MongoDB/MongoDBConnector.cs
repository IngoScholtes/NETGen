using System;
using NETGen.Core;

namespace NETGen.MongoDB
{
	public class MongoDBConnector
	{
		public static void SaveToMongoDB(string connectionstring, Network net)
		{
			var db = Norm.Mongo.Create(connectionstring);
			var VertexColl = db.GetCollection<Vertices>();
			var EdgeColl = db.GetCollection<Edges>();
			foreach(Vertex v in net.Vertices)
				VertexColl.Save(new Vertices(v));
			foreach(Edge e in net.Edges)
				EdgeColl.Save(new Edges(e));
		}
		
		public static void SaveToMongoDB<TVertex,TEdge>(string connectionstring, Network net, Func<Vertex,Vertices> vertexmapper, Func<Edge,Edges> edgemapper)
		{
			var db = Norm.Mongo.Create(connectionstring);
			var VertexColl = db.GetCollection<Vertices>();
			var EdgeColl = db.GetCollection<Edges>();
			foreach(Vertex v in net.Vertices)
				VertexColl.Save(vertexmapper(v));
			foreach(Edge e in net.Edges)
				EdgeColl.Save(edgemapper(e));
		}
		
		public static Network LoadFromMongoDB(string connectionstring)
		{
			var db = Norm.Mongo.Create(connectionstring);
			var VertexColl = db.GetCollection<Vertices>();
			var EdgeColl = db.GetCollection<Edges>();
			return new NETGen.Core.Network();
		}	
	}
}

