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

            routingTable = new RoutingTable(portNr);

            TcpListener listener = new TcpListener(IPAddress.Any, portNr);
            listener.Start();
            new Thread(() => AcceptConnections(listener)).Start();
        }

        public RoutingTable RoutingTable
        {
            get { return routingTable; }
        }

        private void NewConnection(Connection c)
        {
            lock (nbConns)
                nbConns.Add(c.Port, c);

            lock (routingTable)
                routingTable.AddNeighbour(c.Port);

            Console.WriteLine("Verbonden: " + c.Port);
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

            NewConnection(c);

            c.SendMessage("MyPort: " + routingTable.OurPortNr);
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
                Console.WriteLine("Verbroken: " + c.Port);
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

                
                int theirPort = int.Parse(clientIn.ReadLine().Split()[1]);
                //Console.WriteLine("//Accepting connection from " + theirPort);

                // Start reading
                Connection c = new Connection(clientIn, clientOut, theirPort);
                c.Thread = new Thread(() => ProcessMessages(c));
                c.Thread.Start();

                NewConnection(c);
            }
        }
    }
}
