using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EveSpaceTrucker
{
    public class Pathfinder
    {
        //used to represent nodes in search
        public class Node
        {
            public Node parent;
            public UniverseGraph.SystemId sysid;

            public Node(Node parent, UniverseGraph.SystemId sysid)
            {
                this.parent = parent;
                this.sysid = sysid;
            }
        }

        //might want to maintain a cache of common routes here at some point

        public struct SystemPair
        {
            public ushort id1, id2;

            public SystemPair(ushort sourceSystemId, ushort destSystemId)
            {
                //universe graph is undirected, so we only need to cache one direction
                if (sourceSystemId < destSystemId)
                {
                    id1 = sourceSystemId;
                    id2 = destSystemId;
                }
                else
                {
                    id1 = destSystemId;
                    id2 = sourceSystemId;
                }
            }
        }

        private Dictionary<SystemPair, int> distanceCache;

        private int cacheHits, cacheMisses;

        public string CacheStats
        {
            get
            {
                return "cache[ratio=" + (((double)cacheHits) / cacheMisses)+", size="+distanceCache.Count+"]";
            }
        }


        //store a ref to our map information
        public UniverseGraph graph;

        //closed / open lists for search
        private PriorityQueue<int, Node> open;
        private HashSet<UniverseGraph.SystemId> closed;
        //ignore systems with security less than this
        private float minSecurity;
        //terminate search if closed list size exceeds this constraint
        private int maxClosedSize;

        public Pathfinder(UniverseGraph graph, float minSecurity, int maxClosedSize)
        {
            this.graph = graph;
            this.minSecurity = minSecurity;
            this.maxClosedSize = maxClosedSize;

            distanceCache = new Dictionary<SystemPair, int>();
            cacheHits = 0;
            cacheMisses = 0;
        }

        public void exportDistanceCache(string path)
        {
            StreamWriter writer = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write));

            foreach (SystemPair key in distanceCache.Keys)
            {
                String line = key.id1.ToString()+","+key.id2.ToString()+","+distanceCache[key];
                writer.WriteLine(line);
            }

            writer.Close();
        }

        public void importDistanceCache(string path)
        {
            StreamReader reader = new StreamReader(File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite));

            String line = reader.ReadLine();
            while((line!=null) & (line!=""))
            {
                char[] commas = {','};
                String[] toks = line.Split(commas);
                if (toks.Length == 3)
                {
                    ushort id1 = UInt16.Parse(toks[0]);
                    ushort id2 = UInt16.Parse(toks[1]);
                    int dist = Int32.Parse(toks[2]);
                    SystemPair pair = new SystemPair(id1, id2);
                    distanceCache[pair] = dist;
                }
                line = reader.ReadLine();
                if (line == null)
                    break;
            }

            reader.Close();
        }

        public int getDistance(UniverseGraph.SystemInfo source, UniverseGraph.SystemInfo dest)
        {
            return getDistance(graph.nameToId[source.name], graph.nameToId[dest.name]);
        }

        //todo : add some kind of control to keep cache size in check
        public int getDistance(UniverseGraph.SystemId source, UniverseGraph.SystemId dest)
        {
            SystemPair key = new SystemPair(source.id, dest.id);
            if (distanceCache.ContainsKey(key))
            {
                ++cacheHits;
                return distanceCache[key];
            }
            else
            {
                ++cacheMisses;
                int computedDist = findDist(source, dest);
                distanceCache[key] = computedDist;
                return computedDist;
            }
        }

        private int findDist(UniverseGraph.SystemId source, UniverseGraph.SystemId dest)
        {
            open = new PriorityQueue<int, Node>();
            closed = new HashSet<UniverseGraph.SystemId>();

            open.push(0, new Node(null, source));

            Node current = null;
            int currentPriority = 0;
            while ((open.count() > 0) && (closed.Count < maxClosedSize))
            {
                //pop top
                PriorityQueue<int, Node>.Pair head = open.pop();
                current = head.value;
                currentPriority = head.key;

                //check if we've reached the destination
                if (current.sysid.id == dest.id)
                    return currentPriority*-1;

                if (!closed.Contains(current.sysid))
                {
                    //mark that we've already expanded it
                    closed.Add(current.sysid);
                    //expand it
                    foreach (UniverseGraph.SystemId neighbour in graph.systemEdges[current.sysid])
                    {
                        if (graph.idToInfo[neighbour].security > minSecurity)
                        {
                            Node nNode = new Node(current, neighbour);
                            //decrease priority by number of jumps so far - ie breadth first search
                            //some a* like heuristic would be nice....
                            int nPriority = currentPriority - 1;
                            open.push(nPriority, nNode);
                        }
                    }
                }
            }

            //some kind of failure state - either no path, or seach size exceeded (too far)
            return -1;
        }
    }
}
