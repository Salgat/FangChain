using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    public class PromoteUserTransaction : TransactionModel
    {
        public override TransactionType TransactionType { get => TransactionType.PromoteUser; }
        public string PublicKeyBase58 { get; init; }
        public UserDesignation UserDesignation { get; init; }

        public PromoteUserTransaction(string publicKeyBase58, UserDesignation designation)
        {
            PublicKeyBase58 = publicKeyBase58;
            UserDesignation = designation;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            stream.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            stream.Write(BitConverter.GetBytes((int)UserDesignation));
        }
    }
}
