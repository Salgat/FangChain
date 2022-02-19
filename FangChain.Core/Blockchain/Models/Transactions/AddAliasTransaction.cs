using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class AddAliasTransaction : TransactionModel
    {
        public override TransactionType TransactionType { get => TransactionType.AddAlias; }
        public string PublicKeyBase58 { get; init; }
        public string Alias { get; init; }

        public AddAliasTransaction(string publicKeyBase58, string alias)
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
