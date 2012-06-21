using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EveSpaceTrucker
{
    class Program
    {
        static void Main(string[] args)
        {
            //test space time profit cache

            //SpaceTimeProfitCache stpCache = new SpaceTimeProfitCache();
            //Console.WriteLine(stpCache);
            //Console.WriteLine(stpCache.achievesBetterProfit("foobar", 1.0, 100));
            //Console.WriteLine(stpCache);
            //Console.WriteLine(stpCache.achievesBetterProfit("foobar", 1.0, 100));
            //Console.WriteLine(stpCache);
            //Console.WriteLine(stpCache.achievesBetterProfit("foobar", 2.0, 100));
            //Console.WriteLine(stpCache);
            //Console.WriteLine(stpCache.achievesBetterProfit("foobar", 3.0, 150));
            //Console.WriteLine(stpCache);
            //Console.WriteLine(stpCache.achievesBetterProfit("foobar", 3.0, 175));
            //Console.WriteLine(stpCache);
            //Console.WriteLine(stpCache.achievesBetterProfit("foobar", 0.5, 175));
            //Console.WriteLine(stpCache);
            //Console.ReadLine();
            //return;

            //test pqueue

            //PriorityQueue<int, int> pq = new PriorityQueue<int, int>();
            //Random r = new Random();

            //for (int i = 0; i < 100; ++i)
            //{
            //    pq.push(r.Next(1000) - 500, i);
            //}
            //while (pq.count() > 0)
            //{
            //    PriorityQueue<int, int>.Pair p = pq.pop();
            //    Console.WriteLine("key = " + p.key + ", value = " + p.value);
            //}

            //Console.ReadKey();

            //return;
                
            Console.WriteLine("Welcome to a rather ghetto build of EveSpaceTrucker.");
            Console.WriteLine();
            Console.Write("Importing Solar System Info...");

            //import system info
            StreamReader reader = File.OpenText(
                "C:\\Documents and Settings\\snap\\Desktop\\eve\\space trucking\\quantum rise database csvs\\solarSystems.csv");


            UniverseGraph universeGraph = new UniverseGraph();
            
            
            string line = reader.ReadLine();
            //ignore header line
            line = reader.ReadLine();
            while (line != null)
            {
                universeGraph.addSystemInfo(line);
                line = reader.ReadLine();
            }

            reader.Close();

            Console.WriteLine(" OK.");

            Console.Write("Importing System Jump Info...");

            //import jump info to construct adjacency lists
            reader = File.OpenText(
                "C:\\Documents and Settings\\snap\\Desktop\\eve\\space trucking\\quantum rise database csvs\\solarSystemJumps.csv");

            line = reader.ReadLine();
            //ignore header line
            line = reader.ReadLine();
            while (line != null)
            {
                universeGraph.addJumpInfo(line);
                line = reader.ReadLine();
            }
            reader.Close();

            Console.WriteLine(" OK.");

            float lowSecThreshhold = 0.5f;

            Console.WriteLine("Removing Systems with Security < "+lowSecThreshhold+" ...");

            // cull lowsec from the map
            int cullCount = universeGraph.removeLowSecuritySystems(lowSecThreshhold);

            Console.WriteLine("OK, number of systems culled was "+cullCount);
            
            Console.Write("Importing Item Type Info ...");

            ItemDatabase items = new ItemDatabase();
            items.parseInvTypes(
                "C:\\Documents and Settings\\snap\\Desktop\\eve\\space trucking\\quantum rise database csvs\\invTypes.csv");

            Console.WriteLine(" OK.");

            Console.Write("Importing Market Dumps ...");

            MarketDatabase market = new MarketDatabase(universeGraph, items);

            DirectoryInfo dumpDir = new DirectoryInfo(
                "C:\\Documents and Settings\\snap\\My Documents\\EVE\\logs\\Marketlogs\\");
            FileInfo[] dumps = dumpDir.GetFiles("*.txt");
            foreach (FileInfo fi in dumps)
            {
                string dumpPath = fi.FullName;
                //Console.WriteLine("- " + dumpPath);
                market.parseMarketDumpFile(dumpPath);
            }
            Console.WriteLine(" OK.");

            TradeRouteMiner.Constraints constraints;

            //trade routes must meet or exceed this level of profit / (volume * jump)
            constraints.minProfitDensityRate = 3.0;
            //trade routes must meet or exceed this level of profit / jump assuming ALL items were transported
            //this ensures orders with small quantities are ignored, unless they are very profitable per item
            constraints.minBulkProfitRate = 30000.0;

            Pathfinder pather = new Pathfinder(universeGraph, 0.5f, 1024);
            TradeRouteMiner routeMiner = new TradeRouteMiner(pather, market, items);
            routeMiner.setConstraints(constraints);

            string distanceCachePath =
                "C:\\Documents and Settings\\snap\\Desktop\\eve\\space trucking\\cache\\jumps.cache";

            Console.WriteLine("Importing jumps from "+distanceCachePath);
            pather.importDistanceCache(distanceCachePath);
            Console.Write(" OK.");

            Console.WriteLine("Computing trade routes ...");

            routeMiner.computeRoutes();

            Console.WriteLine(" OK.");
            Console.WriteLine(" ~ " + routeMiner.addedRoutes + " were added");

            Console.WriteLine("Exporting jumps to " + distanceCachePath);
            pather.exportDistanceCache(distanceCachePath);
            Console.WriteLine(" OK.");

            bool quit = false;

            double initialWallet = 0.0;
            double initialCargoCap = 0.0;
            string initialSystemName = "";
            int maxJumps = 0;
            while (!quit)
            {
                try
                {
                    Console.WriteLine("GHETTO PLANNER INTERFACE");
                    Console.WriteLine("available commands: query, plan");
                    Console.Write("Cmd >");
                    string cmd = Console.ReadLine();
                    if (cmd.Equals("query"))
                    {
                        Console.WriteLine("item names are case sensitive");
                        Console.Write("Item Name >");
                        string itemName = Console.ReadLine();
                        Console.WriteLine("database entry: " + items.nameToEntry[itemName]);
                        continue;
                    }
                    else if (cmd.Equals("plan"))
                    {
                        Console.Write("Wallet >");
                        initialWallet = double.Parse(Console.ReadLine());
                        Console.Write("CargoCap >");
                        initialCargoCap = double.Parse(Console.ReadLine());
                        Console.Write("SystemName >");
                        initialSystemName = Console.ReadLine();
                        //double initialWallet = 60000000.0;
                        //double initialCargoCap = 13000.0;
                        //string initialSystemName = "Amarr";
                        Console.Write("MaxJumps >");
                        maxJumps = int.Parse(Console.ReadLine());
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("parse error (?)");
                    continue;
                }

                Console.Write("Attempting to Plan Trade Routes (Experimental, yet Promising!) ...");

                TradePlanner planner = new TradePlanner(routeMiner, universeGraph);

                TradePlanner.PlannerState ps = planner.computePlan(initialWallet,
                    initialCargoCap,
                    initialSystemName,
                    maxJumps);

                Console.WriteLine(" OK.");
                planner.nicelyDumpPlanToConsole(ps);
                Console.ReadKey();
            }

        }
    }
}
