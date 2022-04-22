using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class EnableUserTransaction : TransactionModel
    {
        public override TransactionType TransactionType => TransactionType.EnableUser;
        public string PublicKeyBase58 { get; init; }

        public EnableUserTransaction(string publicKeyBase58, string transactionId) : base(transactionId)
        {
            PublicKeyBase58 = publicKeyBase58;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(Id));
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
        }
    }
}
