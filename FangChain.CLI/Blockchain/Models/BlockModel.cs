using NokitaKaze.Base58Check;
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    public class BlockModel
    {
        public BlockModel(int index, string previousBlockHash, ImmutableArray<TransactionModel> transactions, ImmutableArray<SignatureModel> signatures)
        {
            BlockIndex = index;
            PreviousBlockHash = previousBlockHash;
            Transactions = transactions;
            Signatures = signatures;
        }

        /// <summary>
        /// Creates the initial block for a blockchain, including a signed transaction designating the creator as the SuperAdministrator.
        /// </summary>
        /// <param name="initialUserKeys"></param>
        /// <returns></returns>
        public static BlockModel CreateInitialBlock(PublicAndPrivateKeys initialUserKeys)
        {
            using var secp256k1 = new Secp256k1();

            // Initial block promotes creator of blockchain
            var firstUserTransaction = new TransactionModel[]
            {
                new PromoteUserTransaction(Base58CheckEncoding.Encode(initialUserKeys.PublicKey), UserDesignation.SuperAdministrator)
            }.ToImmutableArray();
            var transactionHash = firstUserTransaction.Single().GetHash();
            var transactionSignature = new byte[64];
            secp256k1.Sign(transactionSignature, transactionHash, initialUserKeys.PrivateKey);
            var transactionSignatureModel = new[]
            {
                new SignatureModel(Base58CheckEncoding.Encode(initialUserKeys.PublicKey), Base58CheckEncoding.Encode(transactionSignature))
            }.ToImmutableArray();
            firstUserTransaction.Single().SetSignatures(transactionSignatureModel);

            // Get the block hash (excluding the block signature) and sign it by the blockchain creator
            var initialBlockHash = GetHash(0, string.Empty, firstUserTransaction);
            var signature = new byte[64];
            secp256k1.Sign(signature, initialBlockHash, initialUserKeys.PrivateKey);
            var initialBlockSignature = new[]
            {
                new SignatureModel(Base58CheckEncoding.Encode(initialUserKeys.PublicKey), Base58CheckEncoding.Encode(signature))
            }.ToImmutableArray();

            return new BlockModel(0, "", firstUserTransaction, initialBlockSignature);
        }

        /// <summary>
        /// Returns the hash of the proposed block (excluding the signatures).
        /// </summary>
        /// <param name="index"></param>
        /// <param name="previousBlockHash"></param>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public static byte[] GetHash(int index, string previousBlockHash, ImmutableArray<TransactionModel> transactions)
        {
            // Return a SHA-256 hash of the block
            using var contentsToHash = new MemoryStream();
            contentsToHash.Write(BitConverter.GetBytes(index));
            contentsToHash.Write(Encoding.ASCII.GetBytes(previousBlockHash));
            foreach (var transaction in transactions)
            {
                contentsToHash.Write(transaction.GetBytes());
            }

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(contentsToHash.ToArray());
            return hash;
        }

        public int BlockIndex { get; }
        public string PreviousBlockHash { get; }
        public ImmutableArray<TransactionModel> Transactions { get; }
        public ImmutableArray<SignatureModel> Signatures { get; private set; }
    }
}
