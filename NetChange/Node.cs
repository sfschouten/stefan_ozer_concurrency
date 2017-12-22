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

            routingTable = new RoutingTable(this, portNr);

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
            
            c.SendMessage("!myport: " + routingTable.OurPortNr);

            NewConnection(c);
        }

        /// <summary>
        /// Disconnect from process that is listening on portNr
        /// </summary>
        public void CmdDisconnect(int portNr)
        {
            if (!nbConns.ContainsKey(portNr))
            {
                Console.WriteLine("Poort " + portNr + " is niet bekend");
                return;
            }

            Console.WriteLine("Verbroken: " + portNr);

            nbConns[portNr].Closing = true;

            nbConns[portNr].Close();
            nbConns[portNr].Thread.Join();
            

            Disconnect(portNr);
        }

        private void Disconnect(int portNr)
        {
            lock (nbConns)
                nbConns.Remove(portNr);

            lock (routingTable)
                routingTable.RemoveNeighbour(portNr);
        }

        private void ProcessMessages(Connection c)
        {
            try
            {
                while (!c.Closing)
                {
                    string line = c.Read.ReadLine();
                    //Console.WriteLine("//" + line);

                    if (line.StartsWith("!"))
                    {
                        string[] args = line.Split(' ');

                        //Process updates from neighbours
                        switch (args[0])
                        {
                            case "!mydist":
                                int to = int.Parse(args[1]);
                                int newDist = int.Parse(args[2]);
                                lock (routingTable)
                                    routingTable.Update(c.Port, to, newDist);
                                break;
                            case "!msg":
                                int rcvr = int.Parse(args[1]);
                                if (routingTable.OurPortNr == rcvr)
                                {
                                    Console.WriteLine(args[2]);
                                }
                                else
                                {
                                    int prefNb = routingTable.PrefNb[rcvr];
                                    Console.WriteLine("Bericht voor " + rcvr + " doorgestuurd naar " + prefNb);
                                    SendMessage(rcvr, args[2], prefNb);
                                }
                                break;
                            default:
                                Console.WriteLine("//Shouldn't see this.");
                                break;
                        }
                    }
                }
            }
            catch (Exception e) // Connection broken
            {
                Disconnect(c.Port);
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
        
        public void Broadcast(string msg)
        {
            lock(nbConns)
                foreach (Connection c in nbConns.Values)
                    c.SendMessage(msg);
        }

        public void SendMessage(int port, string msg)
        {
            int prefNb = routingTable.PrefNb[port];
            SendMessage(port, msg, prefNb);
        }

        private void SendMessage(int port, string msg, int via)
        {
            nbConns[via].SendMessage("!msg " + port + " " + msg);
        }
    }
}
