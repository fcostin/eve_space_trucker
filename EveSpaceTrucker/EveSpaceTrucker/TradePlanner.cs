using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EveSpaceTrucker
{
    //TODO LIST:
    //
    // * BETTER HEURISTIC FOR UPPER BOUND ON POTENTIAL PROFIT
    // * BETTER CLOSED LIST BEHAVIOUR - leverage temporal ordering - younger richer entries kill older poorer ones
    //      + implemented! as SpaceTimeProfitCache.cs, via Collections.Generic.SortedList
    // * TIGHTER ARBITRARY BOUNDS ON TRADES TO CONSIDER ?
    // * CONTROL ON OPEN LIST SIZE - DROP ENTRIES WITH POOR PRIORITY IF IT GROWS TOO LARGE?
    //
    // * MODEL CHANGES - REWORK COSTS FOR JUMPING
    // - perhaps take docking time into account - add some constant cost to each trade route jump count ( +2 perhaps)
    // - this would favour longer haul runs over multiple slightly more profitable (net) runs
    // - ie it would favour laziness. yay

    public class TradePlanner
    {
        public class PlannerState
        {
            static double jumpElapsedCost(int rawJumps)
            {
                if (rawJumps > 0)
                    return (double)rawJumps;
                else
                    return 0.5;
            }

            //nb: jumps Elapsed left as double in case we want to count intra-system trades as 0.5 a jump
            public double cargoCapacity, wallet, jumpsElapsed;
            public string sysName;
            public PlannerState parent;
            //information to track the last trade we made:
            public TradeRouteMiner.Route lastRoute;
            public int lastQuantityHauled;

            //used to construct an initial state
            public PlannerState(double cargoCapacity, double wallet, string sysName)
            {
                this.cargoCapacity = cargoCapacity;
                this.wallet = wallet;
                this.sysName = sysName;
                this.parent = null;
                this.lastRoute = null;
                this.lastQuantityHauled = 0;
            }

            //used to advance to a new state via a trade route
            public PlannerState(PlannerState previous, TradeRouteMiner.Route routeToTake, int quantityToHaul)
            {
                this.cargoCapacity = previous.cargoCapacity;
                this.wallet = previous.wallet + routeToTake.profitPerItem * quantityToHaul;
                this.jumpsElapsed = previous.jumpsElapsed + jumpElapsedCost(routeToTake.jumps);
                this.sysName = routeToTake.buyer.location.name;
                this.parent = previous;
                this.lastRoute = routeToTake;
                this.lastQuantityHauled = quantityToHaul;
            }

            //used to advance to a new state via an empty jump
            public PlannerState(PlannerState previous, string destination)
            {
                this.cargoCapacity = previous.cargoCapacity;
                this.wallet = previous.wallet;
                this.jumpsElapsed = previous.jumpsElapsed + 1.0;
                this.sysName = destination;
                this.parent = previous;
                this.lastRoute = null;
                this.lastQuantityHauled = 0;
            }

            public void performTrade()
            {
                if (lastRoute != null)
                {
                    lastRoute.buyer.quantity -= lastQuantityHauled;
                    lastRoute.seller.quantity -= lastQuantityHauled;
                    lastRoute.quantity = Math.Min(lastRoute.seller.quantity, lastRoute.buyer.quantity);
                }
            }

            public void revertTrade()
            {
                if (lastRoute != null)
                {
                    lastRoute.buyer.quantity += lastQuantityHauled;
                    lastRoute.seller.quantity += lastQuantityHauled;
                    lastRoute.quantity = Math.Min(lastRoute.seller.quantity, lastRoute.buyer.quantity);
                }
            }

            public void revertTradesToStart()
            {
                PlannerState s = this;
                while (s.parent != null)
                {
                    s.revertTrade();
                    s = s.parent;
                }
            }

            public void performTradesFromStart()
            {
                PlannerState s = this;
                //trades are commutitive so it's fine that we perform them in reverse chron order.
                while (s.parent != null)
                {
                    s.performTrade();
                    s = s.parent;
                }
            }

            public int computeOptimalQuantity(TradeRouteMiner.Route route)
            {
                int maxBuyQuantity = (int)(wallet / route.seller.price);
                int maxCarryQuantity = (int)(cargoCapacity / route.seller.item.volume);
                //this is how many of the things we can buy and carry
                int maxPotentialQuantity = Math.Min(maxBuyQuantity, maxCarryQuantity);
                //ensure we dont try to carry more things than there are to buy / sell
                return Math.Min(maxPotentialQuantity, route.quantity);
            }

            public double computeProfitRate(TradeRouteMiner.Route route)
            {
                //return profit per jump
                return (double)(computeOptimalQuantity(route)) * route.profitPerItem / jumpElapsedCost(route.jumps);
            }

            public List<PlannerState> expandStates(TradeRouteMiner trm, UniverseGraph graph)
            {

                //synchronise our idea of history with market state
                performTradesFromStart();

                List<PlannerState> expandedStates = new List<PlannerState>();

                //expand no-trade one-jump moves to neighbouring systems
                List<UniverseGraph.SystemId> neighbours = graph.systemEdges[graph.nameToId[sysName]];
                foreach (UniverseGraph.SystemId nid in neighbours)
                {
                    expandedStates.Add(new PlannerState(this,graph.idToInfo[nid].name));
                }
                // for each trade route destination, expand the most profitable route to that destination
                // to do this, construct a dictionary of (route,profitRate) pairs indexed by dest
                Dictionary<string, KeyValuePair<TradeRouteMiner.Route,double>> bestRouteToDest
                    = new Dictionary<string, KeyValuePair<TradeRouteMiner.Route,double>>();

                //check if there are any decent trade routes from the current location
                UniverseGraph.SystemId ourSystemID = graph.nameToId[sysName];
                if(trm.systemRoutes.ContainsKey(ourSystemID))
                {

                    //Console.WriteLine("[begin expand trade routes]");

                    //determine best route to each dest
                    foreach (TradeRouteMiner.Route r in trm.systemRoutes[ourSystemID])
                    {
                        //Console.WriteLine("+ " + r);
                        string destName = r.buyer.location.name;
                        double profitRate = computeProfitRate(r);
                        if ((!bestRouteToDest.ContainsKey(destName)) || (profitRate > bestRouteToDest[destName].Value))
                        {
                            bestRouteToDest[destName] =
                                new KeyValuePair<TradeRouteMiner.Route, double>(r,profitRate);
                        }
                    }
                    //expand states along best route to each dest
                    foreach (string destName in bestRouteToDest.Keys)
                    {
                        KeyValuePair<TradeRouteMiner.Route,double> pair = bestRouteToDest[destName];
                        //Console.WriteLine("+{"+destName+"} " + pair.Key + " @ profit/jump " + pair.Value);
                        expandedStates.Add(new PlannerState(this,
                            pair.Key,
                            computeOptimalQuantity(pair.Key)));
                        //Console.WriteLine("++ opt quantity = " + computeOptimalQuantity(pair.Key));
                    }

                    //Console.WriteLine("[end expand trade routes]");
                    //Console.ReadLine();
                }

                //restore market state
                revertTradesToStart();


                return expandedStates;
            }
        }

        private PriorityQueue<double, PlannerState> open;
        

        ////we can track the best profit attained for each encountered space,time pair
        ////and use this to prune excess nodes
        private SpaceTimeProfitCache closed;

        public TradeRouteMiner trm;
        public UniverseGraph graph;


        public TradePlanner(TradeRouteMiner trm, UniverseGraph graph)
        {
            this.trm = trm;
            this.graph = graph;
        }

        public double getPriority(PlannerState state, double bestProfitRate, int maxJumps)
        {
            //note here we're implicitly heavily penalising plans that go over max jumps
            return state.wallet + bestProfitRate * (maxJumps - state.jumpsElapsed);
        }

        public PlannerState computePlan(double initialWallet,
            double initialCargoCap,
            string initialSystemName,
            int maxJumps)
        {
            //reset lists
            this.open = new PriorityQueue<double, PlannerState>();
            this.closed = new SpaceTimeProfitCache();



            //compute a heuristic for the max possible profit rate
            double bestProfitRate = 0.0;
            double candidateProfitRate = 0.0;
            foreach(List<TradeRouteMiner.Route> lr in trm.systemRoutes.Values)
            {
                foreach (TradeRouteMiner.Route r in lr)
                {
                    candidateProfitRate = Math.Min(r.profitDensityRate * initialCargoCap, r.bulkProfitRate);
                    bestProfitRate = Math.Max(bestProfitRate, candidateProfitRate);
                }
            }

            //hence we can order PlannerStates by state.wallet + bestProfitRate*(maxJumps-state.jumpsElapsed)
            //and use a*

            Console.WriteLine("best profit rate heuristic : " + bestProfitRate);


            //establish the initial state
            PlannerState initialState = new PlannerState(initialCargoCap, initialWallet, initialSystemName);
            open.push(getPriority(initialState, bestProfitRate, maxJumps), initialState);

            //stat tracking
            int statTouchedStates = 0;
            int statAddedStates = 0;
            int progressUpdateFrequency = 10000;

            //use a* to find the optimal plan up to max specified number of jumps
            while (open.count() > 0)
            {
                PriorityQueue<double, PlannerState>.Pair pair = open.pop();
                ++statTouchedStates;


                //Console.WriteLine("!tradeplanner : open count = " + open.count()
                //    + "; jumpsElapsed = " + pair.value.jumpsElapsed
                //    + "; priority = "+pair.key);

                if (statTouchedStates % progressUpdateFrequency == 0)
                {
                    //display search progress update for user
                    Console.WriteLine("TradePlanner Stats : WORKING");
                    Console.WriteLine("\ttouched states = " + statTouchedStates);
                    Console.WriteLine("\tadded states = " + statAddedStates);
                    Console.WriteLine("\tstates in open list = " + open.count());
                    Console.WriteLine("\thighest priority = " + pair.key);
                    Console.WriteLine("\trecent jumps elapsed = " + pair.value.jumpsElapsed);
                }
                
                //check for stopping condition
                if (pair.value.jumpsElapsed >= maxJumps)
                {
                    Console.WriteLine("TradePlanner Stats : FINISHED!");
                    Console.WriteLine("\ttouched states = " + statTouchedStates);
                    Console.WriteLine("\tadded states = " + statAddedStates);
                    Console.WriteLine("\tstates in open list = " + open.count());
                    Console.WriteLine("\thighest priority = " + pair.key);
                    Console.WriteLine("\trecent jumps elapsed = " + pair.value.jumpsElapsed);
                    return pair.value;
                }

                //otherwise, expand planner state and pop children back into the open list
                foreach (PlannerState childState in pair.value.expandStates(trm, graph))
                {
                    //only add the child state if it gives us an improved profit upon what we've already seen
                    if(closed.achievesBetterProfit(childState.sysName, childState.jumpsElapsed, childState.wallet))
                    {
                        open.push(getPriority(childState,bestProfitRate, maxJumps), childState);
                        ++statAddedStates;
                    }
                }

            }

            //we've failed, for some reason, to find a plan
            return null;
        }

        public void dumpPlanToConsole(PlannerState final)
        {
            Console.WriteLine("UGLY REVERSE TIME PLAN DUMP");
            PlannerState s = final;

            while (s.parent != null)
            {
                if (s.lastRoute == null)
                {
                    Console.WriteLine("\tEMPTY RUN FROM "+s.parent.sysName+" TO "+s.sysName);
                }
                else
                {
                    Console.WriteLine("\tquantity = " + s.lastQuantityHauled + "; route = " + s.lastRoute.ToString());
                }
                s = s.parent;
            }
        }

        public void nicelyDumpPlanToConsole(PlannerState final)
        {
            Console.WriteLine("TRADE PLAN");
            //reverse the ordering
            List<PlannerState> states = new List<PlannerState>();
            PlannerState s = final;
            while (s.parent != null)
            {
                states.Insert(0, s);
                s = s.parent;
            }
            //print them nicely
            for (int i = 0; i < states.Count; ++i)
            {
                PlannerState si = states[i];
                string header = "[" + (i + 1) + "] " + si.parent.sysName + " --- ";
                string footer = " --> " + si.sysName + " ~ ";
                if (si.lastRoute == null)
                {
                    Console.WriteLine(header + "empty" + footer+"1 jump;");
                }
                else
                {
                    Console.WriteLine(header + si.lastQuantityHauled + " x " + si.lastRoute.seller.item.name
                        + footer+si.lastRoute.jumps+" jump(s);");
                    Console.WriteLine("\t SELL ORDER = " + si.lastRoute.seller);
                    Console.WriteLine("\t  BUY ORDER = " + si.lastRoute.buyer);
                }
                Console.WriteLine("\tWallet = "+si.wallet);
            }
        }
    }
}
