using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    public abstract class TransactionModel
    {
        public ImmutableArray<SignatureModel> Signatures { get; private set; }
        
        public void SetSignatures(ImmutableArray<SignatureModel> signatures)
        {
            Signatures = signatures;
        }

        /// <summary>
        /// Returns the bytes of the transaction, including the signatures.
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetBytes();

        /// <summary>
        /// Returns the hash of the Transaction (not including Signatures).
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetHash();
    }
}
