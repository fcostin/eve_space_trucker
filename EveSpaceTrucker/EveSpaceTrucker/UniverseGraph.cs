using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EveSpaceTrucker
{
    public class UniverseGraph
    {
        public struct SystemId
        {
            //store last 4 digits of eve SolarSystemID here (rmr data)
            public ushort id;

            public SystemId(ushort id)
            {
                this.id = id;
            }

            public override string ToString()
            {
                return "SYSID:" + id.ToString();
            }
        }

        public struct SystemInfo
        {
            public string name;
            public double security;

            public SystemInfo(string name, double security)
            {
                this.name = name;
                this.security = security;
            }
        }

        // adjacency lists of neighbouring systems
        public Dictionary<SystemId, List<SystemId>> systemEdges;

        //systemid to info lookup
        public Dictionary<SystemId, SystemInfo> idToInfo;
        public Dictionary<string, SystemId> nameToId;

        public UniverseGraph()
        {
            systemEdges = new Dictionary<SystemId, List<SystemId>>();
            idToInfo = new Dictionary<SystemId, SystemInfo>();
            nameToId = new Dictionary<string, SystemId>();
        }

        public SystemId parseSystemId(string raw)
        {
            // - strip dots from system id string
            string result = raw.Replace(".", "");
            // only consider the last 4 digits
            result = result.Remove(0, result.Length - 4);
            return new SystemId(UInt16.Parse(result));
        }

        public void addSystemInfo(string solarSystemInfo)
        {
            char[] splitChars = {';'};
            char[] trimChars = {'\"'};
            string[] tokens = solarSystemInfo.Split(splitChars);
            for(int i=0;i<tokens.Length;++i)
                tokens[i] = tokens[i].Trim(trimChars);

            if (tokens.Length != 26)
                return;


            //token format (rmr data):

            // 0 "regionID";
            // 1 "constellationID";
            // 2 "solarSystemID";
            // 3 "solarSystemName";
            // 4 "x";
            // 5 "y";
            // 6 "z";
            // 7 "xMin";
            // 8 "xMax";
            // 9 "yMin";
            // 10 "yMax";
            // 11 "zMin";
            // 12 "zMax";
            // 13 "luminosity";
            // 14 "border";
            // 15 "fringe";
            // 16 "corridor";
            // 17 "hub";
            // 18 "international";
            // 19 "regional";
            // 20 "constellation";
            // 21 "security";
            // 22 "factionID";
            // 23 "radius";
            // 24 "sunTypeID";
            // 25 "securityClass";
            const int TOK_SYSTEM_ID = 2,
                TOK_SYSTEM_NAME = 3,
                TOK_SYSTEM_SECURITY = 21;


            
            //get system id
            SystemId parsedSysId = parseSystemId(tokens[TOK_SYSTEM_ID]);

            //convert security rating from 0,xx to 0.xx
            //tokens[TOK_SYSTEM_SECURITY] = tokens[TOK_SYSTEM_SECURITY].Replace(",", ".");
            
            SystemInfo parsedSysInfo = new SystemInfo(tokens[TOK_SYSTEM_NAME],
                double.Parse(tokens[TOK_SYSTEM_SECURITY]));

            //add to database
            idToInfo[parsedSysId] = parsedSysInfo;
            nameToId[tokens[TOK_SYSTEM_NAME]] = parsedSysId;
        }

        public void addJumpInfo(string jumpInfo)
        {
            // format (rmr data)
            // 0 "fromRegionID";
            // 1 "fromConstellationID";
            // 2 "fromSolarSystemID";
            // 3 "toSolarSystemID";
            // 4 "toConstellationID";
            // 5 "toRegionID"

            char[] splitChars = {';'};
            char[] trimChars = {'\"'};
            string[] tokens = jumpInfo.Split(splitChars);
            for(int i=0;i<tokens.Length;++i)
                tokens[i] = tokens[i].Trim(trimChars);

            if (tokens.Length != 6)
                return;

            SystemId fromSysId = parseSystemId(tokens[2]),
                toSysId = parseSystemId(tokens[3]);


            if(!systemEdges.ContainsKey(fromSysId))
                systemEdges[fromSysId] = new List<SystemId>();
            systemEdges[fromSysId].Add(toSysId);
        }

        //returns the number of systems culled
        public int removeLowSecuritySystems(float minSecurity)
        {
            List<SystemId> removeList = new List<SystemId>();
            foreach(SystemId sid in idToInfo.Keys)
            {
                if (idToInfo[sid].security < minSecurity)
                {
                    //flag for later removal
                    removeList.Add(sid);
                    //remove links to and from this system
                    if (!systemEdges.ContainsKey(sid))
                    {
                        //system has no edges? (?!?)
                        continue;
                    }

                    List<SystemId> neighbours = systemEdges[sid];
                    systemEdges.Remove(sid);
                    foreach (SystemId neighbour in neighbours)
                    {
                        systemEdges[neighbour].Remove(sid);
                    }
                }
            }
            int cullCount = removeList.Count;
            foreach (SystemId sid in removeList)
            {
                string name = idToInfo[sid].name;
                idToInfo.Remove(sid);
                nameToId.Remove(name);
            }
            return cullCount;
        }
    }
}
