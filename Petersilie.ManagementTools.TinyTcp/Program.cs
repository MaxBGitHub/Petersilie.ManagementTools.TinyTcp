using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Petersilie.ManagementTools.TinyTcp
{
    class Program
    {
        static TinyServer tinyServer;
        static IPAddress srvIP;
        static int srvPort;


        static void MessageReceived(object sender, TcpDataReceivedEventArgs e)
        {
            Console.WriteLine(e.Length + " - " + e.Data.Length  + " - " + Encoding.ASCII.GetString(e.Data, 0, e.Length));
        }

        static void Main(string[] args)
        {
            tinyServer = TinyServer.StartLocal();
            tinyServer.DataReceived += MessageReceived;
            srvIP = tinyServer.IPAddress;
            srvPort = tinyServer.Port;

            Thread serverThread = new Thread(delegate ()
            {                
                tinyServer.Start();
            });
            serverThread.Start();
            Console.WriteLine($"Server started on {srvIP.ToString()} with Port {srvPort}.");
        }


        static void ConnectToServer(IPAddress ip, int port)
        {
            try
            {
                TcpClient client = new TcpClient(new IPEndPoint(ip, port));
                NetworkStream stream = client.GetStream();
                int count = 0;
                while (count++ < 3)
                {
                    byte[] data = Encoding.ASCII.GetBytes($"Some text message that is send to server {count}");
                    stream.Write(data, 0, data.Length);

                    Thread.Sleep(500);
                }
                stream.Close();
                client.Close();
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
    }
}
