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
    public class TinyServer : IDisposable
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

        // Thread for accepting connections.
        private Thread _acceptLoop = null;


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


        private event EventHandler<EventArgs> onDisposing;
        /// <summary>
        /// Raised when the server is disposing.
        /// </summary>
        public event EventHandler<EventArgs> Disposing
        {
            add {
                onDisposing += value;
            }
            remove {
                onDisposing -= value;
            }
        }

        /// <summary>
        /// Invokes the Disposing event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnDisposing(EventArgs e)
        {
            onDisposing?.Invoke(this, e);
        }


        private event EventHandler<EventArgs> onDisposed;
        public event EventHandler<EventArgs> Disposed
        {
            add {
                onDisposed += value;
            }
            remove {
                onDisposed -= value;
            }
        }


        protected virtual void OnDisposed(EventArgs e)
        {
            onDisposed?.Invoke(this, e);
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
            
            try
            {
                byte[] buffer = new byte[0x0100];
                int length = stream.Read(buffer, 0, buffer.Length);

                while (0 != length) {
                    OnDataReceived(new TcpDataReceivedEventArgs(buffer, length));
                    length = stream.Read(buffer, 0, buffer.Length);
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


        // Loop for accepting client connections.
        private void AcceptClientLoop()
        {
            try
            {
                while (true) {
                    TcpClient client = _tinyServer.AcceptTcpClient();
                    var paramThreadStart = new ParameterizedThreadStart(ClientCallback);
                    Thread callbackThread = new Thread(paramThreadStart);
                    callbackThread.Start(client);
                } /* Endless loop to connect all clients knocking. */
            }
            catch (SocketException socketEx) {                
                _tinyServer.Stop();
                throw new SocketException(socketEx.ErrorCode);
            }
        }


        /// <summary>
        /// Start accepting clients.
        /// </summary>
        public void Start()
        {
            /* Make sure to get rid of the accept loop thread
            ** before starting a new one. */
            Stop();

            // Initialize new client accept loop thread.
            _acceptLoop = new Thread(new ThreadStart(AcceptClientLoop));
            // Start accepting clients.
            _acceptLoop.Start();
        }


        // Checks if the ThreadState.Running flag is set.
        private bool IsRunning(ThreadState state)
        {
            return ((ThreadState.Running & state) == ThreadState.Running);            
        }


        /// <summary>
        /// Stops the current server instance from accepting anymore clients.
        /// </summary>
        public void Stop()
        {
            if (null != _acceptLoop) {
                if (IsRunning(_acceptLoop.ThreadState)) {
                    try {
                        _acceptLoop.Join(500);
                        if (_acceptLoop.IsAlive) {
                            try {
                                _acceptLoop.Abort();
                            } // Try to abort thread.
                            catch { }
                        } // Check if thread is still alive.
                        _acceptLoop = null;
                    } // Try to stop thread.
                    catch { }
                } // Thread is currently running.
                else {
                    _acceptLoop = null;
                } // Thread is not running properly.
            }
        }


        /* ==============================
        ** =    InitServer() Function   =
        ** ==============================
        ** 
        ** Initializes the TinyServer instance and starts it.
        ** Sets IP address and port and checks if port is valid.
        ** 
        ** Return values:
        ** ==============
        ** 1 - If the port is not larger than 1024 the function returns 1,
        ** indicating that the port in within the well-known port range
        ** and thus invalid for use of internal applications.
        ** 
        ** 2 - The port is already beeing used by a other application or 
        ** server instance and cannot be used.
        **
        ** 3 - Sanity check indicating that still, after all checks the
        ** IP or port are invalid.
        **
        ** 4 + SocketError - TcpListener could not be started.
        ** The return value is 4 + error code of the caught socket exception.
        ** To get the underlying socket error code simply subtract 4 from
        ** the return value of this function.
        **
        ** Example:
        ** ========
        **  int retVal = InitServer(IPAddress, int);
        **  if (retVal >= 4) {
        **      SocketError sErr = retVal - 4;
        **      throw new SocketException(sErr);
        **  }
        */
        private int InitServer(IPAddress address, int port)
        {
            if (1024 >= port) {
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
            catch (SocketException ex) {
                return 4 + ex.ErrorCode;
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
            
            // Initialize server and port.
            retVal = InitServer(address, endpoint.Port);

            if (1 == retVal) {
                throw new ArgumentOutOfRangeException(
                    nameof(endpoint), 
                    endpoint.Port, 
                    "Port cannot be between 0 to 1024.");
            }
            else if (2 == retVal) {
                throw new AccessViolationException(
                    "Specified port is already beeing used.");
            }
            else if (3 == retVal) {
                throw new ArgumentException("Invalid Server parameters.");
            }
            else if (4 <= retVal) {
                throw new InvalidOperationException(
                    "Unable to start server socket.");
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
                    nameof(port), 
                    port, 
                    "Port cannot be between 0 to 1024.");
            }
            else if (2 == retVal) {
                throw new AccessViolationException(
                    "Specified port is already beeing used.");
            }
            else if (3 == retVal) {
                throw new ArgumentException("Invalid Server parameters.");
            }
            else if (4 == retVal) {
                throw new InvalidOperationException(
                    "Unable to start server socket.");
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
                    "IP is invalid or not IPv4.", 
                    nameof(ip));
            }

            retVal = InitServer(address, port);

            if (1 == retVal) {
                throw new ArgumentOutOfRangeException(
                    nameof(port), 
                    port, 
                    "Port cannot be between 0 to 1024.");
            }
            else if (2 == retVal) {
                throw new AccessViolationException(
                    "Specified port is already beeing used.");
            }
            else if (3 == retVal) {
                throw new ArgumentException("Invalid Server parameters.");
            }
            else if (4 == retVal) {
                throw new InvalidOperationException(
                    "Unable to start server socket.");
            }
        }


        ~TinyServer() { Dispose(false); }
        public void Dispose() { Dispose(true); }
        private void Dispose(bool disposing)
        {
            OnDisposing(EventArgs.Empty);

            if (disposing) {
                GC.SuppressFinalize(this);
            }

            Stop();
            if (null != _tinyServer) {
                try {
                    _tinyServer.Stop();
                    _tinyServer = null;
                } catch { }
            }

            OnDisposed(EventArgs.Empty);
        }
    }
}
