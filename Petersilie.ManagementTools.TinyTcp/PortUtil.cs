using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;

namespace Petersilie.ManagementTools.TinyTcp
{
    /// <summary>
    /// Utility class for getting and checking network ports.
    /// </summary>
    public static class PortUtil
    {
        private static int[] GetRange(int min, int max)
        {
            int count = 1 + (max - min);
            int[] range = new int[count];

            int n = -1;
            while (++n < count) {
                range[n] = n + min;
            }
            return range;
        }


        /* Creates an array of ports which can be used.
        ** Returns only the ports which are within the
        ** specified range of ports.
        ** Returns the ports of range 14415 to 14935 as 
        ** default. */
        private static int[] GetPortRange(PortRange pr)
        {
            switch (pr)
            {
                case PortRange.PR_14415_14935:
                    {
                        return GetRange(14415, 14935);
                    }
                case PortRange.PR_21011_21552:
                    {
                        return GetRange(21011, 21552);                       
                    }
                case PortRange.PR_26490_26999:
                    {
                        return GetRange(26490, 26999);
                    }
                case PortRange.PR_28590_29117:
                    {
                        return GetRange(28590, 29117);
                    }
                case PortRange.PR_29170_29998:
                    {
                        return GetRange(29170, 29998);
                    }
                case PortRange.PR_30261_30831:
                    {
                        return GetRange(30261, 30831);
                    }
                case PortRange.PR_33657_34248:
                    {
                        return GetRange(33657, 34248);
                    }
                case PortRange.PR_35358_36000:
                    {
                        return GetRange(35358, 36000);
                    }
                case PortRange.PR_36866_37474:
                    {
                        return GetRange(36866, 37474);
                    }
                case PortRange.PR_38204_38799:
                    {
                        return GetRange(38204, 38799);
                    }
                case PortRange.PR_38866_39680:
                    {
                        return GetRange(38866, 39680);
                    }
                case PortRange.PR_41231_41793:
                    {
                        return GetRange(41231, 41793);
                    }
                case PortRange.PR_41798_42507:
                    {
                        return GetRange(41798, 42507);
                    }
                case PortRange.PR_43442_44122:
                    {
                        return GetRange(43442, 44122);
                    }
                case PortRange.PR_46337_46997:
                    {
                        return GetRange(46337, 46997);
                    }
                default:
                    {
                        return GetRange(14415, 14935);
                    }
            }
        }


        /// <summary>
        /// Checks if the specified port is currently used on the machine.
        /// </summary>
        /// <param name="port">Port to check.</param>
        /// <returns>Returns TRUE if the port is still 
        /// available for use.</returns>
        public static bool IsAvailable(int port)
        {
            bool isAvail = true;

            // Load all IP props.
            IPGlobalProperties ipGProps = IPGlobalProperties.GetIPGlobalProperties();
            // Load all TCP connection informations.
            TcpConnectionInformation[] tcpConnInfos = ipGProps.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpInfo in tcpConnInfos) {
                if (port == tcpInfo.LocalEndPoint.Port) {
                    // Port is used and unavailable.
                    isAvail = false;
                    break;
                } /* Check if port matches the specified port. */
            } /* Loop through TCP connection infos. */

            // Return port availabilty.
            return isAvail;
        }


        /// <summary>
        /// Gets a range of available ports for internal applications.
        /// </summary>
        /// <param name="pr">Enum to specify which range of 
        /// ports to receive and to check for availablity.</param>
        /// <param name="checkAvailability">TRUE to perform an availabilty 
        /// check on each port.</param>
        /// <returns>Returns an array of ports which are 
        /// currently unused.</returns>
        public static int[] GetAvailablePorts(PortRange pr, bool checkAvailability)
        {
            // Get all ports of that range.
            int[] ports = GetPortRange(pr);
            List<int> availPorts = new List<int>();
            if (checkAvailability) {
                for (int i=0; i<ports.Length; i++) {
                    if (IsAvailable(ports[i])) {
                        availPorts.Add(ports[i]);
                    } /* Check if that port is available for use. */
                } /* Loop through all ports. */

                // Return unused ports.
                return availPorts.ToArray();
            } /* Port availablity should be checked. */
            else {
                // Return all ports.
                return ports;
            } /* Do no perform port availabilty check. */ 
        }


        /// <summary>
        /// Gets a range of ports for internal applications.
        /// </summary>
        /// <param name="pr">Enum to specify which range of 
        /// ports to receive and to check for availablity.</param>
        /// <returns>Returns an array of ports.</returns>
        public static int[] GetAvailablePorts(PortRange pr)
        {
            int[] ports = GetPortRange(pr);
            return ports;
        }


    }
}
