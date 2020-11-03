using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Petersilie.ManagementTools.TinyTcp
{
    /// <summary>
    /// Stores server and client object of Tcp communication.
    /// </summary>
    public class CallbackStateObject
    {
        /// <summary>
        /// Server that received a message from the TcpClient.
        /// </summary>
        public TcpListener Server { get; internal set; } 
        /// <summary>
        /// TcpClient that send the data to the server.
        /// </summary>
        public TcpClient Client { get; internal set; }


        /// <summary>
        /// Returns an empty instance of the CallbackStateObject.
        /// </summary>
        public static CallbackStateObject Empty
        {
            get {
                return new CallbackStateObject();
            }
        }

        /// <summary>
        /// Initializes a CallbackStateObject instance
        /// with the sever and client.
        /// </summary>
        /// <param name="server">Server that the client is connected to</param>
        /// <param name="client">Client that connected to the server.</param>
        public CallbackStateObject(TcpListener server, TcpClient client)
        {
            Server = server;
            Client = client;
        }

        /// <summary>
        /// Initializes a new CallbackStateObject instance.
        /// </summary>
        public CallbackStateObject()
        {
            Server = null;
            Client = null;
        }
    }
}
