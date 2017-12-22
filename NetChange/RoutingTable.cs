using System;
using System.Collections.Generic;
using System.Text;

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

        int ourPortNr;
        public int OurPortNr
        {
            get { return ourPortNr; }
        }

        public RoutingTable(int ourPortNr)
        {
            this.ourPortNr = ourPortNr;
            D = new Dictionary<int, int>();
            prefNb = new Dictionary<int, int>();
            nbDist = new Dictionary<Tuple<int, int>, int>();
            Recompute(ourPortNr);
        }

        public void AddNeighbour(int nbPort)
        {
            D[nbPort] = 1;
            prefNb.Add(nbPort, nbPort);
            nbDist[Tuple.Create(nbPort, nbPort)] = 0;
            nbDist[Tuple.Create(ourPortNr, nbPort)] = 1;
            nbDist[Tuple.Create(nbPort, ourPortNr)] = 1;
            Recompute(nbPort);
        }

        public void Recompute(int v)
        {
            if (v == ourPortNr)
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
                D[v] = closestD + 1;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach(KeyValuePair<int, int> kvp in D)
                sb.AppendLine(kvp.Key + " " + kvp.Value + " " + (prefNb[kvp.Key] == -1 ? "local" : prefNb[kvp.Key].ToString()));

            return sb.ToString();
        }
    }
}
