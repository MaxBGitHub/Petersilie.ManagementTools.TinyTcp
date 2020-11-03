using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petersilie.ManagementTools.TinyTcp
{
    /// <summary>
    /// Used to notify applications that a server had
    /// to drop the data of a client.
    /// </summary>
    public class DataDroppedEventArgs : EventArgs
    {
        


        /// <summary>
        /// Inializes a new instance of the DataDroppedEventArgs
        /// </summary>
        /// <param name="stateObject"></param>
        /// <param name="ex"></param>
        public DataDroppedEventArgs(CallbackStateObject stateObject, 
                                    Exception ex)
        {
            ClientStateObject = stateObject;
            Exception = ex;
        }


        public DataDroppedEventArgs(CallbackStateObject stateObject)
        {
            ClientStateObject = stateObject;
        }
    }
}
