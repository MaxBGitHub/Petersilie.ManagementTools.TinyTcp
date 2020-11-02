using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petersilie.ManagementTools.TinyTcp
{
    /// <summary>
    /// Used to notify applications that a server received
    /// a new TCP packet.
    /// </summary>
    public class TcpDataReceivedEventArgs : EventArgs
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
        /// Initializes a new instance with the data and the length property.
        /// </summary>
        /// <param name="data">The data the server received.</param>
        /// <param name="length">The length of the received data.</param>
        public TcpDataReceivedEventArgs(byte[] data, int length)
        {
            Data = data;
            Length = length;
        }
    }
}
