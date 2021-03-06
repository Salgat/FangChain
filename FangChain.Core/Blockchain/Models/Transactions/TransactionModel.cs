using NokitaKaze.Base58Check;
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public abstract class TransactionModel
    {
        public string Id { get; set; } = string.Empty;
        public abstract TransactionType TransactionType { get; }
        public ImmutableArray<SignatureModel> Signatures { get; set; } = ImmutableArray<SignatureModel>.Empty;

        public TransactionModel(string transactionId)
        {
            Id = transactionId;
        }
        
        public void SetSignatures(IEnumerable<SignatureModel> signatures)
        {
            Signatures = signatures.ToImmutableArray();
        }

        public void AddSignature(SignatureModel signature)
        {
            Signatures = Signatures.Concat(new[] { signature }).ToImmutableArray();
        }

        public void AddSignature(PublicAndPrivateKeys keys)
        {
            var signature = CreateSignature(keys);
            AddSignature(signature);
        }

        protected abstract void PopulateBytesFromProperties(MemoryStream stream);

        /// <summary>
        /// Returns the bytes of the transaction, including the signatures.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            using var contents = new MemoryStream();
            PopulateBytesFromProperties(contents);
            foreach (var signature in Signatures)
            {
                contents.Write(signature.GetBytes());
            }
            return contents.ToArray();
        }

        /// <summary>
        /// Returns the hash of the Transaction (not including Signatures).
        /// </summary>
        /// <returns></returns>

        public byte[] GetHash()
        {
            using var contentsToHash = new MemoryStream();
            PopulateBytesFromProperties(contentsToHash);

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(contentsToHash.ToArray());
            return hash;
        }

        public string GetHashString()
        {
            var hash = GetHash();
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Returns the signature for the transaction.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public SignatureModel CreateSignature(PublicAndPrivateKeys keys)
        {
            var transactionHash = this.GetHash();
            var transactionSignature = new byte[64];
            Secp256k1Singleton.Execute(secp256k1 => secp256k1.Sign(transactionSignature, transactionHash, keys.PrivateKey));
            return new SignatureModel(keys.GetBase58PublicKey(), Base58CheckEncoding.Encode(transactionSignature));
        }

        public virtual bool IsValid(bool validateSignatures)
        {
            throw new NotImplementedException();
        }
    }
}
