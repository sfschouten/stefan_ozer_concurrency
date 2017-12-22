using System;
using System.Text;

namespace NetChange
{
    class Program
    {
        static Node thisNode;

        static void Main(string[] args)
        {
            int ep = int.Parse(args[0]);
            thisNode = new Node(ep);
            
            for (int i = 1; i < args.Length; i++)
            {
                int bmi = int.Parse(args[i]);
                if (ep > bmi)
                {
                    thisNode.Connect(bmi);
                }
            }

            Console.Title = "NetChange " + ep;

            while (true)
            {
                processInput();
            }
        }

        static void processInput()
        {
            int port;
            char c = (char)Console.Read();
            Console.Read();
            switch (c)
            {
                case 'R':
                    Console.Write(thisNode.RoutingTable.ToString());
                    break;
                case 'B':
                    port = int.Parse(readArg());
                    string message = readArg();

                    break;
                case 'C':
                    port = int.Parse(readArg());
                    thisNode.Connect(port);
                    break;
                case 'D':
                    port = int.Parse(readArg());
                    thisNode.Disconnect(port);
                    break;
            }
        }


        static string readArg()
        {
            StringBuilder result = new StringBuilder();
            char next = (char)Console.Read();
            while (next != ' ')
            {
                result.Append(next);
                next = (char)Console.Read();
            }
            return result.ToString();
        }
    }
}
