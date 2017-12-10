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

        public Connection(int port)
        {
            client = new TcpClient("localhost", port);
            read = new StreamReader(client.GetStream());
            write = new StreamWriter(client.GetStream());
            Write.AutoFlush = true;
        }

        public Connection(StreamReader reader, StreamWriter writer)
        {
            read = reader;
            write = writer;
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

        public void Close()
        {
            client.Close();
        }
    }
}
