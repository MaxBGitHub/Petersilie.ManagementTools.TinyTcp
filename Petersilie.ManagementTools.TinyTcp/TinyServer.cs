using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Petersilie.ManagementTools.TinyTcp
{
    /// <summary>
    /// A lightweight server that listens for TCP packets.
    /// </summary>
    public class TinyServer
    {
        /// <summary>
        /// IPv4 address of the socket.
        /// </summary>
        public IPAddress IPAddress { get; private set; }
        /// <summary>
        /// Port of the server instance.
        /// </summary>
        public int Port { get; private set; }

        // Listener for receiving data from clients.
        private TcpListener _tinyServer = null;

        private event EventHandler<TcpDataReceivedEventArgs> onDataReceived;
        /// <summary>
        /// Raised when the server receives a message from a client.
        /// </summary>
        public event EventHandler<TcpDataReceivedEventArgs> DataReceived
        {
            add {
                onDataReceived += value;
            }
            remove {
                onDataReceived -= value;
            }
        }

        /// <summary>
        /// Invokes the DataReceived event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnDataReceived(TcpDataReceivedEventArgs e)
        {
            onDataReceived?.Invoke(this, e);
        }


        private event EventHandler<EventArgs> onClientLost;
        /// <summary>
        /// Raised when the TcpClient in the ClientCallback method is null.
        /// </summary>
        public event EventHandler<EventArgs> ClientLost
        {
            add {
                onClientLost += value;
            }
            remove {
                onClientLost -= value;
            }
        }

        /// <summary>
        /// Invokes the ClientLost event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClientLost(EventArgs e)
        {
            onClientLost?.Invoke(this, e);
        }


        /// <summary>
        /// Callback to receive client packages.
        /// </summary>
        /// <param name="obj">Connected TcpClient</param>
        /// <exception cref="ArgumentNullException">Buffer error</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Buffer offset error</exception>
        /// <exception cref="System.IO.IOException">
        /// Socket exception</exception>
        /// <exception cref="ObjectDisposedException">
        /// Clients NetworkStream was closed while reading it.</exception>
        /// <exception cref="InvalidOperationException">
        /// Client NetworkStream does not support read operations.</exception>
        protected virtual void ClientCallback(object obj)
        {
            TcpClient client = (TcpClient)obj;
            if (null == client) {
                OnClientLost(EventArgs.Empty);
                return;
            }

            NetworkStream stream = client.GetStream();
            if (null == stream) {
                return;
            }

            byte[] buffer = new byte[client.SendBufferSize];
            int read = 0;
            try
            {
                while ((read = stream.Read(buffer, 0, buffer.Length)) != 0) {
                    OnDataReceived(new TcpDataReceivedEventArgs(buffer, read));
                }
            }            
            catch (ArgumentNullException) {
                throw new ArgumentNullException(
                    "Buffer for reading client data is null.");
            }
            catch (ArgumentOutOfRangeException) {
                throw new ArgumentOutOfRangeException(
                    "Unable to read data at the offset 0.");
            }            
            catch (System.IO.IOException) {
                throw new System.IO.IOException(
                    "Error while accessing socket or while trying " +
                    "to read the clients NetworkStream.");
            }
            catch (ObjectDisposedException) {
                throw new ObjectDisposedException(
                    "NetworkStream of client was closed " +
                    "while attempting to read it.");
            }
            catch (InvalidOperationException) {
                throw new InvalidOperationException(
                    "NetworkStream of client does " +
                    "not support read operations.");
            }
        }


        /// <summary>
        /// Start listening for incoming connections and messages.
        /// </summary>
        public void Start()
        {
            try
            {
                while (true) {
                    TcpClient client = _tinyServer.AcceptTcpClient();
                    Thread thread = new Thread(new ParameterizedThreadStart(ClientCallback));
                    thread.Start(client);
                } /* Endless loop to connect all clients knocking. */
            }
            catch (SocketException socketEx) {                
                _tinyServer.Stop();
                throw new SocketException(socketEx.ErrorCode);
            }
        }


        /* Initializes the TinyServer instance and starts it.
        ** Sets IP address and port and checks if
        ** port is valid or used already. */
        private int InitServer(IPAddress address, int port)
        {
            if (1024 >= port || 0 >= port) {
                return 1;
            } /* Check if port is within reserved and well-known range. */

            if ( !(PortUtil.IsAvailable(port)) ) {
                return 2;
            } /* Check if the port is already used on the machine. */

            // Set IP address and port member.
            IPAddress = address;
            Port = port;

            try {
                _tinyServer = new TcpListener(IPAddress, Port);
                _tinyServer.Start();
            } /* try to initializes internal TcpListener and start it. */
            catch (ArgumentNullException) {
                return 3;
            } /* IP or port where invalid. */
            catch (ArgumentOutOfRangeException) {
                return 3;
            } /* IP or port where out of range. */
            catch (SocketException) {
                return 4;
            } /* TcpListener could not be started.*/

            // Everything went well.
            return 0;
        }


        /// <summary>
        /// Creates a new TinyServer instance on local host 127.0.0.1
        /// and a port between 46337 and 46997.
        /// </summary>
        /// <returns>Returns a TinyServer instance that runs
        /// on the local host address 127.0.0.1</returns>
        /// <exception cref="Exception">Throws any exception produced
        /// by the TinyServer(string, int) constructor.</exception>
        public static TinyServer StartLocal()
        {
            try {
                int[] port = PortUtil.GetAvailablePorts(PortRange.PR_46337_46997, true);
                var server = new TinyServer("127.0.0.1", port[0]);

                return server;
            }            
            catch (Exception e) {
                throw new Exception(e.Message);
            }
        }


        /// <summary>
        /// Initializes a new TinyServer which listens for TCP packets.
        /// </summary>
        /// <param name="endpoint">IPEndPoint containg server address 
        /// and port.</param>
        /// <exception cref="ArgumentException">
        /// IP is invalid or no IPv4 Address.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Port is within an invalid range (0-1024).
        /// </exception>
        /// <exception cref="AccessViolationException">
        /// Specified port is already in use.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Server could not be started.
        /// </exception>
        public TinyServer(IPEndPoint endpoint)
        {
            int retVal = 0;
            IPAddress address;
            if ( !(IPUtil.IsValid(endpoint.Address.ToString(), out address)) ) {
                throw new ArgumentException(
                    "IP is invalid or not IPv4.", nameof(endpoint));
            }
            
            retVal = InitServer(address, endpoint.Port);

            if (1 == retVal) {
                throw new ArgumentOutOfRangeException(
                    nameof(endpoint), endpoint.Port, "Port cannot be between 0 to 1024.");
            }
            else if (2 == retVal) {
                throw new AccessViolationException(
                    "Specified port is already beeing used.");
            }
            else if (3 == retVal) {
                throw new ArgumentException("Invalid Server parameters.");
            }
            else if (4 == retVal) {
                throw new InvalidOperationException("Unable to start server socket.");
            }
        }


        /// <summary>
        /// Initializes a new TinyServer which listens for TCP packets.
        /// </summary>
        /// <param name="ip">The IP address to use for the server.</param>
        /// <param name="port">The port on which the server runs.</param>
        /// <exception cref="ArgumentException">
        /// IP is invalid or no IPv4 Address.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Port is within an invalid range (0-1024).
        /// </exception>
        /// <exception cref="AccessViolationException">
        /// Specified port is already in use.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Server could not be started.
        /// </exception>
        public TinyServer(IPAddress ip, int port)
        {
            int retVal = 0;
            IPAddress address;
            if ( !(IPUtil.IsValid(ip.ToString(), out address)) ) {
                throw new ArgumentException(
                    "IP is invalid or not IPv4.", nameof(ip));
            }

            retVal = InitServer(address, port);

            if (1 == retVal) {
                throw new ArgumentOutOfRangeException(
                    nameof(port), port, "Port cannot be between 0 to 1024.");
            }
            else if (2 == retVal) {
                throw new AccessViolationException(
                    "Specified port is already beeing used.");
            }
            else if (3 == retVal) {
                throw new ArgumentException("Invalid Server parameters.");
            }
            else if (4 == retVal) {
                throw new InvalidOperationException("Unable to start server socket.");
            }
        }


        /// <summary>
        /// Initializes a new TinyServer which listens for TCP packets.
        /// </summary>
        /// <param name="ip">The IP address to use for the server.</param>
        /// <param name="port">The port on which the server runs.</param>
        /// <exception cref="ArgumentException">
        /// IP is invalid or no IPv4 Address.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Port is within an invalid range (0-1024).
        /// </exception>
        /// <exception cref="AccessViolationException">
        /// Specified port is already in use.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Server could not be started.
        /// </exception>
        public TinyServer(string ip, int port)
        {
            int retVal = 0;
            IPAddress address;
            if ( !(IPUtil.IsValid(ip, out address)) ) {
                throw new ArgumentException(
                    "IP is invalid or not IPv4.", nameof(ip));
            }

            retVal = InitServer(address, port);

            if (1 == retVal) {
                throw new ArgumentOutOfRangeException(
                    nameof(port), port, "Port cannot be between 0 to 1024.");
            }
            else if (2 == retVal) {
                throw new AccessViolationException(
                    "Specified port is already beeing used.");
            }
            else if (3 == retVal) {
                throw new ArgumentException("Invalid Server parameters.");
            }
            else if (4 == retVal) {
                throw new InvalidOperationException("Unable to start server socket.");
            }
        }


    }
}
