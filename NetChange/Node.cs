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
            lock (this)
            {
                nbConns[c.Port] = c;
                routingTable.AddNeighbour(c.Port);
            }

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
            lock (this)
            {
                if (!nbConns.ContainsKey(portNr))
                {
                    Console.WriteLine("Poort " + portNr + " is niet bekend");
                    return;
                }
            }
            
            nbConns[portNr].Close();
            nbConns[portNr].Thread.Join();
        }

        private void ProcessMessages(Connection c)
        {
            try
            {
                while (!c.Closing)
                {
                    string line = "";
                    line = c.Read.ReadLine();

                    if (line.StartsWith("!"))
                    {
                        string[] args = line.Split(' ');

                        //Process updates from neighbours
                        switch (args[0])
                        {
                            case "!mydist":
                                int to = int.Parse(args[1]);
                                int newDist = int.Parse(args[2]);

                                lock (this)
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
                                    lock (this)
                                    {
                                        int prefNb = routingTable.PrefNb[rcvr];
                                        Console.WriteLine("Bericht voor " + rcvr + " doorgestuurd naar " + prefNb);
                                        SendMessage(rcvr, args[2], prefNb);
                                    }
                                }
                                break;
                            default:
                                Console.WriteLine("//Shouldn't see this.");
                                break;
                        }
                    }
                }
            }
            catch (Exception e) //when (e is IOException || e is NullReferenceException )// Connection broken
            {
                Console.WriteLine("//" + e.Message);
            }

            lock (this)
            {
                nbConns.Remove(c.Port);
                routingTable.RemoveNeighbour(c.Port);
            }
            Console.WriteLine("Verbroken: " + c.Port);
        }

        private void AcceptConnections(TcpListener handle)
        {
            while (true)
            {
                TcpClient client = handle.AcceptTcpClient();
                
                Connection c = new Connection(client);
                c.Thread = new Thread(() => ProcessMessages(c));
                c.Thread.Start();

                NewConnection(c);
            }
        }
        
        public void Broadcast(string msg)
        {
            lock(this)
                foreach (Connection c in nbConns.Values)
                    c.SendMessage(msg);
        }

        public void SendMessage(int port, string msg)
        {
            lock (this)
            {
                int prefNb = routingTable.PrefNb[port];
                SendMessage(port, msg, prefNb);
            }
        }

        private void SendMessage(int port, string msg, int via)
        {
            lock (this)
                nbConns[via].SendMessage("!msg " + port + " " + msg);
        }
    }
}
