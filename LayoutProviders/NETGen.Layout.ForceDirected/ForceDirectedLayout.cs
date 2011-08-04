using System;
using System.Linq;
using NETGen.Core;
using NETGen.Visualization;
using System.Collections.Generic;

namespace NETGen.Layout.ForceDirected
{
	public class ForceDirectedLayout : Dictionary<Vertex,Point3D>, ILayoutProvider
	{
		double _area = 0d;
		double _k = 0d;
		double _width = 0d;
		double _height = 0d;
		int _iterations = 0;		
		Network _n = null;
		
		public ForceDirectedLayout (int width, int height, int iterations, Network n)
		{
			_iterations = iterations;			
			_area = width * height;
			_width = width;
			_height = height;
			_k = Math.Sqrt(_area/(double) n.Vertices.Count());	
			_n = n;
			Random _rand = new Random();
			foreach(Vertex v in n.Vertices)
				this[v] = new Point3D(_rand.NextDouble() * width, _rand.NextDouble() * height, 0d);
		}
		
		public void DoLayout()
		{
			// Algorith according to Fruchterman Reingold... 
			Dictionary<Vertex, Point3D> disp = new Dictionary<Vertex, Point3D>();
			double t = _width/10d;
			for (int i=0; i<_iterations; i++)
			{
				foreach(Vertex v in _n.Vertices)
				{
					disp[v] = new Point3D(0d, 0d, 0d);
					foreach(Vertex u in Keys)
						if(v!=u)
						{
							Point3D delta = this[v] - this[u];
							disp[v] = disp[v] + (delta / Point3D.Length(delta)) * repulsion(Point3D.Length(delta));							
						}
				}
							
				foreach(Edge e in _n.Edges)
				{
					Vertex v = e.Source;
					Vertex w = e.Target;
					Point3D delta = this[v] - this[w];
					disp[v] = disp[v] - (delta / Point3D.Length(delta)) * attraction(Point3D.Length(delta));
					disp[w] = disp[w] - (delta / Point3D.Length(delta)) * attraction(Point3D.Length(delta));
				}
				foreach(Vertex v in _n.Vertices)
				{
					Point3D vPos = this[v] + (disp[v] / Point3D.Length(disp[v])) * Math.Min(Point3D.Length(disp[v]), t);
					vPos.X = Math.Min(_width / 2d, Math.Max(-_width/2d, this[v].X));
					vPos.Y = Math.Min(_height / 2d, Math.Max(-_height/2d, this[v].Y));
					this[v] = vPos;
				}
				t-=t/100d;
			}
		}
			
		double repulsion(double x)
		{
			return Math.Pow(_k, 2d) / x;
		}
		
		double attraction(double x)
		{
			return Math.Pow(x, 2d) / _k;
		}
		
		public Point3D GetPositionFromNode(NETGen.Core.Vertex v)
		{
			return this[v];
		}
	}
}

