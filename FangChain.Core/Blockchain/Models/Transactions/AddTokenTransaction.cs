using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class AddTokenTransaction : TransactionModel
    {
        public override TransactionType TransactionType { get => TransactionType.AddToken; }
        public string PublicKeyBase58 { get; init; }
        public string TokenId { get; init; }
        public string Contents { get; init; }

        public AddTokenTransaction(string publicKeyBase58, string tokenId, string contents)
        {
            PublicKeyBase58 = publicKeyBase58;
            TokenId = tokenId;
            Contents = contents;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            stream.Write(Encoding.ASCII.GetBytes(TokenId));
            stream.Write(Encoding.ASCII.GetBytes(Contents));
        }
    }
}
