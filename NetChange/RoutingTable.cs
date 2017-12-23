using System;
using System.Collections.Generic;
using System.Text;

namespace NetChange
{
    class RoutingTable
    {
        //The node that this routingtable belongs to.
        Node myNode;

        //Estimate of distance from here to key
        Dictionary<int, int> D;

        //Prefered neighbours
        Dictionary<int, int> prefNb;

        //This node's knowledge about distance from key.i1 to key.i2
        Dictionary<Tuple<int, int>, int> nbDist;

        int ourPortNr;
        
        public RoutingTable(Node node, int ourPortNr)
        {
            myNode = node;
            this.ourPortNr = ourPortNr;
            D = new Dictionary<int, int>();
            prefNb = new Dictionary<int, int>();
            nbDist = new Dictionary<Tuple<int, int>, int>();
            Recompute(ourPortNr);
        }

        public int OurPortNr
        {
            get { return ourPortNr; }
        }
        public Dictionary<int, int> PrefNb
        {
            get { return prefNb; }
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
            //Remove all distances that are from the neighbour.
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
            Recompute(to);
        }

        private void Recompute(int v)
        {
            if (v == ourPortNr)
            { //If this is us, distance is 0
                D[v] = 0;
                prefNb[v] = -2;
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
                        if (d < D.Count && d < closestD)
                        {
                            closest = kvp.Key.Item1;
                            closestD = d;
                        }
                    }
                }

                int newD = closestD + 1; //If nothing found will overflow to int.MinValue
                
                if (!D.ContainsKey(v) || !prefNb.ContainsKey(v) || (newD > 0 && D[v] != newD) || prefNb[v] != closest)
                {
                    prefNb[v] = closest;

                    if (closest == -1)
                    {
                        D[v] = D.Count;
                        Console.WriteLine("Onbereikbaar: " + v);
                    }
                    else
                    {
                        D[v] = newD;
                        Console.WriteLine("Afstand naar " + v + " is nu " + D[v] + " via " + prefNb[v]);
                    }
                    
                    myNode.Broadcast("!mydist " + v + " " + D[v]);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach(KeyValuePair<int, int> kvp in D)
                if (prefNb[kvp.Key] != -1)
                    sb.AppendLine(kvp.Key + " " + kvp.Value + " " + (prefNb[kvp.Key] == -2 ? "local" : prefNb[kvp.Key].ToString()));

            return sb.ToString();
        }
    }
}
