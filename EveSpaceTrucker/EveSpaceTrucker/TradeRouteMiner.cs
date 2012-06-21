using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EveSpaceTrucker
{
    public class TradeRouteMiner
    {
        const double TAX_RATE = 0.01;
        public class Route
        {
            public MarketDatabase.Order seller, buyer;
            public int jumps, quantity;
            public double profitDensityRate, bulkProfitRate, profitPerItem;

            public override string ToString()
            {
                return "SELLER=" + seller + "; BUYER=" + buyer + "; jumps=" + jumps;
            }
        }

        public struct Constraints
        {
            public double minProfitDensityRate, minBulkProfitRate;
        }


        public Pathfinder pather;
        public MarketDatabase market;
        public ItemDatabase items;


        public Dictionary<UniverseGraph.SystemId, List<Route>> systemRoutes;

        //stores user constraints
        Constraints constraints;

        public int addedRoutes;

        public TradeRouteMiner(Pathfinder pather, MarketDatabase market, ItemDatabase items)
        {
            this.pather = pather;
            this.market = market;
            this.items = items;

            systemRoutes = new Dictionary<UniverseGraph.SystemId, List<Route>>();

            addedRoutes = 0;
        }

        public void setConstraints(Constraints c)
        {
            constraints = c;
        }

        private void considerRoute(MarketDatabase.Order sellOrder, MarketDatabase.Order buyOrder)
        {
            Route r = new Route(); 
            r.profitPerItem = buyOrder.price * (1.0 - TAX_RATE) - sellOrder.price;

            if (r.profitPerItem<=0.0)
                return;

            double volumePerItem = items.idToEntry[sellOrder.item.typeId].volume;

            //int outlayQuantityBound = (int)(constraints.maxOutlay / sellOrder.price);
            //int volumeQuantityBound = (int)(constraints.maxVolume / volumePerItem);
            //int constraintsQuantityBound = Math.Min(outlayQuantityBound, volumeQuantityBound);
            
            r.seller = sellOrder;
            r.buyer = buyOrder;
            r.jumps = pather.getDistance(sellOrder.location, buyOrder.location);
            //negative jumps indicates no path found
            if (r.jumps < 0)
                return;
            r.quantity = Math.Min(sellOrder.quantity, buyOrder.quantity);
            r.profitDensityRate = r.profitPerItem / (volumePerItem * Math.Max(r.jumps, 1));
            r.bulkProfitRate = r.profitPerItem * r.quantity / Math.Max(r.jumps, 1);

            if ((r.profitDensityRate > constraints.minProfitDensityRate)
                && (r.bulkProfitRate > constraints.minBulkProfitRate))
            {
                systemRoutes[pather.graph.nameToId[r.seller.location.name]].Add(r);
                addedRoutes++;
            }
        }

        public void computeRoutes()
        {
            foreach (string itemId in market.sellers.Keys)
            {
                //skip if there are no records of buyers in market db
                if(!market.buyers.ContainsKey(itemId))
                    continue;

                Console.WriteLine(" miner : analysing " + items.idToEntry[itemId].name);
                foreach (MarketDatabase.Order sellOrder in market.sellers[itemId])
                {
                    //Console.WriteLine("~~ miner : analysing seller " + sellOrder);
                    //Console.WriteLine("~~ miner : " + pather.CacheStats);
                    foreach (MarketDatabase.Order buyOrder in market.buyers[itemId])
                    {
                        UniverseGraph.SystemId sid = pather.graph.nameToId[sellOrder.location.name];
                        if (!systemRoutes.ContainsKey(sid))
                            systemRoutes[sid] = new List<Route>();

                        considerRoute(sellOrder, buyOrder);
                    }
                }

            }
        }
    }
}
