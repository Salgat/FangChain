using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    /// <summary>
    /// Maintains a singleton object for thread-safe crypto operations.
    /// </summary>
    public static class Secp256k1Singleton
    {
        private static readonly Secp256k1 _secp256k1 = new(); // It's not clear if this is thread-safe from the documentation,
                                                              // so we lock all accesses to it to be safe.

        public static TReturn Execute<TReturn>(Func<Secp256k1, TReturn> operation)
        {
            lock (_secp256k1)
            {
                return operation(_secp256k1);
            }
        }

        public static void Execute(Action<Secp256k1> operation)
        {
            lock (_secp256k1)
            {
                operation(_secp256k1);
            }
        }
    }
}
