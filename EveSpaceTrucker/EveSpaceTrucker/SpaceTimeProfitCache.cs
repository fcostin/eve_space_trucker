using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EveSpaceTrucker
{
    //maintain, per location and time, a minimum bound on the greatest amount of money we can achieve.
    public class SpaceTimeProfitCache
    {
        Dictionary<string, SortedList<double, double>> bestProfits;

        public SpaceTimeProfitCache()
        {
            bestProfits = new Dictionary<string, SortedList<double, double>>();
        }

        private void removeRedundantEntries(string location, double wallet, int index)
        {
            //if there are entries of lesser values at greater times, they are redundant
            // - so delete all the ones with greater times but no greater profit
            int post = index + 1;
            IList<double> vals = bestProfits[location].Values;
            while ((post < bestProfits[location].Count) && (vals[post] <= wallet))
            {
                bestProfits[location].RemoveAt(index+1);
            }
        }

        public bool achievesBetterProfit(string location, double time, double wallet)
        {

            if (bestProfits.ContainsKey(location))
            {
                if (bestProfits[location].ContainsKey(time))
                {
                    double existingMinWallet = bestProfits[location][time];

                    if (wallet > existingMinWallet)
                    {
                        //we achieve a strictly better profit, replace the existing entry with the new profit,
                        bestProfits[location][time] = wallet;
                        removeRedundantEntries(location, wallet, bestProfits[location].IndexOfKey(time));
                        return true;
                    }
                    else
                    {
                        //we're redundant!
                        return false;
                    }
                }
                else
                {
                    //add our new entry
                    bestProfits[location].Add(time, wallet);
                    //determine neighbours of new entry in the sorted list
                    int i = bestProfits[location].IndexOfKey(time);
                    int pre = i - 1, post = i + 1;
                    IList<double> vals = bestProfits[location].Values;
                    //if there is an entry of no lesser value at an earlier time, we are redundant
                    if ((pre >= 0) && (vals[pre] >= vals[i]))
                    {
                        bestProfits[location].RemoveAt(i);
                        return false;
                    }
                    else
                    {
                        //if there are entries of lesser values at greater times, they are redundant
                        removeRedundantEntries(location, wallet, i);
                        return true;
                    }
                }
            }
            else
            {
                //we're the first bit of information for this location, so start a new list to
                //cache profit keyed by time
                bestProfits[location] = new SortedList<double, double>();
                bestProfits[location].Add(time, wallet);
                return true;
            }
        }

        public override string ToString()
        {
            //this is probably really slow for large caches, as strings are most likely invariant
            //but who cares only use this for debugging!
            string res = "(";
            foreach (string location in bestProfits.Keys)
            {
                res += location + ":[";
                IList<double> keys = bestProfits[location].Keys;
                IList<double> vals = bestProfits[location].Values;
                for (int i = 0; i < keys.Count; ++i)
                    res += keys[i].ToString() + ":" + vals[i].ToString()+",";
                res += "],";
            }
            return res + ")";
        }
    }
}
