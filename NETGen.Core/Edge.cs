using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETGen.Core
{

    /// <summary>
    /// Enumeration that is used to distinguish between directed and undirected edges
    /// </summary>
    public enum EdgeType {
        /// <summary>
        /// Undirected edge between A and B
        /// </summary>
        Undirected = 0, 
        /// <summary>
        /// Directed edge from A to B
        /// </summary>
        DirectedAB = 1,
        
        /// <summary>
        /// Directed edge from B to 
        /// </summary>
        DirectedBA = 2
    };

    /// <summary>
    /// This class represents an undirected edge between two vertices
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// The ID of this edge
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// The source vertex of this edge
        /// </summary>
        internal Vertex A { get; private set; }

        /// <summary>
        /// The target vertex of this edge
        /// </summary>
        internal Vertex B { get; private set; }

        /// <summary>
        /// The graph to which this edge belongs
        /// </summary>
        protected Network Network { get; private set; }

        /// <summary>
        /// Can be used to attach an arbitrary .NET/MONO object to this edge
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Can be used to classify edges
        /// </summary>
        public int Label { get; set; }

        /// <summary>
        /// Creates a new edge between source and target. The edge is not registered with the source and 
        /// target vertices unless it is added to the graph!
        /// </summary>
        /// <param name="a">The A vertex</param>
        /// <param name="b">The B vertex</param>
        /// <param name="graph">The graph to which this edge belongs</param>
        /// <param name="type">Whether to create an undirected or a directed edge</param>
        /// <param name="id">The ID that this vertex will be assigned</param>
        public Edge(Vertex a, Vertex b, Network graph, EdgeType type, Guid id)
        {
            ID = id;
            A = a;
            B = b;
            Network = graph;
            EdgeType = type;
        }   

        /// <summary>
        /// Creates a new edge between source and target. The edge is not registered with the source and 
        /// target vertices unless it is added to the graph!
        /// </summary>
        /// <param name="a">The A vertex</param>
        /// <param name="b">The B vertex</param>
        /// <param name="graph">The graph to which this edge belongs</param>
        /// <param name="type">Whether to create an undirected or a directed edge</param>
        public Edge(Vertex a, Vertex b, Network graph, EdgeType type)
        {
            ID = Guid.NewGuid();
            A = a;
            B = b;
            Network = graph;
            EdgeType = type;
        }   


        /// <summary>
        /// Creates a new undirected edge between source and target. The edge is not registered with the source and 
        /// target vertices unless it is added to the graph!
        /// </summary>
        /// <param name="a">The A vertex</param>
        /// <param name="b">The B vertex</param>
        /// <param name="graph">The graph to which this edge belongs</param>
        public Edge(Vertex a, Vertex b, Network graph)
        {
            ID = Guid.NewGuid();
            A = a;
            B = b;
            Network = graph;
            EdgeType = EdgeType.Undirected;
        }

        /// <summary>
        /// Returns whether two edge instances are equal
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static bool operator ==(Edge e1, Edge e2)
        {
            
            if ((e1 as object) == null)
                return (e2 as object) == null;
            if ((e2 as object) == null)
                return (e1 as object) == null;
            lock (e1)
                lock (e2)
                {
                    if ((e1.EdgeType == EdgeType.DirectedAB && e2.EdgeType == EdgeType.DirectedAB) || (e1.EdgeType == EdgeType.DirectedBA && e2.EdgeType == EdgeType.DirectedBA))
                        return e1.A.ID == e2.A.ID && e1.B.ID == e2.B.ID;
                    else if ((e1.EdgeType == EdgeType.DirectedBA && e2.EdgeType == EdgeType.DirectedAB) || (e1.EdgeType == EdgeType.DirectedAB && e2.EdgeType == EdgeType.DirectedBA))
                        return e1.A.ID == e2.B.ID && e1.B.ID == e2.A.ID;
                    if (e1.EdgeType == EdgeType.Undirected && e2.EdgeType == EdgeType.Undirected)
                        return (e1.A.ID == e2.A.ID && e1.B.ID == e2.B.ID) || (e1.B.ID == e2.A.ID && e1.A.ID == e2.B.ID);
                    return false;
                }
        }


        /// <summary>
        /// This property can be used to get or set whether this edge is a directed or an undirected edge. Changing the type of this edge 
        /// will automatically perform all necessary changes in the underlying graph and the corresponding vertices. This operation is thread-safe. Its complexity 
        /// is in O(1).
        /// </summary>
        public EdgeType EdgeType { get; internal set; }


        /// <summary>
        /// The source vertex of this edge
        /// </summary>
        public Vertex Source
        {
            get
            {
                lock (this)
                {
                    if (EdgeType == EdgeType.DirectedAB)
                        return A;
                    else if (EdgeType == EdgeType.DirectedBA)
                        return B;
                    else
                        return A;
                }
            }
        }

        /// <summary>
        /// The target vertex of this edge
        /// </summary>
        public Vertex Target
        {
            get
            {
                lock (this)
                {
                    if (EdgeType == EdgeType.DirectedAB)
                        return B;
                    else if (EdgeType == EdgeType.DirectedBA)
                        return A;
                    else
                        return B;
                }
            }
        }

        /// <summary>
        /// Returns whether two edge instances differ
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static bool operator !=(Edge e1, Edge e2)
        {
            return !(e1 == e2);
        }

        /// <summary>
        /// Returns the hash code for this object
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Compares an instance to an object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Edge)
                return ((obj as Edge) == this);
            else
                return false;
        }        
    }
}
