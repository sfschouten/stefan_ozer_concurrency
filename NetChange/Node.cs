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
        Dictionary<int, Connection> nBconns;
        
        public Node(int portNr)
        {
            nBconns = new Dictionary<int, Connection>();

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

            nBconns.Add(portNr, c);
        }

        /// <summary>
        /// Disconnect from process that is listening on portNr
        /// </summary>
        public void Disconnect(int portNr)
        {
            nBconns[portNr].Thread.Abort();
            nBconns[portNr].Close();
            nBconns.Remove(portNr);
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
                nBconns.Add(theirPort, c);
            }
        }
    }
}
