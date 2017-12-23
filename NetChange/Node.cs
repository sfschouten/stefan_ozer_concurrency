using System;
using System.Collections.Generic;
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

        bool acceptingConnections = true;
        //thread accepting connections
        Thread acceptingThread;

        public Node(int portNr)
        {
            //initialise Dictionary that holds neigbour connections
            nbConns = new Dictionary<int, Connection>();

            //initialise routingtable
            routingTable = new RoutingTable(this, portNr);

            //create listener and start it
            TcpListener listener = new TcpListener(IPAddress.Any, portNr);
            listener.Start();
            
            //create thread and start thread
            acceptingThread = new Thread(() => AcceptConnections(listener));
            acceptingThread.Start();
        }

        public RoutingTable RoutingTable
        {
            get { return routingTable; }
        }

        /// <summary>
        /// Disconnect from all connections and accept no further connections
        /// </summary>
        public void Quit()
        {
            foreach (int p in nbConns.Keys)
                CmdDisconnect(p);

            acceptingConnections = false;
            acceptingThread.Join();
        }
        /// <summary>
        /// Add new connection
        /// </summary>
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
                    //connect with portNr
                    c = new Connection(portNr);
                }
                catch (SocketException e)
                {
                    //try again later
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
            
            //close the connection and thread
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
                            case "!mydist"://process message related to updating the routing table
                                int to = int.Parse(args[1]);
                                int newDist = int.Parse(args[2]);

                                lock (this)
                                    routingTable.Update(c.Port, to, newDist);
                                break;
                            case "!msg"://receive or forward message 
                                int rcvr = int.Parse(args[1]);
                                if (routingTable.OurPortNr == rcvr)
                                { //We are recipient: print message.
                                    Console.WriteLine(args[2]);
                                }
                                else
                                { //Message needs to be forwarded, so send the message to the preffered neighbor.
                                    lock (this)
                                    {
                                        int prefNb = routingTable.PrefNb[rcvr];
                                        Console.WriteLine("Bericht voor " + rcvr + " doorgestuurd naar " + prefNb);
                                        SendMessage(rcvr, args[2], prefNb);
                                    }
                                }
                                break;
                            default: //debug case
                                Console.WriteLine("//Shouldn't see this.");
                                break;
                        }
                    }
                }
            }
            catch (Exception e) // Connection broken
            {
               // Console.WriteLine("//" + e.Message);
            }

            lock (this)
            {
                nbConns.Remove(c.Port);
                routingTable.RemoveNeighbour(c.Port);
            }
            Console.WriteLine("Verbroken: " + c.Port);
        }

        /// <summary>
        /// Method for accepting new incoming connections
        /// </summary>
        private void AcceptConnections(TcpListener handle)
        {
            while (acceptingConnections)
            {
                TcpClient client = handle.AcceptTcpClient();
                
                // make new connection and start a new thread for this connection
                Connection c = new Connection(client);
                c.Thread = new Thread(() => ProcessMessages(c));
                c.Thread.Start();

                NewConnection(c);
            }
        }
        /// <summary>
        /// Send messages to all neighbors
        /// </summary>
        public void Broadcast(string msg)
        {
            lock(this)
                foreach (Connection c in nbConns.Values)
                    c.SendMessage(msg);
        }
        /// <summary>
        /// Send message to prefered neighbour for recipient.
        /// </summary>
        public void SendMessage(int port, string msg)
        {
            lock (this)
            {
                int prefNb = routingTable.PrefNb[port];
                SendMessage(port, msg, prefNb);
            }
        }
        /// <summary>
        /// Send message to 'port' through 'via'
        /// </summary>
        private void SendMessage(int port, string msg, int via)
        {
            lock (this)
                nbConns[via].SendMessage("!msg " + port + " " + msg);
        }
    }
}
