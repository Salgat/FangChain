using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class TransferToUserBalanceTransaction : TransactionModel
    {
        public override TransactionType TransactionType => TransactionType.TransferToUserBalance;
        public string FromPublicKeyBase58 { get; init; }
        public string ToPublicKeyBase58 { get; init; }
        public BigInteger Amount { get; init; }

        public TransferToUserBalanceTransaction(string fromPublicKeyBase58, string toPublicKeyBase58, BigInteger amount, string transactionId) : base(transactionId)
        {
            FromPublicKeyBase58 = fromPublicKeyBase58;
            ToPublicKeyBase58 = toPublicKeyBase58;
            Amount = amount;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(Id));
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(FromPublicKeyBase58));
            stream.Write(Encoding.ASCII.GetBytes(ToPublicKeyBase58));
            stream.Write(Amount.ToByteArray());
        }
    }
}
