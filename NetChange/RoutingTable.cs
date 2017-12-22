using System;
using System.Collections.Generic;
using System.Text;

namespace NetChange
{
    class RoutingTable
    {
        Node myNode;

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

        public Dictionary<int, int> PrefNb
        {
            get { return prefNb; }
        }

        public RoutingTable(Node node, int ourPortNr)
        {
            myNode = node;
            this.ourPortNr = ourPortNr;
            D = new Dictionary<int, int>();
            prefNb = new Dictionary<int, int>();
            nbDist = new Dictionary<Tuple<int, int>, int>();
            Recompute(ourPortNr);
        }

        public void AddNeighbour(int nbPort)
        {
            nbDist[Tuple.Create(nbPort, nbPort)] = 0;
            nbDist[Tuple.Create(nbPort, ourPortNr)] = 1;
            Recompute(nbPort);

            foreach(KeyValuePair<int, int> kvp in D)
                myNode.Broadcast("!mydist " + kvp.Key + " " + kvp.Value);
        }

        public void RemoveNeighbour(int nbPort)
        {
            Console.WriteLine("//Removing Neighbour");

            List<Tuple<int, int>> toRemove = new List<Tuple<int, int>>();
            foreach (Tuple<int, int> key in nbDist.Keys)
                if (key.Item1 == nbPort)
                    toRemove.Add(key);

            foreach (Tuple<int, int> key in toRemove)
            {
                nbDist.Remove(key);
                Recompute(key.Item2);
            }
                

            nbDist.Remove(Tuple.Create(ourPortNr, nbPort));

            Recompute(nbPort);
        }

        public void Update(int from, int to, int newDist)
        {
            nbDist[Tuple.Create(from, to)] = newDist;
            //nbDist[Tuple.Create(to, from)] = newDist;
            Recompute(to);
            //Recompute(from);
        }

        private void Recompute(int v)
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

                if (closest == -1)
                {
                    Console.WriteLine("Onbereikbaar: " + v);
                    D.Remove(v);
                    prefNb.Remove(v);
                }
                else if (!D.ContainsKey(v) || !prefNb.ContainsKey(v) || D[v] != closestD + 1 || prefNb[v] != closest)
                {
                    prefNb[v] = closest;
                    D[v] = closestD + 1;

                    Console.WriteLine("Afstand naar " + v + " is nu " + D[v] + " via " + prefNb[v]);
                    myNode.Broadcast("!mydist " + v + " " + D[v]);
                }
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
