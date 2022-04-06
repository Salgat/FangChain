using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class RemoveTokenTransaction : TransactionModel
    {
        public override TransactionType TransactionType { get => TransactionType.RemoveToken; }
        public string PublicKeyBase58 { get; init; }
        public string TokenId { get; init; }

        public RemoveTokenTransaction(string publicKeyBase58, string tokenId)
        {
            PublicKeyBase58 = publicKeyBase58;
            TokenId = tokenId;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            stream.Write(Encoding.ASCII.GetBytes(TokenId));
        }
    }
}
