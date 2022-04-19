using Newtonsoft.Json.Linq;
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
    public class BlockModel
    {
        public const string CreatorAlias = "creator";

        public long BlockIndex { get; }
        public string PreviousBlockHashBase58 { get; }
        public ImmutableArray<TransactionModel> Transactions { get; } = ImmutableArray<TransactionModel>.Empty;
        public ImmutableArray<SignatureModel> Signatures { get; private set; } = ImmutableArray<SignatureModel>.Empty;
        public virtual bool IsCompacted => false;

        public BlockModel() { }

        public BlockModel(long index, string previousBlockHashBase58, IEnumerable<TransactionModel> transactions)
        {
            BlockIndex = index;
            PreviousBlockHashBase58 = previousBlockHashBase58;
            Transactions = transactions.ToImmutableArray();
        }

        public BlockModel(JObject blockJson)
        {
            BlockIndex = blockJson.Value<int>(nameof(BlockIndex));
            PreviousBlockHashBase58 = blockJson.Value<string>(nameof(PreviousBlockHashBase58));

            var transactions = blockJson[nameof(Transactions)] as JArray;
            Transactions = transactions.Select(t => Loader.DeserializeTransaction((JObject)t)).ToImmutableArray();


            var signatures = blockJson[nameof(Signatures)] as JArray;
            Signatures = signatures.Select(s => s.ToObject<SignatureModel>()).ToImmutableArray();
        }

        /// <summary>
        /// Creates the initial block for a blockchain, including a signed transaction designating the creator as the SuperAdministrator.
        /// </summary>
        /// <param name="initialUserKeys"></param>
        /// <returns></returns>
        public static BlockModel CreateInitialBlock(PublicAndPrivateKeys initialUserKeys)
        {
            using var secp256k1 = new Secp256k1();
            var base58PublicKey = initialUserKeys.GetBase58PublicKey();

            // Initial block designates creator of blockchain
            var firstUserTransactions = new List<TransactionModel>
            {
                new DesignateUserTransaction(base58PublicKey, UserDesignation.SuperAdministrator)
            };
            var designateTransactionSignature = firstUserTransactions.Single().CreateSignature(initialUserKeys);
            firstUserTransactions.Single().SetSignatures(new [] { designateTransactionSignature });

            // Set block creator's alias to "creator"
            var creatorAliasTransaction = new SetAliasTransaction(base58PublicKey, CreatorAlias);
            var creatorAliasTransactionSignature = creatorAliasTransaction.CreateSignature(initialUserKeys);
            creatorAliasTransaction.SetSignatures(new [] { creatorAliasTransactionSignature });
            firstUserTransactions.Add(creatorAliasTransaction);

            // Sign initial block
            var block = new BlockModel(0, "", firstUserTransactions);
            var blockSignature = block.CreateSignature(initialUserKeys);
            block.SetSignatures(new[] { blockSignature });

            return block;
        }

        /// <summary>
        /// Returns the hash of the proposed block (excluding the signatures).
        /// </summary>
        /// <param name="index"></param>
        /// <param name="previousBlockHash"></param>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public virtual byte[] GetHash()
        {
            // Return a SHA-256 hash of the block
            using var contentsToHash = new MemoryStream();
            contentsToHash.Write(BitConverter.GetBytes(IsCompacted));
            contentsToHash.Write(BitConverter.GetBytes(BlockIndex));
            contentsToHash.Write(Encoding.ASCII.GetBytes(PreviousBlockHashBase58));
            foreach (var transaction in Transactions)
            {
                contentsToHash.Write(transaction.GetBytes());
            }

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(contentsToHash.ToArray());
            return hash;
        }

        public string GetHashString()
        {
            var hash = GetHash();
            return Convert.ToHexString(hash);
        }
        
        public void AddSignature(PublicAndPrivateKeys keys)
        {
            var signature = CreateSignature(keys);
            Signatures = Signatures.Concat(new[] { signature }).ToImmutableArray();
        }

        public SignatureModel CreateSignature(PublicAndPrivateKeys keys)
        {
            using var secp256k1 = new Secp256k1();

            var blockHash = GetHash();
            var blockSignature = new byte[64];
            secp256k1.Sign(blockSignature, blockHash, keys.PrivateKey);
            return new SignatureModel(keys.GetBase58PublicKey(), Base58CheckEncoding.Encode(blockSignature));
        }

        public void SetSignatures(IEnumerable<SignatureModel> signatures)
        {
            Signatures = signatures.ToImmutableArray();
        }
    }
}
