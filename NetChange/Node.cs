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
        
        public Node(int portNr)
        {
            nbConns = new Dictionary<int, Connection>();

            routingTable = new RoutingTable();

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
            Connection c = null;
            while (c == null)
            {
                try
                {
                    c = new Connection(portNr);
                }
                catch (SocketException e)
                {
                    Thread.Sleep(5);
                }
            }

            // Start reading
            c.Thread = new Thread(() => ProcessMessages(c));
            c.Thread.Start();

            lock(nbConns)
                nbConns.Add(portNr, c);
        }

        /// <summary>
        /// Disconnect from process that is listening on portNr
        /// </summary>
        public void Disconnect(int portNr)
        {
            nbConns[portNr].Thread.Abort();
            nbConns[portNr].Close();

            lock (nbConns)
                nbConns.Remove(portNr);
        }

        private void ProcessMessages(Connection c)
        {
            try
            {
                while (true)
                {
                    string line = c.Read.ReadLine();
                    Console.WriteLine(line);
                    
                    //Process updates from neighbours
                    if (line[0] == '<')
                    {
                        switch (line[1])
                        {
                            case 'd':
                                //routingTable
                                break;
                        }
                    }
                }
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

                lock (nbConns)
                    nbConns.Add(theirPort, c);
            }
        }
    }
}
