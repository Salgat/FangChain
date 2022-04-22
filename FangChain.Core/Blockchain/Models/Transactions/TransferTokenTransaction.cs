using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class TransferTokenTransaction : TransactionModel
    {
        public override TransactionType TransactionType { get => TransactionType.TransferToken; }
        public string FromPublicKeyBase58 { get; init; }
        public string ToPublicKeyBase58 { get; init; }
        public string TokenId { get; init; }

        public TransferTokenTransaction(string fromPublicKeyBase58, string toPublicKeyBase58, string tokenId, string transactionId) : base(transactionId)
        {
            FromPublicKeyBase58 = fromPublicKeyBase58;
            ToPublicKeyBase58 = toPublicKeyBase58;
            TokenId = tokenId;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(Id));
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(FromPublicKeyBase58));
            stream.Write(Encoding.ASCII.GetBytes(ToPublicKeyBase58));
            stream.Write(Encoding.ASCII.GetBytes(TokenId));
        }
    }
}
