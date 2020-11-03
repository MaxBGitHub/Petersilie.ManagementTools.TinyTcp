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
    public class TinyTcpServer : IDisposable
    {
        #region Public properties

        /// <summary>
        /// IPv4 address of the socket.
        /// </summary>
        public IPAddress IPAddress { get; private set; }
        /// <summary>
        /// Port of the server instance.
        /// </summary>
        public int Port { get; private set; }

        #endregion


        #region Private properties

        // Listener for receiving data from clients.
        private TcpListener _tinyServer = null;

        // Thread for accepting connections.
        private Thread _acceptLoop = null;

        // Syncs the calls to EndAcceptClient(IAsyncResult).
        private ManualResetEvent _waiter = new ManualResetEvent(false);

        #endregion


        #region Custom events

        private event EventHandler<TcpDataEventArgs> onDataReceived;
        /// <summary>
        /// Raised when the server receives a message from a client.
        /// </summary>
        public event EventHandler<TcpDataEventArgs> DataReceived
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
        protected virtual void OnDataReceived(TcpDataEventArgs e)
        {
            onDataReceived?.Invoke(this, e);
        }


        private event EventHandler<TcpDataEventArgs> onDataDropped;
        /// <summary>
        /// Raised when the TcpClient in the ClientCallback method is null.
        /// </summary>
        public event EventHandler<TcpDataEventArgs> DataDropped
        {
            add {
                onDataDropped += value;
            }
            remove {
                onDataDropped -= value;
            }
        }

        /// <summary>
        /// Invokes the DataDropped event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnDataDropped(TcpDataEventArgs e)
        {
            onDataDropped?.Invoke(this, e);
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
        /// <summary>
        /// Raised when the server is disposed.
        /// </summary>
        public event EventHandler<EventArgs> Disposed
        {
            add {
                onDisposed += value;
            }
            remove {
                onDisposed -= value;
            }
        }

        /// <summary>
        /// Invokes the Disposed event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnDisposed(EventArgs e)
        {
            onDisposed?.Invoke(this, e);
        }

        #endregion


        #region Client polling / processing

        /// <summary>
        /// Closes a client connection and raises events depending
        /// on various object states.
        /// </summary>
        /// <param name="ar"></param>
        protected virtual void EndAcceptClient(IAsyncResult ar)
        {
            // Stores Exception that might occur.
            Exception ex = null;

            CallbackStateObject stateObj = ar.AsyncState as CallbackStateObject;
            if (null == stateObj) {
                ex = new ArgumentNullException(nameof(ar.AsyncState));
                stateObj = new CallbackStateObject(_tinyServer, null);
                OnDataDropped(new TcpDataEventArgs(stateObj, ex));
                return;
            } /* Invalid CallbackStateObject. */            

            try
            {
                // Get TcpClient from state object.
                TcpClient client = stateObj.Server.EndAcceptTcpClient(ar);
                if (null == client) {
                    ex = new ArgumentNullException(nameof(client));
                    OnDataDropped(new TcpDataEventArgs(stateObj, ex));
                    return;
                } /* Invalid TcpClient object. */

                NetworkStream stream = client.GetStream();
                if (null == stream) {
                    stateObj.Client = client;
                    ex = new ArgumentNullException(nameof(stream));                    
                    OnDataDropped(new TcpDataEventArgs(stateObj, ex));
                    return;
                } /* No data. */                

                // Assign buffer for TCP data.
                byte[] buffer = new byte[0x0100];
                // Begin reading the NetworkStream.
                int length = stream.Read(buffer, 0, buffer.Length);

                while (0 != length) {
                    // Notify that data has been received.
                    OnDataReceived(new TcpDataEventArgs(buffer, length));
                    // Continue reading stream.
                    length = stream.Read(buffer, 0, buffer.Length);
                } /* Loop while stream has data. */
            }            
            catch (ArgumentNullException) {
                ex = new ArgumentNullException(
                    "Buffer for reading client data is null.");
            }
            catch (ArgumentOutOfRangeException) {
                ex = new ArgumentOutOfRangeException(
                    "Unable to read data at the offset 0.");
            }            
            catch (System.IO.IOException) {
                ex = new System.IO.IOException(
                    "Error while accessing socket or while trying " +
                    "to read the clients NetworkStream.");
            }
            catch (ObjectDisposedException) {
                ex = new ObjectDisposedException(
                    "NetworkStream of client was closed " +
                    "while attempting to read it.");
            }
            catch (InvalidOperationException) {
                ex = new InvalidOperationException(
                    "NetworkStream of client does " +
                    "not support read operations.");
            }
            finally
            {
                if (null != ex) {
                    OnDataDropped(new TcpDataEventArgs(stateObj, ex));
                } /* Exception was caught. */

                // Allow next call of EndAcceptClient().
                _waiter.Set();
            }
        }



        // Loop for accepting client connections.
        private void AcceptClientLoop()
        {
            try
            {
                while (true) {
                    // Start polling
                    _waiter.Reset();
                    // Create state object for async callback of client.
                    var stateObj = new CallbackStateObject(_tinyServer, null);
                    // Begin accepting client communication.
                    _tinyServer.BeginAcceptTcpClient(EndAcceptClient, stateObj);
                    // Wait until client is processed.
                    _waiter.WaitOne();

                } /* Endless loop to connect all clients knocking. */
            }
            catch (SocketException socketEx) {                
                _tinyServer.Stop();
                throw new SocketException(socketEx.ErrorCode);
            }
        }

        #endregion


        #region Start / Stop server

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

        #endregion


        #region Server initialization

        /* ==============================
        ** =    InitServer() Function   =
        ** ==============================
        ** 
        ** Initializes the TinyServer instance and starts it.
        ** Sets IP address and port and checks if port is valid.
        ** 
        ** Return values:
        ** ==============
        ** 0 - Success.
        **
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

        #endregion


        #region Constructor

        /// <summary>
        /// Creates a new TinyServer instance on local host 127.0.0.1
        /// and a port between 46337 and 46997.
        /// </summary>
        /// <returns>Returns a TinyServer instance that runs
        /// on the local host address 127.0.0.1</returns>
        /// <exception cref="Exception">Throws any exception produced
        /// by the TinyServer(string, int) constructor.</exception>
        public static TinyTcpServer StartLocal()
        {
            try
            {
                int[] port = PortUtil.GetAvailablePorts(
                    PortRange.PR_46337_46997, 
                    true);

                var server = new TinyTcpServer("127.0.0.1", port[0]);
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
        public TinyTcpServer(IPEndPoint endpoint)
        {
            int retVal = 0;
            IPAddress address;
            string ip = endpoint.Address.ToString();
            if ( !(IPUtil.IsValid(ip, out address)) ) {
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
        public TinyTcpServer(IPAddress ip, int port)
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
        public TinyTcpServer(string ip, int port)
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

        #endregion


        #region IDisposable implementation

        /// <summary>
        /// Destructor.
        /// </summary>
        ~TinyTcpServer() {
            Dispose(false);
            OnDisposed(EventArgs.Empty);
        }

        /// <summary>
        /// Frees all resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            OnDisposed(EventArgs.Empty);
        }

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
        }

        #endregion
    }
}
