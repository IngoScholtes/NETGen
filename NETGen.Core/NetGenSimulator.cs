using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace NETGen.Core
{
    public enum SimulationMode { PerNetwork = 0, PerVertex = 1, PerEdge = 2, PerVertexAndEdge = 3 };
    public enum ConcurrencyMode { VertexParallel = 0, EdgeParallel = 2, Both = 3, NoConcurrency = 4};

    public class NetGenSimulator
    {
        [DefaultValue(null)]
        public static Network Network { get; set; }

        [DefaultValue(false)]
        public static bool SimulationRunning { get; private set; }

        [DefaultValue(0)]
        public static long SimulationTime { get; private set; }
        
        [DefaultValue(ConcurrencyMode.NoConcurrency)]
        public static ConcurrencyMode ConcurrencyMode { get; set; }

        [DefaultValue(SimulationMode.PerNetwork)]
        public static SimulationMode SimulationMode { get; set; }

        [DefaultValue(double.PositiveInfinity)]
        public static double TicksPerSecond { get; set; }

        public delegate void VertexTickHandler(Vertex v);
        public delegate void EdgeTickHandler(Edge e);
        public delegate void NetworkTickHandler(Network n);

        public static event VertexTickHandler TickVertex;
        public static event EdgeTickHandler TickEdge;
        public static event NetworkTickHandler TickNetwork;

        public static void Start()
        {
            if (Network == null)
                return;
            SimulationRunning = true;
            SimulationTime = 0;
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(simulationWorker), null);            
        }

        static void simulationWorker(object o)
        {
            while (SimulationRunning)
            {
                if (SimulationMode == Core.SimulationMode.PerNetwork && TickNetwork != null)
                    TickNetwork(Network);
                if ((SimulationMode == Core.SimulationMode.PerVertex || SimulationMode == Core.SimulationMode.PerVertexAndEdge) && TickVertex != null)
                {
                    if (ConcurrencyMode == Core.ConcurrencyMode.VertexParallel || ConcurrencyMode == Core.ConcurrencyMode.Both)
                        Parallel.ForEach(Network.Vertices.ToArray(), v =>
                            {
                                TickVertex(v);
                            });
                    else
                        foreach(Vertex v in Network.Vertices.ToArray())
                            TickVertex(v);
                }
                if ((SimulationMode == Core.SimulationMode.PerEdge || SimulationMode == Core.SimulationMode.PerVertexAndEdge) && TickEdge != null)
                {
                    if (ConcurrencyMode == Core.ConcurrencyMode.VertexParallel || ConcurrencyMode == Core.ConcurrencyMode.Both)
                        Parallel.ForEach(Network.Edges.ToArray(), e =>
                        {
                            TickEdge(e);
                        });
                    else
                        foreach (Edge e in Network.Edges.ToArray())
                            TickEdge(e);
                }
                SimulationTime++;
                if (TicksPerSecond != double.PositiveInfinity)
                    System.Threading.Thread.Sleep((int) (1000/(TicksPerSecond)));
            }
        }

        public static void Stop()
        {
            SimulationRunning = false;
        }

    }
}
