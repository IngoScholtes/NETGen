using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Xml;

namespace NETGen.Core
{
    /// <summary>
    /// This is NETGen's central class, representing a thread-safe network consisting of vertices and directed or undirected edges. This class is multi-thread-efficient and thread-safe,
    /// multiple concurrent read accesses are allowed, while concurrent read/write accesses are being synchronized. Please note, that an exception will be thrown when a write-access (like e.g.
    /// AddVertex, RemoveEdge, ... is nested inside a thread-safe iteration (like e.g. the Vertices and Edges iterators). If you need to do such an operation, 
    /// obtain a copy of the thread-safe iterator first. Please refer to the iterators for more information. The network is internally stored as a adjacency list. 
    /// This class is the base class of extensions in NETGen.NetworkModels
    /// </summary>
    public class Network
    {
        /// <summary>
        /// A delegate type that handles vertex add and remove events
        /// </summary>
        /// <param name="v"></param>
        public delegate void VertexUpdateHandler(Vertex v);

        /// <summary>
        /// A delegate type that handles edge add and remove events
        /// </summary>
        /// <param name="e"></param>
        public delegate void EdgeUpdateHandler(Edge e);

        /// <summary>
        /// A delegate type that handles update events of the graph
        /// </summary>
        /// <param name="g"></param>
        public delegate void GraphUpdateHandler(Network g);

        /// <summary>
        /// A reader/writer lock used to synchronize multi-thread efficient access to _edges and _vertices
        /// </summary>
        protected System.Threading.ReaderWriterLockSlim _rwl;

        /// <summary>
        /// This event is thrown as soon as a new vertex has been added to the graph
        /// </summary>
        public event VertexUpdateHandler OnVertexAdded;

        /// <summary>
        /// This event is thrown as soon as a vertex has been removed from the graph
        /// </summary>
        public event VertexUpdateHandler OnVertexRemoved;

        /// <summary>
        /// This event is thrown as soon as an edge has been added to the graph
        /// </summary>
        public event EdgeUpdateHandler OnEdgeAdded;

        /// <summary>
        /// This event will be thrown as soon as an edge has been removed from the graph
        /// </summary>
        public event EdgeUpdateHandler OnEdgeRemoved;

        /// <summary>
        /// The name displayed for this graph
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// This is a non-thread-safe dictionary that contains all vertices
        /// </summary>
        protected Dictionary<Guid, Vertex> _vertices;

        /// <summary>
        /// This is a non-thread-safe dictionary that contains a collection of vertices by vertex label
        /// </summary>
        protected Dictionary<string, Vertex> _vertexLabels;

        /// <summary>
        /// This is a non-thread-safe dictinary that contains all edges
        /// </summary>
        protected Dictionary<Guid, Edge> _edges;
		
		/// <summary>
		/// A random generator that is required to provide thread safe creation of random numbers
		/// </summary>
        private Random _random;

        /// <summary>
        /// Creates a new graph with its random generator being initialized with a given seed
        /// </summary>
        /// <param name="seed">the seed to init the random generator</param>
        public Network(int seed)
        {
            _random = new Random(seed);
            _edges = new Dictionary<Guid, Edge>();
            _vertices = new Dictionary<Guid, Vertex>();
            _vertexLabels = new Dictionary<string, Vertex>();
            _rwl = new System.Threading.ReaderWriterLockSlim(System.Threading.LockRecursionPolicy.SupportsRecursion);
        }

        /// <summary>
        /// Creates a new graph with its random generator being initialized with the current tick count
        /// </summary>
        public Network()
        {
            _random = new Random();
            _edges = new Dictionary<Guid, Edge>();
            _vertices = new Dictionary<Guid, Vertex>();
            _vertexLabels = new Dictionary<string, Vertex>();
            _rwl = new System.Threading.ReaderWriterLockSlim(System.Threading.LockRecursionPolicy.SupportsRecursion);
        }

        /// <summary>
        /// A thread-safe implementation that returns the next int based on the graph's underlying random generator. Important notice: 
        /// Using a Random random generator object is NOT thread-safe and potentially ends up corrupting the underlying seed. For this is reason 
        /// it is absolutely necessary to use these thread-safe wrappers!
        /// </summary>
        public int NextRandom(int min, int max) 
        {
            lock(_random) return _random.Next(min, max); 
        }

        /// <summary>
        /// A thread-safe implementation that returns the next int based on the graph's underlying random generator. Important notice: 
        /// Using a Random random generator object is NOT thread-safe and potentially ends up corrupting the underlying seed. For this is reason 
        /// it is absolutely necessary to use these thread-safe wrappers!
        /// </summary>
        public int NextRandom(int max)
        {
            lock (_random) return _random.Next(max);
        }

        /// <summary>
        /// A thread-safe implementation that returns the next double based on the graph's underlying random generator. Important notice: 
        /// Using a Random random generator object is NOT thread-safe and potentially ends up corrupting the underlying seed. For this is reason 
        /// it is absolutely necessary to use these thread-safe wrappers!
        /// </summary>
        public double NextRandomDouble()
        {
            lock (_random) return _random.NextDouble();
        }
		
		/// <summary>
		/// Loads a network from a GraphML file.
		/// </summary>
		/// <returns>
		/// A network
		/// </returns>
		/// <param name='path'>
		/// The path of the graphml file
		/// </param>
        public static Network LoadFromGraphML(string path)
        {
            if (path == null)
                return null;
            Network n = new Network();
            XmlTextReader reader = new XmlTextReader(path);
            Dictionary<string, Vertex> _idMapping = new Dictionary<string,Vertex>();
            while (reader.Read())
            {
                XmlNodeType nt = reader.NodeType;
                if (nt == XmlNodeType.Element)
                {
                    if (reader.LocalName == "node")
                    {
                        string id = reader.GetAttribute("id");
                        if (!_idMapping.ContainsKey(id))
                        {
                            _idMapping[id] = new Vertex(n);
                            n.AddVertex(_idMapping[id]);
                        }
                    }
                    else if (reader.LocalName == "edge")
                    {
                        string sourceID = reader.GetAttribute("source");
                        string targetID = reader.GetAttribute("target");
                        Edge newEdge = new Edge(_idMapping[sourceID], _idMapping[targetID], n, EdgeType.Undirected);
                        n.AddEdge(newEdge);
                    }
                }
            }          
            reader.Close();
            return n;
        }

        /// <summary>
        /// Loads a network from a textfile in which each edge is given by a line of whitespace- or comma-seperated strings representing two nodes
        /// </summary>
        /// <param name="path">
        /// The path of the textfile
        /// </param>
        /// <returns>
        /// The network
        /// </returns>
        public static Network LoadFromEdgeFile(string path)
        {
			// First read all lines
            string[] lines = System.IO.File.ReadAllLines(path);
						
            Network net = new Network();			
			
			// Then process them in parallel
            System.Threading.Tasks.Parallel.ForEach(lines, s =>
            {
                string[] vertices = s.Split(' ', '\t');
                if(vertices.Length>=2)
                {
                    Vertex v1 = net.SearchVertex(vertices[0]);
                    Vertex v2 = net.SearchVertex(vertices[1]);					
					
					// this needs to be atomic 
					lock(net)
					{
	                    if(v1 == null)
	                        v1 = net.CreateVertex(vertices[0]);
	                    if (v2 == null)
	                        v2 = net.CreateVertex(vertices[1]);
					}
                    Edge e = net.CreateEdge(v1, v2, EdgeType.Undirected);
					if (vertices.Length==3) {
						try {
							e.Weight = float.Parse(vertices[2]);
						}
						catch
						{
							Logger.AddMessage(LogEntryType.Warning, "Could not parse edge weight.");
						}
					}
                }
            });
            return net;
        } 
		
		public static void SaveToEdgeFile(Network n, string path)
        {
			System.IO.StreamWriter sw = System.IO.File.CreateText(path);
			foreach(Edge e in n.Edges)
				sw.WriteLine(e.Source.Label.Replace(" ", "_") + " " + e.Target.Label.Replace(" ", "_"));
            sw.Close();
		}
		
		static string ExtractNodeLabel (string s)
		{
			return s.Substring(s.IndexOf("(")+1, s.IndexOf(")") - s.IndexOf("(")-1);
		}
		
		static string ExtractSourceLabel (string s)
		{
			return s.Substring(s.IndexOf("(")+1, s.IndexOf(",")-s.IndexOf("(")-1);
		}

		static string ExtractTargetLabel (string s)
		{
			return s.Substring(s.IndexOf(",")+1, s.IndexOf(")")-s.IndexOf(",")-1);
		}
		
		public static Network LoadFromCXF(string filename)
		{
			string[] cxf = System.IO.File.ReadAllLines(filename);
			Network n = new Network();
			
			foreach(string s in cxf)
			{
				string type = s.Substring(0, s.IndexOf(":"));
				if(type=="node")
				{
					string label = ExtractNodeLabel(s);
					n.CreateVertex(label);
//					if(colorizer!= null && extractColor(s)!=Color.Empty)
					//	colorizer[v] = extractColor(s);
				}
				else if (type=="edge")
				{
					string sourceLabel = ExtractSourceLabel (s);
					string targetLabel = ExtractTargetLabel (s);
					n.CreateEdge(n.SearchVertex(sourceLabel), n.SearchVertex(targetLabel));
				}
			}
			return n;
		}

		/// <summary>
		/// Gets the number of vertices in the network
		/// </summary>
		/// <value>
		/// The number of vertices
		/// </value>
        public long VertexCount
        {
            get
            {
                return _vertices.Count;
            }
        }

		/// <summary>
		/// Gets the number of (directed and undirected) edges in the network
		/// </summary>
		/// <value>
		/// The number of edges
		/// </value>
        public long EdgeCount
        {
            get
            {
                return _edges.Count;
            }
        }

		/// <summary>
		/// Saves a network to a graphML file
		/// </summary>
		/// <param name='path'>
		/// The path of the file
		/// </param>
		/// <param name='n'>
		/// The network to save
		/// </param>
        public static void SaveToGraphML(string path, Network n)
        {
            if (path == null || n == null)
                return;

            Dictionary<Guid, long> _GuidMapping = new Dictionary<Guid, long>();

            XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8);

            writer.WriteStartDocument();

            writer.WriteStartElement("graphml", "http://graphml.graphdrawing.org/xmlns");

            writer.WriteStartElement("key");

            writer.WriteStartAttribute("id");
            writer.WriteValue("value0");
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("for");
            writer.WriteValue("node");
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("attr.name");
            writer.WriteValue("strength");
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("attr.type");
            writer.WriteValue("double");
            writer.WriteEndAttribute();

            writer.WriteEndElement();

            writer.WriteStartElement("graph");
            writer.WriteStartAttribute("id");
            writer.WriteValue("g");
            writer.WriteEndAttribute();
            writer.WriteStartAttribute("edgedefault");
            writer.WriteValue("undirected");
            writer.WriteEndAttribute();
            long counter = 0;

            foreach (Vertex v in n.Vertices)
            {
                writer.WriteStartElement("node");
                writer.WriteStartAttribute("id");
                _GuidMapping[v.ID] = counter++;
                writer.WriteValue("n" + _GuidMapping[v.ID].ToString());
                writer.WriteEndAttribute();
                writer.WriteStartElement("data");
                writer.WriteStartAttribute("key");
                writer.WriteValue("value0");
                writer.WriteEndAttribute();
                if (v.Tag != null)
                    writer.WriteValue(v.Tag);
                else
                    writer.WriteValue(0d);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            foreach (Edge e in n.Edges)
            {
                writer.WriteStartElement("edge");

                if (e.EdgeType == EdgeType.DirectedAB)
                {
                    writer.WriteStartAttribute("source");
                    writer.WriteValue("n" + _GuidMapping[e.Source.ID].ToString());
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute("target");
                    writer.WriteValue("n" + _GuidMapping[e.Target.ID].ToString());
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute("directed");
                    writer.WriteValue("true");
                    writer.WriteEndAttribute();
                }
                else if (e.EdgeType == EdgeType.DirectedBA)
                {
                    writer.WriteStartAttribute("source");
                    writer.WriteValue("n" + _GuidMapping[e.Target.ID].ToString());
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute("target");
                    writer.WriteValue("n" + _GuidMapping[e.Source.ID].ToString());
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute("directed");
                    writer.WriteValue("true");
                    writer.WriteEndAttribute();
                }
                else
                {
                    writer.WriteStartAttribute("source");
                    writer.WriteValue("n" + _GuidMapping[e.Source.ID].ToString());
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute("target");
                    writer.WriteValue("n" + _GuidMapping[e.Target.ID].ToString());
                    writer.WriteEndAttribute();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }		

        /// <summary>
        /// Adds a new vertex to the graph. This operation is thread-safe and asynchronously fires a OnVertexAdded event. Its complexity is in O(1).
        /// </summary>
        /// <returns>A reference to the newly created vertex</returns>
        public virtual Vertex CreateVertex()
        {
                Vertex newVertex = new Vertex(this);
                AddVertex(newVertex);               
                return newVertex;
        }

        /// <summary>
        /// Adds a new vertex to the graph. This operation is thread-safe and asynchronously fires a OnVertexAdded event. Its complexity is in O(1).
        /// </summary>
        /// <returns>A reference to the newly created vertex</returns>
        public virtual Vertex CreateVertex(string label)
        {
            Vertex newVertex = new Vertex(this, label);
            AddVertex(newVertex);
            return newVertex;
        }

        /// <summary>
        /// Adds a specific vertex to the graph. This is only available to derived classes. This operation is thread-safe and asynchronously 
        /// fires a OnVertexAdded event. Its complexity is in O(1).
        /// </summary>
        /// <param name="v"></param>
        public virtual void AddVertex(Vertex v)
        {
            if (v.Network != this)
                throw new Exception("Error. A vertex created for a different network cannot be added this network!");

            if(_vertices.ContainsKey(v.ID))
                throw new Exception("Error. A vertex with this ID has already been added to this network!");

            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            if (!_rwl.IsWriteLockHeld)
            {
                _rwl.EnterWriteLock();
                
                _vertices.Add(v.ID, v);
                if (!_vertexLabels.ContainsKey(v.Label))
                    _vertexLabels[v.Label] = v;                
                _rwl.ExitWriteLock();
            }
            else
            {
                _vertices.Add(v.ID, v);

                if (!_vertexLabels.ContainsKey(v.Label))
                    _vertexLabels[v.Label] = v;
            }

            if (OnVertexAdded != null)
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(delegate
                {
                    OnVertexAdded(v);
                }), null);               
        }

        /// <summary>
        /// Creates a new edge ands adds it to the graph. Throws an exception in case either source or target are nonexistent, identical or the edge already exists. 
        /// This operation is thread-safe and asynchronously fires a OnVertexAdded event. Its complexity is in O(1).
        /// </summary>
        /// <param name="source">The source vertex</param>
        /// <param name="target">The target vertex</param>
        /// <returns>A reference to the newly created edge</returns>
        public virtual Edge CreateEdge(Vertex source, Vertex target)
        {
            return CreateEdge(source, target, EdgeType.Undirected);
        }

        /// <summary>
        /// Creates a new edge ands adds it to the graph. Throws an exception in case either source or target are nonexistent, identical or the edge already exists. 
        /// This operation is thread-safe and asynchronously fires a OnVertexAdded event. Its complexity is in O(1).
        /// </summary>
        /// <param name="source">The source vertex</param>
        /// <param name="target">The target vertex</param>
        /// <param name="type">Whether the newly created edge is directed or undirected</param>
        /// <returns>A reference to the newly created edge</returns>
        public Edge CreateEdge(Vertex source, Vertex target, EdgeType type)
        {
            Edge newEdge = new Edge(source, target, this, type);
            AddEdge(newEdge);
            return newEdge;
        }

        /// <summary>
        /// Checks whether two vertices are in the same connected component. This operation is thread-safe. Its complexity is in O(n+m) with n being the number of 
        /// vertices and m being the number of edges.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <returns>Whether v and w are in the same connected component, i.e. whether they are connected</returns>
        public bool InSameComponent(Vertex v, Vertex w)
        {
            _rwl.EnterReadLock();
            Stack<Guid> stack = new Stack<Guid>();
            List<Guid> visited = new List<Guid>();
            stack.Push(v.ID);
            Vertex x;
            while (stack.Count>0)
            {
                x = _vertices[stack.Pop()];
                visited.Add(x.ID);
                foreach (Vertex s in x.Neigbors)
                    if(!visited.Contains(s.ID))
                        stack.Push(s.ID);
            }
            _rwl.ExitReadLock();
            return visited.Contains(w.ID);
        }

        /// <summary>
        /// Checks whether the graph is connected. This operation is thread-safe. Its complexity is in O(n+m) with n being the number of 
        /// vertices and m being the number of edges.
        /// </summary>
        /// <returns>Whether the graph is connected</returns>
        public bool Connected
        {
            get
            {
                _rwl.EnterReadLock();
                Stack<Guid> stack = new Stack<Guid>();
                List<Guid> visited = new List<Guid>();

                if (_vertices.Count == 0)
                {
                    _rwl.ExitReadLock();
                    return false;
                }

                stack.Push(RandomVertex.ID);
                Vertex x;
                while (stack.Count > 0)
                {
                    x = _vertices[stack.Pop()];
                    visited.Add(x.ID);
                    foreach (Vertex s in x.Neigbors)
                        if (!visited.Contains(s.ID) && !stack.Contains(s.ID))
                            stack.Push(s.ID);
                }
                _rwl.ExitReadLock();
                return visited.Count == _vertices.Count;
            }
        }

        /// <summary>
        /// A list of vertices that belong to the largest connected component of this network
        /// </summary>
        public List<Vertex> LargestConnectedComponent
        {
            get
            {
                _rwl.EnterReadLock();

                // collect all unvisited vertices
                List<Vertex> unvisited = new List<Vertex>(_vertices.Values.ToArray());

                // collect all components of the network
                Dictionary<Vertex, List<Vertex>> components = new Dictionary<Vertex, List<Vertex>>();

                // the largest component and the vertex from where it was explored ... 
                int componentsize = int.MinValue;
                Vertex maxComponentHead = null;

                // iterate through all components of the network
                while (unvisited.Count > 0)
                {
                    // select a random unvisited vertex
                    Vertex v = unvisited.ElementAt(NextRandom(unvisited.Count));
                    unvisited.Remove(v);

                    // visit all vertices starting from that node
                    Stack<Guid> stack = new Stack<Guid>();
                    List<Vertex> component = new List<Vertex>();

                    stack.Push(v.ID);
                    Vertex x;
                    while (stack.Count > 0)
                    {
                        x = _vertices[stack.Pop()];
                        component.Add(x);
                        unvisited.Remove(x);
                        foreach (Vertex s in x.Neigbors)
                            if (!component.Contains(s) && !stack.Contains(s.ID))
                                stack.Push(s.ID);
                    }

                    // store this component and check whether it is the largest
                    components[v] = component;
                    if (component.Count > componentsize)
                    {
                        maxComponentHead = v;
                        componentsize = component.Count;
                    }
                }

                _rwl.ExitReadLock();

                return components[maxComponentHead];
            }
        }

        /// <summary>
        /// Removes all vertices and edges that do not belong to the largest connected component of this network
        /// </summary>
        public void ReduceToLargestConnectedComponent()
        {
            List<Vertex> lcc = LargestConnectedComponent;

            // remove all vertices not belonging to the largest component
            foreach (Vertex w in Vertices.ToArray())
                if (!lcc.Contains(w))
                    RemoveVertex(w);
        }

        /// <summary>
        /// Adds a directed or undirected edge to a graph, registering the edge with the source and the target vertex. This will only succeed if and only if
        /// at the time when this method obtains a writer lock on the graph both vertices exist, if the edge is not a loop and if the edge 
        /// does not yet exist. This operation is thread-safe and throws an
        /// OnEdgeAdded event if the edge has been added. Its complexity is in O(1).
        /// </summary>
        /// <param name="e">The edge to add to the graph</param>
        /// <returns>Whether or not the edge has been added to the graph</returns>
        public virtual bool AddEdge(Edge e)
        {
            // nothing to do here
            if (e == null || e.Source == e.Target)
                return false;

            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            if(!_rwl.IsWriteLockHeld)
                _rwl.EnterWriteLock();

            // One of the vertices does not exist
            if (!_vertices.ContainsKey(e.A.ID) || !_vertices.ContainsKey(e.B.ID))
            {
                _rwl.ExitWriteLock();
                return false;
            }
            // an undirected edge exists already, nothing to add here
            else if(e.Source.IsSuccessor(e.Target) && e.Target.IsSuccessor(e.Source))
            {
                _rwl.ExitWriteLock();
                return false;
            }
            // a directed edge shall be added but it exists already
            else if (e.EdgeType != EdgeType.Undirected && e.Source.IsSuccessor(e.Target))
            {
                _rwl.ExitWriteLock();
                return false;
            }
            // the reverse direction exists, so in any case (whether the added edge is directed or undirected) the edge will become undirected
            else if (e.Target.IsSuccessor(e.Source))
            {
                Edge existing = e.Target.GetEdgeToSuccessor(e.Source);
                existing.EdgeType = EdgeType.Undirected;
                e.Target.ChangeEdgeRegistration(existing);
                e.Source.ChangeEdgeRegistration(existing);
                _rwl.ExitWriteLock();
                return true;
            }
            // an undirected edge shall be added and the directed edge exists already, so it becomes undirected
            else if (e.Source.IsSuccessor(e.Target) && e.EdgeType==EdgeType.Undirected)
            {
                Edge existing = e.Source.GetEdgeToSuccessor(e.Target);
                existing.EdgeType = EdgeType.Undirected;
                e.Target.ChangeEdgeRegistration(existing);
                e.Source.ChangeEdgeRegistration(existing);
                _rwl.ExitWriteLock();
                return true;
            }
                // no corresponding directed or undirected edge exists
            else if (!e.Source.IsSuccessor(e.Target) && !e.Target.IsSuccessor(e.Source))
            {
                _edges.Add(e.ID, e);

                e.Source.RegisterEdge(e);
                e.Target.RegisterEdge(e);
                _rwl.ExitWriteLock();

                if (OnEdgeAdded != null)
                        OnEdgeAdded(e);
                return true;
            }
            else 
            throw new Exception("This should never happen");
        }

        /// <summary>
        /// Removes an undirected or directed edge from the graph. This operation is thread-safe. Its complexity is in O(1).
        /// </summary>
        /// <param name="e">The edge to remove</param>
        public void RemoveEdge(Edge e)
        {
            if (e != null)
            {
                if (e.EdgeType == EdgeType.Undirected)
                    RemoveEdge(e.A, e.B);
                else
                    RemoveDirectedEdge(e.Source, e.Target);
            }
        }

        /// <summary>
        /// Removes a list of undirected or directed edges from the graph. This operation is thread-safe. Its complexity is in O(k) with k being the number of edges to remove.
        /// </summary>
        /// <param name="edges">The edge to remove</param>
        public void RemoveEdges(IList<Edge> edges)
        {
            for(int i=0; i<edges.Count; i++)
                RemoveEdge(edges[i]);
        }


         /// <summary>
        /// Removes the directed edge between two vertices. This operation is thread-safe. Its complexity is in O(1). If an edge 
        /// is removed this operation will fire an OnEdgeRemoved event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public void RemoveDirectedEdge(Vertex source, Vertex target)
        {
            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            _rwl.EnterWriteLock();

            Edge toRemove = source.GetEdgeToSuccessor(target);

            // if such an edge exists
            if (toRemove != null && _edges.ContainsKey(toRemove.ID))
            {
                // an undirected edge becomes directed
                if (toRemove.EdgeType == EdgeType.Undirected && toRemove.A == source && toRemove.B == target)
                {
                    toRemove.EdgeType = EdgeType.DirectedBA;
                    toRemove.A.ChangeEdgeRegistration(toRemove);
                    toRemove.B.ChangeEdgeRegistration(toRemove);
                }
                // an undirected edge becomes directed
                else if (toRemove.EdgeType == EdgeType.Undirected && toRemove.B == source && toRemove.A == target)
                {
                    toRemove.EdgeType = EdgeType.DirectedAB;
                    toRemove.A.ChangeEdgeRegistration(toRemove);
                    toRemove.B.ChangeEdgeRegistration(toRemove);
                }
                // a directed edge is removed
                else if (toRemove.Source == source && toRemove.Target == target)
                {
                    _edges.Remove(toRemove.ID);
                    toRemove.A.UnregisterEdge(toRemove);
                    toRemove.B.UnregisterEdge(toRemove);
                }
            }

            _rwl.ExitWriteLock();

            if (OnEdgeRemoved != null && toRemove!=null)
                OnEdgeRemoved(toRemove);
        }

        /// <summary>
        /// Removes any directed or undirected edge between two vertices. This operation is thread-safe. Its complexity is in O(1). If an edge 
        /// is removed this operation will fire an OnEdgeRemoved event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public void RemoveEdge(Vertex source, Vertex target)
        {
            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            _rwl.EnterWriteLock();

            Edge toRemove = source.GetEdgeToSuccessor(target);

            // Remove edge if it exists
            if (toRemove != null && _edges.ContainsKey(toRemove.ID))
            {
                _edges.Remove(toRemove.ID);

                toRemove.Source.UnregisterEdge(toRemove);
                toRemove.Target.UnregisterEdge(toRemove);
            }

            _rwl.ExitWriteLock();
        }

        /// <summary>
        /// Removes a vertex and all incident edges from the graph. This operation is thread-safe. Its complexity is in O(1+deg(v)). If vertices 
        /// (and incident edges) are removed this operation will fire OnEdgeRemoved and OnVertexRemoved events.
        /// </summary>
        /// <param name="v">The vertex to remove</param>
        public void RemoveVertex(Vertex v)
        {
            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            _rwl.EnterWriteLock();

            List<Vertex> _verticesToRemove = new List<Vertex>();

            if (_vertices.ContainsKey(v.ID))
            {
                _vertices.Remove(v.ID);
                _vertexLabels.Remove(v.Label);
                _verticesToRemove.Add(v);
            }
            List<Edge> _toRemove = new List<Edge>();
            foreach (Edge e in v.Edges)
                _toRemove.Add(e);

            foreach (Edge e in _toRemove)
            {
                if (_edges.ContainsKey(e.ID))
                    _edges.Remove(e.ID);               
                    e.A.UnregisterEdge(e);
                    e.B.UnregisterEdge(e);
            }

            _rwl.ExitWriteLock();

            if (OnEdgeRemoved != null)
                foreach (Edge e in _toRemove)
                       OnEdgeRemoved(e);
            if (OnVertexRemoved != null)
                foreach (Vertex x in _verticesToRemove)
                    OnVertexRemoved(x);
        }

        /// <summary>
        /// Removes all vertices and edges.  This operation is thread-safe. Its complexity is in O(1).
        /// </summary>
        public void Clear()
        {
            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            _rwl.EnterWriteLock();

            _vertices.Clear();
            _vertexLabels.Clear();
            _edges.Clear();

            _rwl.ExitWriteLock();
        }

        /// <summary>
        /// Removes all edges (directed and undirected) from the graph. This operation is thread-safe. Its complexity is in O(m) with m being the number of edges.
        /// </summary>
        public void ClearEdges()
        {
            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            _rwl.EnterWriteLock();

            foreach (Edge e in _edges.Values)
            {
                e.A.UnregisterEdge(e);
                e.B.UnregisterEdge(e);
            }
            _edges.Clear();

            _rwl.ExitWriteLock();
        }

        /// <summary>
        /// An enumeration of all vertices. Iterating through this is thread-safe, all changes to the graph asynchronously made by other threads will be blocked until enumeration has finished. 
        /// It is however not allowed for the thread that iterates through vertices to change the network, in which case a synchronization exception will be thrown. 
        /// If you need to do change the network while iterating, you can circumvent this behavior 
        /// by iterating through vertices like this foreach(Vertex v in Graph.Vertices.ToArray()). This forces immediate evaluation 
        /// of the iterator. However if used in this way just like storing the contents of the enumeration externally (e.g. in a list, etc.)
        /// it may happen that a vertex does not exist anymore once the enumeration has been completed. Also, new vertices might have been added by other threads in the meantime.
        /// </summary>
        public IEnumerable<Vertex> Vertices
        {
            get
            {
                _rwl.EnterReadLock();

                foreach (Vertex v in _vertices.Values)
                    yield return v;

                _rwl.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the vertex with a particular label. This operation is thread-safe. Its complexity is in O(1).
        /// </summary>
        /// <param name="cls">The class identifier</param>
        /// <returns></returns>
        public Vertex SearchVertex(string label)
        {
            Vertex v = null;
            _rwl.EnterReadLock();

            if (_vertexLabels.ContainsKey(label))
                v = _vertexLabels[label];
                
            _rwl.ExitReadLock();
            return v;
        }

        /// <summary>
        /// Returns an enumeration of vertex class. This operation is thread-safe.
        /// </summary>
        public IEnumerable<string> VertexLabels
        {
            get
            {
                _rwl.EnterReadLock();

                foreach (string s in _vertexLabels.Keys)
                    yield return s;

                _rwl.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns whether the graph contains a certain vertex
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsVertex(Guid id)
        {
            bool res = false;

            _rwl.EnterReadLock();   
         
            res = _vertices.ContainsKey(id);

            _rwl.ExitReadLock();
            return res;
        }

        /// <summary>
        /// Returns whether the graph contains a certain vertex
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsVertex(string label)
        {
            bool res = false;

            _rwl.EnterReadLock();

            res = _vertexLabels.ContainsKey(label);

            _rwl.ExitReadLock();
            return res;
        }

        /// <summary>
        /// An enumeration of all edges (directed and undirected). Iterating through this is thread-safe, all asynchronous changes to the graph will be blocked until enumeration has finished. 
        /// It is however not allowed to modify the graph while iterating through edges. Furthermore, when this enumeration is stored externally (e.g. in a list, etc.)
        /// it obviously may happen that an edge does not exist anymore once the enumeration has been completed.
        /// </summary>        
        public IEnumerable<Edge> Edges
        {
            get
            {
                _rwl.EnterReadLock();

                foreach (Edge e in _edges.Values)
                    yield return e;

                _rwl.ExitReadLock();
            }
        }

        /// <summary>
        /// An enumeration of all directed edges. Iterating through this is thread-safe, all asynchronous changes to the graph will be blocked until enumeration has finished. 
        /// It is however not allowed to modify the graph while iterating through edges. Furthermore, when this enumeration is stored externally (e.g. in a list, etc.)
        /// it obviously may happen that an edge does not exist anymore once the enumeration has been completed.
        /// </summary>        
        public IEnumerable<Edge> DirectedEdges
        {
            get
            {
                _rwl.EnterReadLock();

                foreach (Edge e in _edges.Values)
                    if(e.EdgeType!=EdgeType.Undirected)
                        yield return e;

                _rwl.ExitReadLock();
            }
        }

        /// <summary>
        /// An enumeration of all undirected edges. Iterating through this is thread-safe, all asynchronous changes to the graph will be blocked until enumeration has finished. 
        /// It is however not allowed to modify the graph while iterating through edges. Furthermore, when this enumeration is stored externally (e.g. in a list, etc.)
        /// it obviously may happen that an edge does not exist anymore once the enumeration has been completed.
        /// </summary>        
        public IEnumerable<Edge> UndirectedEdges
        {
            get
            {
                _rwl.EnterReadLock();

                foreach (Edge e in _edges.Values)
                    if (e.EdgeType == EdgeType.Undirected)
                        yield return e;

                _rwl.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns a random vertex from the graph. This operation is thread-safe. Its complexity is in O(1).
        /// </summary>
        public Vertex RandomVertex { 
            get 
            {
                _rwl.EnterReadLock();


                Vertex v = null;
                
                if( _vertices.Count>0)
                    v = _vertices.Values.ElementAt<Vertex>(NextRandom(_vertices.Count));

                _rwl.ExitReadLock();
                return v;

            } 
        }

        /// <summary>
        /// Returns a random vertex other than the vertex specified. This method is particularly useful if a vertex in a graph needs to find a random
        /// other vertex. This operation is thread-safe. Its complexity is in O(1).
        /// </summary>
        /// <param name="v">The vertex to exclude from the set from which random vertices are selected</param>
        /// <returns>A random vertex other than v or null if no such vertex exists</returns>
        public Vertex RandomVertexOtherThan(Vertex v)
        {
            _rwl.EnterReadLock();

            int vIx = _vertices.Keys.ToList<Guid>().IndexOf(v.ID);
            int rIx = NextRandom(_vertices.Count - 1);

            Vertex w = null;

            if (rIx >= vIx && rIx+1 <_vertices.Count)
            {
                w = _vertices.Values.ElementAt<Vertex>(rIx + 1);
            }
            else if(rIx<vIx)             
                w = _vertices.Values.ElementAt<Vertex>(rIx);  

            _rwl.ExitReadLock();
            return w;
        }

        /// <summary>
        /// Returns a random edge from the graph. This operation is thread-safe. Its complexity is in O(1).
        /// </summary>
        public Edge RandomEdge
        {
            get
            {
                _rwl.EnterReadLock();
                Edge e = null;
                if(_edges.Count>0)
                    e = _edges.Values.ElementAt<Edge>(NextRandom(_edges.Count));
                _rwl.ExitReadLock();
                return e;
            }
        }

        /// <summary>
        /// Returns a string representation of the graph, consisting of name and vertex/edge number. This operation is thread-safe.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            _rwl.EnterReadLock();

            string name = this.Name + "( n= " + _vertices.Count.ToString() + ", m= " + _edges.Count.ToString() + ")";

            _rwl.ExitReadLock();
            return name;
        }

    }
}
