using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetChange
{
    class RoutingTable
    {
        //Estimate of distance from here to key
        Dictionary<int, int> D;

        //Prefered neighbours
        Dictionary<int, int> prefNb;

        //This node's knowledge about distance from key.i1 to key.i2
        Dictionary<Tuple<int, int>, int> nbDist;

        public RoutingTable()
        {
            D = new Dictionary<int, int>();
            prefNb = new Dictionary<int, int>();
            nbDist = new Dictionary<Tuple<int, int>, int>();
        }

        public void Recompute(int v, int u)
        {
            if (v == u)
            {
                D[v] = 0;
                prefNb[v] = -1;
            }
            else
            {
                int closest = -1;
                int closestD = int.MaxValue;
                foreach (KeyValuePair<Tuple<int, int>, int> kvp in nbDist)
                {
                    if (kvp.Key.Item2 == v)
                    {
                        int d = kvp.Value;
                        if (d < closestD)
                        {
                            closest = kvp.Key.Item1;
                            closestD = d;
                        }
                    }
                }

                prefNb[v] = closest;
                D[v] = nbDist[new Tuple<int, int>(closest, v)] + 1;
            }
        }

        public override string ToString()
        {
            return "";
        }
    }
}
