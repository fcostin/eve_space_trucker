using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EveSpaceTrucker
{
    //attempt to use an all points shortest path graph algorithm on the map
    //ie fail.
    public class JumpEstimator
    {
        public const int UNREACHABLE = 1000000;

        public struct SystemPair
        {
            public UniverseGraph.SystemId id1, id2;

            public SystemPair(UniverseGraph.SystemId idA, UniverseGraph.SystemId idB)
            {
                if (idA.id < idB.id)
                {
                    id1 = idA;
                    id2 = idB;
                }
                else
                {
                    id2 = idA;
                    id1 = idB;
                }
            }
        }

        public Dictionary<SystemPair, int> distance;

        public UniverseGraph graph;

        public JumpEstimator(UniverseGraph graph)
        {
            this.graph = graph;
            distance = new Dictionary<SystemPair, int>();
        }

        public void computeDistances()
        {
            //setup
            foreach (UniverseGraph.SystemId sid in graph.idToInfo.Keys)
            {
                //zero distance to self
                distance.Add(new SystemPair(sid, sid), 0);
                //distance one to neighbours
                foreach (UniverseGraph.SystemId sid2 in graph.systemEdges[sid])
                {
                    distance[new SystemPair(sid, sid2)] = 1;
                }
            }

            int systemCount = graph.idToInfo.Keys.Count;
            //relaxation - order n^3 hahahaa
            for (int i = 0; i < systemCount; i++)
            {
                Console.WriteLine("relaxation, iteration " + i);
                foreach (UniverseGraph.SystemId sid1 in graph.idToInfo.Keys)
                {
                    foreach (UniverseGraph.SystemId sid2 in graph.idToInfo.Keys)
                    {
                        relax(sid1, sid2);
                    }
                }
            }
        }

        public int dist(UniverseGraph.SystemId sid1, UniverseGraph.SystemId sid2)
        {
            SystemPair pair = new SystemPair(sid1, sid2);
            if (!distance.ContainsKey(pair))
                return UNREACHABLE;
            else
                return distance[pair];
        }

        public void relax(UniverseGraph.SystemId sid1, UniverseGraph.SystemId sid2)
        {
            int distUpperBound = dist(sid1, sid2);
            SystemPair pair = new SystemPair(sid1, sid2);
            foreach (UniverseGraph.SystemId sid in graph.idToInfo.Keys)
            {
                int distCandidate = dist(sid1, sid) + dist(sid, sid2);
                if (distCandidate < distUpperBound)
                {
                    distance[pair] = distCandidate;
                    distUpperBound = distCandidate;
                }
            }
        }
    }
}
