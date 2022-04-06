using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class AddToUserBalanceTransaction : TransactionModel
    {
        public override TransactionType TransactionType => TransactionType.AddToUserBalance;
        public string PublicKeyBase58 { get; init; }
        public BigInteger Amount { get; init; }

        public AddToUserBalanceTransaction(string publicKeyBase58, BigInteger amount)
        {
            PublicKeyBase58 = publicKeyBase58;
            Amount = amount;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            stream.Write(Amount.ToByteArray());
        }
    }
}
