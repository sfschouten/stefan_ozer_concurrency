using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace NetChange
{
    class Connection
    {
        private StreamReader read;
        private StreamWriter write;
        private TcpClient client;
        private int port;

        public bool Closing { get; set; }

        public Connection(int port)
        {
            this.port = port;
            client = new TcpClient("localhost", port);
            read = new StreamReader(client.GetStream());
            write = new StreamWriter(client.GetStream());
            Write.AutoFlush = true;
        }

        /// <summary>
        /// Make new connection
        /// </summary>
        public Connection(TcpClient client)
        {
            this.client = client;
            read = new StreamReader(client.GetStream());
            write = new StreamWriter(client.GetStream());
            write.AutoFlush = true;
            port = int.Parse(read.ReadLine().Split()[1]);
        }

        public void SendMessage(string msg)
        {
            write.WriteLine(msg);
        }

        public Thread Thread { get; set; }
        public StreamReader Read
        {
            get { return read; }
        }
        public StreamWriter Write
        {
            get { return write; }
        }
        public int Port
        {
            get { return port; }
        }

        public void Close()
        {
            Closing = true;
            client.Close();
        }
    }
}
