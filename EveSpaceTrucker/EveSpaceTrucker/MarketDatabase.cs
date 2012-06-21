using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EveSpaceTrucker
{
    public class MarketDatabase
    {
        public class Order
        {
            public ItemDatabase.Entry item;
            public double price;
            public UniverseGraph.SystemInfo location;
            public int quantity, minQuantity;
            public int orderId;

            public Order(ItemDatabase.Entry item,
                double price,
                UniverseGraph.SystemInfo location,
                int quantity,
                int minQuantity,
                int orderId)
            {
                this.item = item;
                this.price = price;
                this.location = location;
                this.quantity = quantity;
                this.minQuantity = minQuantity;
                this.orderId = orderId;
            }

            public override string ToString()
            {
                //return "{ORDR_"+orderId+" "+item.name + " : $" + price + " X " + quantity + " (" + minQuantity + ") at " + location.name;
                return "{" + item.name + " $" + price + "x" + quantity + "(" + minQuantity + ") at " + location.name+"}";
            }
        }
        public Dictionary<string,List<Order>> sellers, buyers;

        UniverseGraph systems;
        ItemDatabase items;

        public MarketDatabase(UniverseGraph systems, ItemDatabase items)
        {
            this.systems = systems;
            this.items = items;

            sellers = new Dictionary<string, List<Order>>();
            buyers = new Dictionary<string, List<Order>>();
        }

        public void parseMarketDumpFile(string path)
        {
            StreamReader reader = File.OpenText(path);
            string line = reader.ReadLine();
            //ignore header line
            line = reader.ReadLine();
            while (line != null)
            {
                addMarketDumpLine(line);
                line = reader.ReadLine();
            }
            reader.Close();
        }

        public void addMarketDumpLine(string line)
        {
            char[] splitChars = { ',' };
            string[] tokens = line.Split(splitChars);
            if (tokens.Length != 15)
            {
                //Console.WriteLine("market - skipping line with incorrect token count");
                return;
            }

            const int TOK_PRICE = 0,
                TOK_QUANTITY = 1,
                TOK_ITEM_TYPE = 2,
                TOK_ORDER_ID = 4,
                TOK_MIN_QUANTITY = 6,
                TOK_IS_SELL_ORDER = 7,
                TOK_SYSTEM_ID = 12;

            double price = Double.Parse(tokens[TOK_PRICE]);
            int quantity = (int)Double.Parse(tokens[TOK_QUANTITY]);
            string itemType = tokens[TOK_ITEM_TYPE];
            int orderId = (int)Int32.Parse(tokens[TOK_ORDER_ID]);
            int minQuantity = (int)Double.Parse(tokens[TOK_MIN_QUANTITY]);
            bool isSellOrder = bool.Parse(tokens[TOK_IS_SELL_ORDER]);
            UniverseGraph.SystemId location = systems.parseSystemId(tokens[TOK_SYSTEM_ID]);

            //ignore if we dont know the system
            if (!systems.idToInfo.ContainsKey(location))
            {
                //Console.WriteLine("market - ignoring unknown system id: "+location);
                return;
            }

            if (!items.idToEntry.ContainsKey(itemType))
            {
                //Console.WriteLine("market - ignoring unknown item id: " + itemType);
                return;
            }


            Order order = new Order(items.idToEntry[itemType],
                price,
                systems.idToInfo[location],
                quantity,
                minQuantity,
                orderId);

            if (!filterOrder(order))
            {
                //Console.WriteLine("market - entry fails filter");
                return;
            }

            Dictionary<string, List<Order>> target;

            if (!isSellOrder)
                target = sellers;
            else
                target = buyers;

            if(!target.ContainsKey(itemType))
                target[itemType] = new List<Order>();

            target[itemType].Add(order);

            //Console.WriteLine("market - entry added!");

        }

        //ignore market hits for lowsec
        public bool filterOrder(Order o)
        {
            const float minSecurity = 0.5f;

            const double minVolume = 0.0;

            if (o.location.security < minSecurity)
                return false;

            if (o.quantity * o.item.volume < minVolume)
                return false;

            return true;

        }
    }
}
