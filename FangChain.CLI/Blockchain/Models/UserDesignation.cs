using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    /// <summary>
    /// The user's designated level. How high or low the value is has no relation to the privilege of the user's level beyond its designation.
    /// </summary>
    public enum UserDesignation
    {
        Anonymous = 0,
        Verified = 1, // Is acknowledged by the blockchain as a specific verified user, with a known identity
        SuperAdministrator = 2 // The highest privilege available, able to create blocks, promote users, etc
    }
}
