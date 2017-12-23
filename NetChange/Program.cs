using System;
using System.Text;

namespace NetChange
{
    class Program
    {
        static Node thisNode;

        static void Main(string[] args)
        {
            //currentProcess' own portnumber
            int ep = int.Parse(args[0]);
            thisNode = new Node(ep);
            
            //Read the other arguments, wich are the portnumbers of our neighbours
            for (int i = 1; i < args.Length; i++)
            {
                // neigbour portnumber
                int bmi = int.Parse(args[i]);
                if (ep > bmi)
                {
                    thisNode.Connect(bmi);
                }
            }

            Console.Title = "NetChange " + ep;

            //Do cmdline processing on default thread.
            while (thisNode != null)
            {
                processInput();
            }
        }

        static void processInput()
        {
            int port;
            //Read input argument
            char c = (char)Console.Read();
            Console.Read();
            switch (c)
            {
                case 'R'://Print the routing table
                    lock (thisNode)
                        Console.Write(thisNode.RoutingTable.ToString());
                    break;
                case 'B'://Send message to the process running on a port
                    port = int.Parse(readArg());
                    string message = Console.ReadLine();
                    thisNode.SendMessage(port, message);
                    return;
                case 'C'://Connect to a new process on a port
                    port = int.Parse(readArg());
                    thisNode.Connect(port);
                    break;
                case 'D'://Disconnect from the process on a port
                    port = int.Parse(readArg());
                    thisNode.CmdDisconnect(port);
                    break;
                case 'Q'://Quit the program.
                    thisNode.Quit();
                    thisNode = null; //Causes while to end and program to terminate.
                    return;
            }
            Console.ReadLine();
        }

        //Method used to read the rest of the arguments after the first character.
        static string readArg()
        {
            StringBuilder result = new StringBuilder();
            char next = (char)Console.Read();
            while (next > ' ')
            {
                result.Append(next);
                next = (char)Console.Read();
            }
            return result.ToString();
        }
    }
}
