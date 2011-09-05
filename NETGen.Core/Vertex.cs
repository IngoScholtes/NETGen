using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETGen.Core
{
    /// <summary>
    /// This class represents a vertex in a graph
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// The ID of this vertex
        /// </summary>
        public Guid ID {get; private set;}

        /// <summary>
        /// The graph to which this vertex belongs
        /// </summary>
        public Network Network { get; private set; }

        private Dictionary<Guid, Vertex> _successors;
        private Dictionary<Guid, Vertex> _predecessors;
        private Dictionary<Guid, Edge> _incomingEdges;
        private Dictionary<Guid, Edge> _outgoingEdges;
        private Dictionary<Guid, Edge> _undirectedEdges;
        private Dictionary<Guid, Edge> _successorToEdgeMap;
        private Dictionary<Guid, Edge> _predecessorToEdgeMap;

        System.Threading.ReaderWriterLock _rwl;

        /// <summary>
        /// Can be used to store arbitrary information related to this vertex
        /// </summary>
        public object Tag { get; set; }


        /// <summary>
        /// Can be used to classify vertices
        /// </summary>
        public int Class { get; private set; }

        /// <summary>
        /// The label/name of the vertex
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Creates a new vertex that can be added to a certain graph, but does not yet add it to the graph
        /// </summary>
        /// <param name="graph">The graph to which this vertex can be added</param>
        public Vertex(Network graph)
        {
            ID = Guid.NewGuid();
            Network = graph;
            _predecessors = new Dictionary<Guid, Vertex>();
            _successors = new Dictionary<Guid, Vertex>();
            _incomingEdges = new Dictionary<Guid, Edge>();
            _outgoingEdges = new Dictionary<Guid, Edge>();
            _undirectedEdges = new Dictionary<Guid, Edge>();
            _successorToEdgeMap = new Dictionary<Guid, Edge>();
            _predecessorToEdgeMap = new Dictionary<Guid, Edge>();
             _rwl = new System.Threading.ReaderWriterLock();
             Label = ID.ToString();
        }

        /// <summary>
        /// Creates a new vertex that can be added to a certain graph, but does not yet add it to the graph
        /// </summary>
        /// <param name="graph">The graph to which this vertex can be added</param>
        /// <param name="cls">The classification id of this vertex</param>
        public Vertex(Network graph, string label)
        {
            ID = Guid.NewGuid();
            Network = graph;
            _predecessors = new Dictionary<Guid, Vertex>();
            _successors = new Dictionary<Guid, Vertex>();
            _incomingEdges = new Dictionary<Guid, Edge>();
            _outgoingEdges = new Dictionary<Guid, Edge>();
            _undirectedEdges = new Dictionary<Guid, Edge>();
            _successorToEdgeMap = new Dictionary<Guid, Edge>();
            _predecessorToEdgeMap = new Dictionary<Guid, Edge>();
            _rwl = new System.Threading.ReaderWriterLock();
            Label = label;

        }

        /// <summary>
        /// Creates a new vertex that can be added to a certain graph, but does not yet add it to the graph
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="id"></param>
        public Vertex(Network graph, Guid id)
        {
            ID = id;
            Network = graph;
            _predecessors = new Dictionary<Guid, Vertex>();
            _successors = new Dictionary<Guid, Vertex>();
            _incomingEdges = new Dictionary<Guid, Edge>();
            _outgoingEdges = new Dictionary<Guid, Edge>();
            _undirectedEdges = new Dictionary<Guid, Edge>();
            _successorToEdgeMap = new Dictionary<Guid, Edge>();
            _predecessorToEdgeMap = new Dictionary<Guid, Edge>();
            _rwl = new System.Threading.ReaderWriterLock();
            Label = ID.ToString();
        }

        /// <summary>
        /// Creates a new vertex that can be added to a certain graph, but does not yet add it to the graph
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="id"></param>
        /// <param name="cls">The classification of this vertex</param>
        public Vertex(Network graph, Guid id, string label)
        {
            ID = id;
            Network = graph;
            _predecessors = new Dictionary<Guid, Vertex>();
            _successors = new Dictionary<Guid, Vertex>();
            _incomingEdges = new Dictionary<Guid, Edge>();
            _outgoingEdges = new Dictionary<Guid, Edge>();
            _undirectedEdges = new Dictionary<Guid, Edge>();
            _successorToEdgeMap = new Dictionary<Guid, Edge>();
            _predecessorToEdgeMap = new Dictionary<Guid, Edge>();
            _rwl = new System.Threading.ReaderWriterLock();
            Label = label;
        }

        /// <summary>
        /// Registers an edge with this vertex' local mappings. This operation is thread-safe.
        /// </summary>
        /// <param name="e">The Edge to register</param>
        internal void RegisterEdge(Edge e)
        {
           _rwl.AcquireWriterLock(-1);
            

            // undirected edges are added to the _undirectedEdges map
            if (e.EdgeType == EdgeType.Undirected)
            {
                Vertex neighbor = (e.Source == this) ? e.Target : e.Source;

                _predecessors.Add(neighbor.ID, neighbor);
                _predecessorToEdgeMap.Add(neighbor.ID, e);

                _successors.Add(neighbor.ID, neighbor);
                _successorToEdgeMap.Add(neighbor.ID, e);
                

                _undirectedEdges.Add(e.ID, e);
            }
            else if (e.EdgeType==EdgeType.DirectedAB)
            {
                if (e.A == this)
                {
                    _successors.Add(e.B.ID, e.B);
                    _successorToEdgeMap.Add(e.B.ID, e);

                    _outgoingEdges.Add(e.ID, e);
                }
                else if (e.B == this)
                {
                    _predecessors.Add(e.A.ID, e.A);
                    _predecessorToEdgeMap.Add(e.A.ID, e);

                    _incomingEdges.Add(e.ID, e);
                }
            }
            else if (e.EdgeType == EdgeType.DirectedBA)
            {
                if (e.B == this)
                {
                    _successors.Add(e.A.ID, e.A);
                    _successorToEdgeMap.Add(e.A.ID, e);

                    _outgoingEdges.Add(e.ID, e);
                }
                else if (e.A == this)
                {
                    _predecessors.Add(e.B.ID, e.B);
                    _predecessorToEdgeMap.Add(e.B.ID, e);

                    _incomingEdges.Add(e.ID, e);
                }
            }
            
            _rwl.ReleaseWriterLock();
        }

        /// <summary>
        /// Unregisters an edge with this vertex' local mappings. This operation is thread-safe.
        /// </summary>
        /// <param name="e">The edge to unregister</param>
        internal void UnregisterEdge(Edge e)
        {
             _rwl.AcquireWriterLock(-1);

            if (e.EdgeType == EdgeType.Undirected)
            {
                Vertex neighbor = (e.A == this) ? e.B : e.A;
                _predecessors.Remove(neighbor.ID);
                _predecessorToEdgeMap.Remove(neighbor.ID);

                _successors.Remove(neighbor.ID);
                _successorToEdgeMap.Remove(neighbor.ID);                

                _undirectedEdges.Remove(e.ID);
            }
            else if (e.EdgeType == EdgeType.DirectedAB)
            {
                if (e.A == this)
                {
                    _successors.Remove(e.B.ID);
                    _successorToEdgeMap.Remove(e.B.ID);

                    _outgoingEdges.Remove(e.ID);
                }
                else if (e.B == this)
                {
                    _predecessors.Remove(e.A.ID);
                    _predecessorToEdgeMap.Remove(e.A.ID);

                    _incomingEdges.Remove(e.ID);
                }
            }
            else if (e.EdgeType == EdgeType.DirectedBA)
            {
                if (e.B == this)
                {
                    _successors.Remove(e.A.ID);
                    _successorToEdgeMap.Remove(e.A.ID);

                    _outgoingEdges.Remove(e.ID);
                }
                else if (e.A == this)
                {
                    _predecessors.Remove(e.B.ID);
                    _predecessorToEdgeMap.Remove(e.B.ID);

                    _incomingEdges.Remove(e.ID);
                }
            }

            _rwl.ReleaseWriterLock();
        }

        /// <summary>
        /// Changes the registration of an edge depending on its type. This operation is thread-safe and its complexity is in O(1).
        /// </summary>
        /// <param name="e">The edge with the updated type</param>
        internal void ChangeEdgeRegistration(Edge e)
        {
            _rwl.AcquireWriterLock(-1);

            // A directed edge (AB or BA) is changed to undirected
            if (e.EdgeType == EdgeType.Undirected && !_undirectedEdges.ContainsKey(e.ID))
            {
                // this vertex is A and the edge to B is missing
                if (e.A == this && !_predecessors.ContainsKey(e.B.ID))
                {
                    _predecessors.Add(e.B.ID, e.B);
                    _predecessorToEdgeMap.Add(e.B.ID, e);
                }

                // this vertex is A and the edge to B is missing
                else if (e.B == this && !_predecessors.ContainsKey(e.A.ID))
                {
                    _predecessors.Add(e.A.ID, e.A);
                    _predecessorToEdgeMap.Add(e.A.ID, e);
                }

                // this vertex is B and the edge to A is missing
                else if (e.B == this && !_successors.ContainsKey(e.A.ID))
                {
                    _successors.Add(e.A.ID, e.A);
                    _successorToEdgeMap.Add(e.A.ID, e);
                }

                 // this vertex is B and the edge to A is missing
                else if (e.A == this && !_successors.ContainsKey(e.B.ID))
                {
                    _successors.Add(e.B.ID, e.B);
                    _successorToEdgeMap.Add(e.B.ID, e);
                }



                _incomingEdges.Remove(e.ID);
                _outgoingEdges.Remove(e.ID);
                _undirectedEdges.Add(e.ID, e);

            } 
            // An undirected edge is changed to directed (AB)
            else if (e.EdgeType == EdgeType.DirectedAB && _undirectedEdges.ContainsKey(e.ID))
            {
                // this is the source, so we remove the predecessor and add it as outgoing edge
                if (e.A == this && _predecessors.ContainsKey(e.B.ID) && !_outgoingEdges.ContainsKey(e.ID))
                {
                    _predecessors.Remove(e.B.ID);
                    _predecessorToEdgeMap.Remove(e.B.ID);
                    _outgoingEdges.Add(e.ID, e);
                }
                // this is the target, so we remove the successor and add it as incoming edge
                else if (e.B == this && _successors.ContainsKey(e.A.ID) && !_incomingEdges.ContainsKey(e.ID))
                {
                    _successors.Remove(e.A.ID);
                    _successorToEdgeMap.Remove(e.A.ID);
                    _incomingEdges.Add(e.ID, e);
                }

                // remove this edge from the _undirected edges
                _undirectedEdges.Remove(e.ID);
            }
            // An undirected edge is changed to directed (BA)
            else if (e.EdgeType == EdgeType.DirectedBA && _undirectedEdges.ContainsKey(e.ID))
            {
                // this is the source, so we remove the predecessor and add it as outgoing edge
                if (e.B == this && _predecessors.ContainsKey(e.A.ID) && !_outgoingEdges.ContainsKey(e.ID))
                {
                    _predecessors.Remove(e.A.ID);
                    _predecessorToEdgeMap.Remove(e.A.ID);
                    _outgoingEdges.Add(e.ID, e);
                }
                // this is the target, so we remove the successor and add it as incoming edge
                else if (e.A == this && _successors.ContainsKey(e.B.ID) && !_incomingEdges.ContainsKey(e.ID))
                {
                    _successors.Remove(e.B.ID);
                    _successorToEdgeMap.Remove(e.B.ID);
                    _incomingEdges.Add(e.ID, e);
                }

                // remove this edge from the _undirected edges
                _undirectedEdges.Remove(e.ID);
            }
            // The direction of an edge is changed from AB to BA
            else if (e.EdgeType == EdgeType.DirectedBA && (_incomingEdges.ContainsKey(e.ID) || _outgoingEdges.ContainsKey(e.ID)))
            {
                // this vertex is now the source but used to be the target
                if (e.B == this && _predecessors.ContainsKey(e.A.ID) && _incomingEdges.ContainsKey(e.ID))
                {
                    // remove incoming edge
                    _predecessors.Remove(e.A.ID);
                    _predecessorToEdgeMap.Remove(e.A.ID);
                    _incomingEdges.Remove(e.ID);

                    // add outgoing edge
                    _successors.Add(e.A.ID, e.A);
                    _successorToEdgeMap.Add(e.A.ID, e);
                    _outgoingEdges.Add(e.ID, e);                    
                }
                // this vertex is now the target but used to be the source
                else if (e.A == this && _successors.ContainsKey(e.B.ID) && _outgoingEdges.ContainsKey(e.ID))
                {
                    // remove outgoing edge
                    _successors.Remove(e.B.ID);
                    _successorToEdgeMap.Remove(e.B.ID);
                    _outgoingEdges.Remove(e.ID);

                    // add incoming edge
                    _predecessors.Add(e.B.ID, e.B);
                    _predecessorToEdgeMap.Add(e.B.ID, e);
                    _incomingEdges.Add(e.ID, e);                    
                }                
            }
            // The direction of an edge is changed from BA to AB
            else if (e.EdgeType == EdgeType.DirectedAB && (_incomingEdges.ContainsKey(e.ID) || _outgoingEdges.ContainsKey(e.ID)))
            {
                // this vertex is now the target but used to be the source (BA)
                if (e.B == this && _successors.ContainsKey(e.A.ID) && _outgoingEdges.ContainsKey(e.ID))
                {
                    // remove outgoing edge
                    _successors.Remove(e.A.ID);
                    _successorToEdgeMap.Remove(e.A.ID);
                    _outgoingEdges.Remove(e.ID);

                    // add incoming edge
                    _predecessors.Add(e.A.ID, e.A);
                    _incomingEdges.Add(e.ID, e);
                    _predecessorToEdgeMap.Add(e.A.ID, e);
                }
                // this vertex is now the source but used to be the target
                else if (e.A == this && _predecessors.ContainsKey(e.B.ID) && _incomingEdges.ContainsKey(e.ID))
                {
                    // remove incoming edge
                    _predecessors.Remove(e.B.ID);
                    _predecessorToEdgeMap.Remove(e.B.ID);
                    _incomingEdges.Remove(e.ID);

                    // add outgoing edge
                    _successors.Add(e.B.ID, e.B);
                    _outgoingEdges.Add(e.ID, e);
                    _successorToEdgeMap.Add(e.B.ID, e);
                }
            }
            // This invariant must be true all the time!
#if DEBUG
            if (_successors.Count != _undirectedEdges.Count + _outgoingEdges.Count)
                throw new Exception("Edge inconsistency detected!");
#endif
            _rwl.ReleaseWriterLock();
        }

        /// <summary>
        /// Returns a reference to the (directed or indirected) edge that connects to a  specific successor vertex. This can either be 
        /// a directed edge starting from this vertex and arriving in the successor or an undirected edge connecting both vertices. This operation is thread-safe.
        /// </summary>
        /// <param name="successor">The successor to which this vertex connects</param>
        /// <returns>The edge between this vertex and the successor in question or null if no such edge exists</returns>
        public Edge GetEdgeToSuccessor(Vertex successor)
        {
            _rwl.AcquireReaderLock(-1);

            Edge e = null;

            if (_successorToEdgeMap.ContainsKey(successor.ID))
                e = _successorToEdgeMap[successor.ID];     

            _rwl.ReleaseReaderLock();

            return e;
        }

        /// <summary>
        /// Returns a reference to the (directed or indirected) edge that connects a specific predecessor with this vertex. This can either be 
        /// a directed edge arriving in this vertex and starting from predecessor or an undirected edge connecting both vertices. This operation is thread-safe.
        /// </summary>
        /// <param name="predecessor">The predecessor that connects to this vertex</param>
        /// <returns>The edge between this vertex and the predecessor in question or null if no such edge exists</returns>
        public Edge GetEdgeFromPredecessor(Vertex predecessor)
        {
            _rwl.AcquireReaderLock(-1);

            Edge e = null;

            if (_predecessorToEdgeMap.ContainsKey(predecessor.ID))
                e = _predecessorToEdgeMap[predecessor.ID];

            _rwl.ReleaseReaderLock();

            return e;
        }

        /// <summary>
        /// An enumeration of all edges (directed and undirected) incident to this vertex. Edges will be enumerated in the following order: edges arriving in this vertex, 
        /// edges starting from this vertex, undirected edges. 
        /// This Enumeration is thread-safe.
        /// </summary>
        public IEnumerable<Edge> Edges
        {
            get
            {
                _rwl.AcquireReaderLock(-1);
                
                    for(int i=0; i<_incomingEdges.Values.Count; i++)
                        yield return _incomingEdges.Values.ElementAt<Edge>(i);
                    for(int i=0; i<_outgoingEdges.Values.Count; i++)
                        yield return _outgoingEdges.Values.ElementAt<Edge>(i);
                    for (int i = 0; i < _undirectedEdges.Values.Count; i++)
                        yield return _undirectedEdges.Values.ElementAt<Edge>(i);

                _rwl.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// An enumeration of all directed and undirected edges starting from this vertex. 
        /// This Enumeration is thread-safe.
        /// </summary>
        public IEnumerable<Edge> OutgoingEdges
        {
            get
            {
                _rwl.AcquireReaderLock(-1);

                for (int i = 0; i < _outgoingEdges.Values.Count; i++)
                    yield return _outgoingEdges.Values.ElementAt<Edge>(i);
                for (int i = 0; i < _undirectedEdges.Values.Count; i++)
                    yield return _undirectedEdges.Values.ElementAt<Edge>(i);

                _rwl.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// An enumeration of all directed and undirected edges arriving in this vertex. 
        /// This Enumeration is thread-safe.
        /// </summary>
        public IEnumerable<Edge> IncomingEdges
        {
            get
            {
                _rwl.AcquireReaderLock(-1);

                for (int i = 0; i < _incomingEdges.Values.Count; i++)
                    yield return _incomingEdges.Values.ElementAt<Edge>(i);
                for (int i = 0; i < _undirectedEdges.Values.Count; i++)
                    yield return _undirectedEdges.Values.ElementAt<Edge>(i);

                _rwl.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// An enumeration of all undirected edges incident to this vertex. 
        /// This Enumeration is thread-safe.
        /// </summary>
        public IEnumerable<Edge> UndirectedEdges
        {
            get
            {
                _rwl.AcquireReaderLock(-1);
              
                for (int i = 0; i < _undirectedEdges.Values.Count; i++)
                    yield return _undirectedEdges.Values.ElementAt<Edge>(i);

                _rwl.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// An enumeration of all neighbor vertices. These are all vertices ... 
        /// a) which either are connected via an undirected edge or 
        /// b) to which a directed edge starting from this vertex points
        /// This Enumeration is thread-safe.
        /// </summary>
        public IEnumerable<Vertex> Neigbors
        {
            get
            {
                _rwl.AcquireReaderLock(-1);

                    foreach (Vertex v in _successors.Values)
                        yield return v;

                _rwl.ReleaseReaderLock();
            }
        }


        /// <summary>
        /// An enumeration of all vertices having an edge pointing to this vertex.
        /// This Enumeration is thread-safe.
        /// </summary>
        public IEnumerable<Vertex> Predecessors
        {
            get
            {
                _rwl.AcquireReaderLock(-1);

                foreach (Vertex v in _predecessors.Values)
                    yield return v;

                _rwl.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Returns the number of (directed and undirected) edges incident to this vertex. This operation is thread-safe.
        /// </summary>
        public int Degree
        {
            get
            {
                _rwl.AcquireReaderLock(-1);

                int count = _incomingEdges.Count + _outgoingEdges.Count + _undirectedEdges.Count;

                _rwl.ReleaseReaderLock();

                return count;
            }
        }

        /// <summary>
        /// Returns the number of undirected edges incident to this vertex.
        /// </summary>
        public int UndirectedDegree
        {
            get
            {
                return _undirectedEdges.Count;
            }
        }

        /// <summary>
        /// Returns the number of (directed and undirected) edges arriving in this node.
        /// </summary>
        public int InDegree
        {
            get
            {
                _rwl.AcquireReaderLock(-1);

                int count = _incomingEdges.Count + _undirectedEdges.Count;

                _rwl.ReleaseReaderLock();

                return count;
            }
        }

        /// <summary>
        ///  Returns the number of (directed and undirected) edges starting in this node.
        /// </summary>
        public int OutDegree
        {
            get
            {
                _rwl.AcquireReaderLock(-1);

                int count = _outgoingEdges.Count + _undirectedEdges.Count;

                _rwl.ReleaseReaderLock();

                return count;
            }
        }

        /// <summary>
        /// Returns a random neighbor (either undirected edge or successor of direct edges) of this vertex.
        /// </summary>
        /// <returns>A random neighbor or null if no neighbor exists</returns>
        public Vertex RandomNeighbor { 
            get 
            {
                Vertex v = null;

                _rwl.AcquireReaderLock(-1);

                if (_successors.Count > 0)
                        v = _successors.Values.ElementAt<Vertex>(Network.NextRandom(_successors.Count));

                _rwl.ReleaseReaderLock();

                return v;
            } 
        }

        /// <summary>
        /// Returns a random neighbor of this vertex other than the vertex specified. Complexity is in O(1) and this operation is thread-safe.
        /// </summary>
        /// <returns>A random neighbor other than v</returns>
        public Vertex RandomNeighborOtherThan(Vertex v)
        {
            Vertex r = null;

            _rwl.AcquireReaderLock(-1);

            if (_successors.Count > 0)
            {

                int vIx = _successors.Keys.ToList<Guid>().IndexOf(v.ID);

                // something's wrong, Vertex v is NOT our neighbor
                if (vIx < 0)
                {
                    _rwl.ReleaseReaderLock();
                    return RandomNeighbor;
                }

                int rIx = Network.NextRandom(_successors.Count - 1);

                if (rIx >= vIx && vIx + 1 < _successors.Count)
                    r =  _successors.Values.ElementAt<Vertex>(rIx + 1);
                else if (rIx < vIx)
                    r =  _successors.Values.ElementAt<Vertex>(rIx);
            }

            _rwl.ReleaseReaderLock();       

            return r;
        }

        /// <summary>
        /// Returns whether a given vertex is a successor (via a directed or an undirected edge) of this vertex. Complexity is in O(1).
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool IsSuccessor(Vertex v)
        {
            return _successors.ContainsKey(v.ID);
        }

        /// <summary>
        /// Returns whether a given vertex is predecessor (via a directed or an undirecte edge) of this vertex. Complexity is in O(1).
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool IsPredecessor(Vertex v)
        {
            return _predecessors.ContainsKey(v.ID);
        }   

        /// <summary>
        /// Returns whether two vertex instances are equal
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool operator ==(Vertex v1, Vertex v2)
        {
            if ((v1 as object) == null)
                return (v2 as object) == null;
            if ((v2 as object) == null)
                return (v1 as object) == null;
            return v1.ID == v2.ID;
        }

        /// <summary>
        /// Returns whether two vertex instances are equal
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool operator <(Vertex v1, Vertex v2)
        {
            if ((v1 as object) == null)
                return false;
            if ((v2 as object) == null)
                return false;
            return v1.ID.CompareTo(v2.ID)<0;
        }

        /// <summary>
        /// Returns whether two vertex instances are equal
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool operator >(Vertex v1, Vertex v2)
        {
            if ((v1 as object) == null)
                return false;
            if ((v2 as object) == null)
                return false;
            return v1.ID.CompareTo(v2.ID) > 0;
        }

        /// <summary>
        /// Returns whether two vertex instances differ
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool operator !=(Vertex v1, Vertex v2)
        {
            if ((v1 as object) == null)
                return (v2 as object) != null;
            if ((v2 as object) == null)
                return (v1 as object) != null;
            return v1.ID != v2.ID;
        }

        /// <summary>
        /// Returns a has code for this object
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Retrurns whether an instance equals another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Vertex)
                return ((obj as Vertex) == this);
            else
                return false;
        }	
    }
}
