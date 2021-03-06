using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class DesignateUserTransaction : TransactionModel
    {
        public override TransactionType TransactionType => TransactionType.DesignateUser;
        public string PublicKeyBase58 { get; init; }
        public UserDesignation UserDesignation { get; init; }

        public DesignateUserTransaction(string publicKeyBase58, UserDesignation designation, string transactionId) : base(transactionId)
        {
            PublicKeyBase58 = publicKeyBase58;
            UserDesignation = designation;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(Id));
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            stream.Write(BitConverter.GetBytes((int)UserDesignation));
        }
    }
}
