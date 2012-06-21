Eve Space Trucker
=================

> Come on, come on, come on, Let's go space trucking

_Deep Purple_


### Introduction

Eve Space Trucker is a utility to plan profitable trade routes for the game
[Eve Online](http://en.wikipedia.org/wiki/Eve_Online). It was written in
C# some time around the start of 2009.

Eve Online has a large and complex player-driven economy, and there are many
ways for players to try and make a buck. One of the less exciting (and as it
turned out, less profitable) ways is to haul cargo between star systems,
buying low and selling high. This utility partly automates the more interesting
aspect of hauling cargo - it identifies profitable trades, and sequences them
into profitable cargo routes, to maximise profit per number of jumps. The
planned routes are constrained by the maximum number of jumps, initial capital,
initial star system, and available cargo capacity.

The player is left with the tasks of exporting market data from the game and
feeding it to the planner, flying the ship, and buying and selling the goods
at each port. It didn't take me long to figure out that I'd had much more
fun writing the code for the planner than actually playing the game.

### State of the code

I was pretty surprised to find a copy of this code lying about.
Looking back over it now, after 3.5 years, I can make a few observations:

1.  I wrote this before I learned to use version control, so there are patches of
    unused code or code that has been commented out. With version control
    this scar-tissue code could be kept in an older revision (or another branch)
    and then deleted.
2.  All of the paths to input data files are hard coded.
3.  In some places the comments are sparse, but there are a lot more comments
    in the trickier parts of the code. This doesn't seem so bad.
4.  The code features much heavier use of mutable state and objects than I tend
    to use these days. Now I prefer to write things in a functional programming
    style, except where concessions need to be made for performance reasons.
5.  I haven't written any C# code since this program, so I have no idea how
    idiomatic this code is. Probably not very!
6.  Since I wrote the code, I wouldn't be surprised if the formats of the
    Eve data files required by this program have changed completely. No
    idea if it still works.


### What the program does

There are three phases : data import, construction of the trade route graph, and
finally searching the trade route graph to determine the best sequence of trades.

*   Data Import
    1.  Read in the graph of solar systems, and their connectivity via jump gates. This
        defines the topology of the universe we're going to trade in.
    2.  Discard solar systems and jumps with low security ratings. This prevents the planner
        from suggesting shortcuts through bad neighbourhoods where our industrial ship
        would be an easy target for pirates. This adjusts the initial topology.
    3.  Read in the database of item types. This defines the volume of every commodity
        we may consider trading. It is important to consider the volume of cargo when
        trading since even pretend internet spaceships have limited cargo capacity.
    4.  Now we read in 1 or more files of exported market data. Each market data file
        lists the buy and sell orders for a single commodity in a single region. A
        region is a subset of solar systems. This data populates a market database.
*   Construction of trade route graph
    1.  We loop over each commodity that has both buy and sell orders, and compute
        the profit per jump, and the profit per (jump * item), where the number
        of jumps is determined by finding the distance of a shortest path from
        the location of the sell order to the location of the buy order.
    2.  To reduce the number of routes that need to be considered, routes
        that have poor profit per jump or profit per (jump * item) are discarded.
    3.  We end up with a graph of viable trade routes between pairs of systems.
        Each trade route stores information about the buy and sell prices,
        quantity and volume of cargo, and distance.
*   Trade route planning
    1.  Parses input parameters for the initial capital, cargo capacity,
        starting system, and planning time horizon (measured in number of jumps).
    2.  We search over the graph of trade routes using `A*`. `A*` is usually used in
        pathfinding, where the idea is to expand paths in increasing order of
        a lower bound on the total path length from the start location to some
        goal location. The difference here is that instead of minimising
        distance, we try to maximise net profit / net jumps. So we expand
        trade routes in decreasing order of an upper bound on the total
        profit.
    3.  When searching, the planner has to ensure that the market database
        is consistent with the history of trades already performed for the
        current trade route under consideration. Changes to the state of
        the market resulting from buying and selling commodities earlier
        in the trade route have to be tracked as part of the state when
        planning.
    4.  After finding the best route, the sequence of actions required to
        carry out the route is displayed.

