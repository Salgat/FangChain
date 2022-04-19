using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    /// <summary>
    /// Sets the alias for the user, where the alias maps to their address. As of now only one alias is supported per user.
    /// </summary>
    public class SetAliasTransaction : TransactionModel
    {
        public override TransactionType TransactionType { get => TransactionType.SetAlias; }
        public string PublicKeyBase58 { get; init; }
        public string Alias { get; init; }

        public SetAliasTransaction(string publicKeyBase58, string alias)
        {
            PublicKeyBase58 = publicKeyBase58;
            Alias = alias;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            stream.Write(Encoding.ASCII.GetBytes(Alias));
        }
    }
}
