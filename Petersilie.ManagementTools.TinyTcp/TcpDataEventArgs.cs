using System;

namespace Petersilie.ManagementTools.TinyTcp
{
    /// <summary>
    /// Used to notify applications that a server received
    /// a new TCP packet.
    /// </summary>
    public class TcpDataEventArgs : EventArgs
    {
        /// <summary>
        /// Raw data that the server received.
        /// </summary>
        public byte[] Data { get; }
        /// <summary>
        /// Length of the raw data.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Object that stores the server and tcp client instance.
        /// </summary>
        public CallbackStateObject ClientStateObject { get; }
        /// <summary>
        /// TRUE if 
        /// </summary>
        public bool HasError { get; internal set; }


        private Exception _exception;
        /// <summary>
        /// The exception which was caught that
        /// caused the callback to fail.
        /// </summary>
        public Exception Exception
        {
            get {
                return _exception;
            }
            internal set {
                if (null != value) {
                    HasError = true;
                } else {
                    HasError = false;
                }
                _exception = value;
            }
        }


        /// <summary>
        /// Returns an empty TcpDataEventArgs instance.
        /// </summary>
        public new static TcpDataEventArgs Empty
        {
            get {
                return new TcpDataEventArgs();
            }
        }


        internal TcpDataEventArgs() { }


        /// <summary>
        /// Initializes a new instance with the data, length 
        /// and ClientStateObject.
        /// </summary>
        /// <param name="stateObj">The callback state.</param>
        /// <param name="data">The data the server received.</param>
        /// <param name="length">The length of the received data.</param>
        public TcpDataEventArgs(CallbackStateObject stateObj, 
                                byte[] data, 
                                int length)
        {
            Data = data;
            Length = length;
            ClientStateObject = stateObj;
            Exception = null;
        }

        /// <summary>
        /// Initializes a new instance with the data and the length property.
        /// </summary>
        /// <param name="data">The data the server received.</param>
        /// <param name="length">The length of the received data.</param>
        public TcpDataEventArgs(byte[] data, int length)
        {
            Data = data;
            Length = length;
            ClientStateObject = null;
            Exception = null;
        }

        /// <summary>
        /// Initializes a new instance with the ClientStateObject
        /// and Exception.
        /// </summary>
        /// <param name="stateObj"></param>
        /// <param name="ex"></param>
        public TcpDataEventArgs(CallbackStateObject stateObj, Exception ex)
        {
            Data = new byte[0];
            Length = 0;
            ClientStateObject = stateObj;
            Exception = ex;
        }
    }
}
