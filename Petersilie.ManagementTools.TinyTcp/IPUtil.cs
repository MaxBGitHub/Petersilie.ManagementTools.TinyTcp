using System.Net;
using System.Net.Sockets;

namespace Petersilie.ManagementTools.TinyTcp
{
    /// <summary>
    /// Utility class for validating IPv4 addresses.
    /// </summary>
    public static class IPUtil
    {
        /// <summary>
        /// Checks if the specified IP can be parsed and
        /// if it is a IPv4 address.
        /// </summary>
        /// <param name="ip">IP address to validate.</param>
        /// <param name="address">Validated IP address.</param>
        /// <returns>Returns TRUE of the IP is a valid IPv4 address.</returns>
        public static bool IsValid(string ip, out IPAddress address)
        {
            if (IPAddress.TryParse(ip, out address)) {
                switch (address.AddressFamily) {
                    case AddressFamily.InterNetwork:
                        {
                            return true;
                        } /* IP is IPv4. */
                    default:
                        {
                            return false;
                        } /* IP is anything but IPv4. */
                } /* Check AddressFamily of parsed IP. */
            }
            else {
                return false;
            } /* IP could not be parsed. */
        }
    }
}
