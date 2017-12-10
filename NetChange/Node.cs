using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetChange
{
    class Node
    {
        RoutingTable routingTable;

        //Connections to neighbours
        Dictionary<int, Connection> nbConns;

        //Estimate of distance from here to key
        Dictionary<int, int> D;

        //Prefered neighbours
        Dictionary<int, int> prefNb;

        //This node's knowledge about distance from key.i1 to key.i2
        Dictionary<Tuple<int, int>, int> nbDist;

        public Node(int portNr)
        {
            nbConns = new Dictionary<int, Connection>();
            D = new Dictionary<int, int>();
            prefNb = new Dictionary<int, int>();
            nbDist = new Dictionary<Tuple<int, int>, int>();

            TcpListener listener = new TcpListener(IPAddress.Any, portNr);
            listener.Start();
            new Thread(() => AcceptConnections(listener)).Start();
        }

        public RoutingTable RoutingTable
        {
            get { return routingTable; }
        }

        /// <summary>
        /// Connect with process that is listening on portNr
        /// </summary>
        public void Connect(int portNr)
        {
            Connection c = new Connection(portNr);

            // Start reading
            c.Thread = new Thread(() => ProcessMessages(c));
            c.Thread.Start();

            nbConns.Add(portNr, c);
        }

        /// <summary>
        /// Disconnect from process that is listening on portNr
        /// </summary>
        public void Disconnect(int portNr)
        {
            nbConns[portNr].Thread.Abort();
            nbConns[portNr].Close();
            nbConns.Remove(portNr);
        }

        private void ProcessMessages(Connection c)
        {
            try
            {
                while (true)
                    Console.WriteLine(c.Read.ReadLine());
            }
            catch // Connection broken
            { 

            } 
        }

        private void AcceptConnections(TcpListener handle)
        {
            while (true)
            {
                TcpClient client = handle.AcceptTcpClient();
                StreamReader clientIn = new StreamReader(client.GetStream());
                StreamWriter clientOut = new StreamWriter(client.GetStream());
                clientOut.AutoFlush = true;

                // 
                int theirPort = int.Parse(clientIn.ReadLine().Split()[1]);

                //Console.WriteLine("Client maakt verbinding: " + zijnPoort);

                // Start reading
                Connection c = new Connection(clientIn, clientOut);
                c.Thread = new Thread(() => ProcessMessages(c));
                c.Thread.Start();
                nbConns.Add(theirPort, c);
            }
        }
    }
}
