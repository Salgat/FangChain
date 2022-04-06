using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class DisableUserTransaction : TransactionModel
    {
        public override TransactionType TransactionType => TransactionType.DisableUser;
        public string PublicKeyBase58 { get; init; }

        public DisableUserTransaction(string publicKeyBase58)
        {
            PublicKeyBase58 = publicKeyBase58;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
        }
    }
}
