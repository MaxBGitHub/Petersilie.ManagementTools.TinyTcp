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
    /// A lightweight client that sends data to a server over TCP.
    /// </summary>
    public class TinyClient
    {
        /// <summary>
        /// IPv4 address of the server.
        /// </summary>
        public IPAddress ServerAddress { get; }
        /// <summary>
        /// Port of the server.
        /// </summary>
        public int ServerPort { get; }


        private event EventHandler<EventArgs> onConnectionLost;
        /// <summary>
        /// Raised when the connection to the server is lost.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionLost
        {
            add {
                onConnectionLost += value;
            }
            remove {
                onConnectionLost -= value;
            }
        }

        /// <summary>
        /// Invokes the ConnectionLost event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnConnectionLost(EventArgs e)
        {
            onConnectionLost?.Invoke(this, e);
        }


        /// <summary>
        /// Sends data to the connected server.
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            TcpClient client = null;
            NetworkStream outStream = null;

            try
            {
                // Try to connect to server.
                client = new TcpClient(ServerAddress.ToString(), ServerPort);
                if (client.Connected) {                    
                    outStream = client.GetStream();
                    // Write data to NetworkStream of client.
                    outStream.Write(data, 0, data.Length);
                } /* Sanity check to make sure we are connected. */
            }
            catch (SocketException) {
                OnConnectionLost(EventArgs.Empty);
            } /* Client could not connect to server. */
            finally
            {
                // Clean up clients NetworkStream.
                if (null != outStream) {
                    try {
                        outStream.Close();
                        outStream.Dispose();
                    } catch { }
                }

                // Clean up client.
                if (null != client) {
                    try {
                        client.Close();
                    } catch { }
                }
            }
        }


        /// <summary>
        /// Initializes a new TinyClient instance for 
        /// a client-server communication over TCP.
        /// </summary>
        /// <param name="serverIp">The IPv4 address of the server.</param>
        /// <param name="serverPort">The port on which the 
        /// server can be reached.</param>
        public TinyClient(string serverIp, int serverPort)
        {
            IPAddress address;
            if ( !(IPUtil.IsValid(serverIp, out address)) ) {
                throw new ArgumentException("Invalid IP or not IPv4.", nameof(serverIp));
            } /* Check if IP is valid and IPv4. */

            // Set server IP and port.
            ServerAddress = address;
            ServerPort = serverPort;
        }
    }
}
