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
        public string PublicKeyBase58 { get; }
        public UserDesignation UserDesignation { get; }

        public PromoteUserTransaction(string publicKeyBase58, UserDesignation designation)
        {
            PublicKeyBase58 = publicKeyBase58;
            UserDesignation = designation;
        }

        public override byte[] GetHash()
        {
            using var contentsToHash = new MemoryStream();
            contentsToHash.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            contentsToHash.Write(BitConverter.GetBytes((int)UserDesignation));

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(contentsToHash.ToArray());
            return hash;
        }

        public override byte[] GetBytes()
        {
            using var contents = new MemoryStream();
            contents.Write(Encoding.ASCII.GetBytes(PublicKeyBase58));
            contents.Write(BitConverter.GetBytes((int)UserDesignation));
            foreach (var signature in Signatures)
            {
                contents.Write(signature.GetBytes());
            }
            return contents.ToArray();
        }
    }
}
