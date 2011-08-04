using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace NETGen.Core
{
    /// <summary>
    /// This is NETGen's central class, representing a thread-safe graph consisting of vertices and undirected edges. This class is multi-thread-efficient and thread-safe,
    /// multiple concurrent read accesses are allowed, while concurrent read/write accesses are being synchronized. Please note, that an exception will be thrown when a write-access (like e.g.
    /// AddVertex, RemoveEdge, ... is nested inside a thread-safe iteration (like e.g. the Vertices and Edges iterators). If you need to do such an operation, 
    /// obtain a copy of the thread-safe iterator first. Please refer to the iterators for more information.
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
        /// A reader/writer lock that can be used b inheriting classes to synchronize multi-thread efficient access to _edges and _vertices
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
        protected Dictionary<int, List<Vertex>> _vertexClasses;

        /// <summary>
        /// This is a non-thread-safe dictinary that contains all edges
        /// </summary>
        protected Dictionary<Guid, Edge> _edges;

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
            _vertexClasses = new Dictionary<int, List<Vertex>>();
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
            _vertexClasses = new Dictionary<int, List<Vertex>>();
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
        /// Adds a specific vertex to the graph. This is only available to derived classes. This operation is thread-safe and asynchronously 
        /// fires a OnVertexAdded event. Its complexity is in O(1).
        /// </summary>
        /// <param name="v"></param>
        public virtual void AddVertex(Vertex v)
        {
            if (v.Network != this)
                throw new Exception("Error. A vertex created for a different graph cannot be added this graph!");

            if(_vertices.ContainsKey(v.ID))
                throw new Exception("Error. A vertex with this ID has already been added to this graph!");

            if (_rwl.IsReadLockHeld)
                throw new Exception("Synchronization error. You are probably trying to change the graph while iterating through its edges or vertices!");

            if (!_rwl.IsWriteLockHeld)
            {
                _rwl.EnterWriteLock();
                if (!_vertexClasses.ContainsKey(v.Class))
                    _vertexClasses[v.Class] = new List<Vertex>();
                _vertices.Add(v.ID, v);
                _vertexClasses[v.Class].Add(v);
                _rwl.ExitWriteLock();
            }
            else
            {
                _vertices.Add(v.ID, v);

                if (!_vertexClasses.ContainsKey(v.Class))
                    _vertexClasses[v.Class] = new List<Vertex>();
                _vertexClasses[v.Class].Add(v);
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
        /// Checks whether two vertices are in the same connected component using only edges of a certain label. 
        /// This operation is thread-safe. Its complexity is in O(n+m) with n being the number of 
        /// vertices and m being the number of edges.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <param name="label">The label of the edges in the connected component</param>
        /// <returns>Whether v and w are in the same connected component, i.e. whether they are connected by a path of edges with the given label</returns>
        public bool InSameComponent(Vertex v, Vertex w, int label)
        {
            _rwl.EnterReadLock();
            Stack<Vertex> stack = new Stack<Vertex>();
            List<Vertex> visited = new List<Vertex>();
            stack.Push(v);
            Vertex x;
            while (stack.Count > 0)
            {
                x = stack.Pop();
                visited.Add(x);
                foreach (Vertex s in x.Neigbors)
                    if (!visited.Contains(s) && x.GetEdgeToSuccessor(s).Label==label)
                        stack.Push(s);
            }
            _rwl.ExitReadLock();
            return visited.Contains(w);
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
        /// Removes all edges with a certain label. This operation is thread-safe. Its complexity is in O(m) with m being the number of edges.
        /// </summary>
        /// <param name="label">The label identifying edges that shall be removed</param>
        public void RemoveEdgesWithLabel(int label)
        {
            _rwl.EnterReadLock();
            List<Edge> toRemove = new List<Edge>();
            foreach (Edge e in _edges.Values)
                if (e.Label == label)
                    toRemove.Add(e);
            _rwl.ExitReadLock();

            RemoveEdges(toRemove);
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
                _vertexClasses[v.Class].Remove(v);
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
        /// Removes all vertices of a certain vertex class
        /// </summary>
        /// <param name="cls"></param>
        public void RemoveVerticesOfClass(int cls)
        {
            foreach (Vertex v in VerticesByClass(cls).ToArray<Vertex>())
                RemoveVertex(v);
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
            _vertexClasses.Clear();
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
        /// An enumeration of all vertices. Iterating through this is thread-safe, all asynchronous changes to the graph will be blocked until enumeration has finished. 
        /// It is however not allowed to modify the graph while iterating through edges. If you explicitely need to do this, you can circumvent this behavior 
        /// by iterating through vertices like this foreach(Vertex v in Graph.Vertices.ToArray()) ... This forces immediate evaluation 
        /// of the iterator. However if used in this way just like storing the contents of the enumeration externally (e.g. in a list, etc.)
        /// it may happen that a vertex does not exist anymore once the enumeration has been completed. Also, new vertices might have been added in the meantime.
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
        /// Returns an enumeration of all vertices of a certain class. This operation is thread-safe.
        /// </summary>
        /// <param name="cls">The class identifier</param>
        /// <returns></returns>
        public IEnumerable<Vertex> VerticesByClass(int cls)
        {
            _rwl.EnterReadLock();

            if (_vertexClasses.ContainsKey(cls))
                foreach (Vertex v in _vertexClasses[cls])
                    yield return v;

            _rwl.ExitReadLock();
        }

        /// <summary>
        /// Returns an enumeration of vertex class. This operation is thread-safe
        /// </summary>
        public IEnumerable<int> VertexClasses
        {
            get
            {
                _rwl.EnterReadLock();

                foreach (int i in _vertexClasses.Keys)
                    yield return i;

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
