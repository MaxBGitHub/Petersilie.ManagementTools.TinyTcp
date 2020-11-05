using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petersilie.ManagementTools.TinyTcp
{
    internal static class SR
    {
        public const string ERROR_WELLKNOWNPORT     = "Port cannot be within well-known port range of 1 to 1024.";
        public const string ERROR_USEDPORT          = "Specified port is already beeing used.";
        public const string ERROR_INVALIDPARAM      = "Invalid server parameters.";
        public const string ERROR_NOSTART           = "Unable to start server socket.";
        public const string ERROR_IPINVALID         = "IP could not be parsed or is not an IPv4 address.";
        public const string ERROR_BUFFERNULL        = "Buffer for reading client data is null.";
        public const string ERROR_OFFSETREAD        = "Unable to read data at offset 0.";
        public const string ERROR_SOCKETORSTREAM    = "Error while accessing socket or while trying to read the clients NetworkStream.";
        public const string ERROR_STREAMCLOSED      = "NetworkStream of client was client while attempting to read it.";
        public const string ERROR_READNOTSUPPORTED  = "NetworkStream of client not support read operations.";
    }
}
