using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class CompactedBlockModel : BlockModel
    {
        public override bool IsCompacted => true;
        public long NextBlockIndex { get; }
        public string NextBlockHashBase58 { get; }
        public ImmutableArray<string> CompactedBlockHashes { get; init; } // An ordered list of all compacted blocks that this compacted block replaces

        public CompactedBlockModel() { }

        public CompactedBlockModel(
            long index, 
            long nextIndex, 
            string previousBlockHashBase58,
            string nextBlockhashBase58,
            IEnumerable<TransactionModel> transactions,
            IEnumerable<string> compactedBlockHashes) 
            : base(index, previousBlockHashBase58, transactions) 
        {
            NextBlockIndex = nextIndex;
            NextBlockHashBase58 = nextBlockhashBase58;
            CompactedBlockHashes = compactedBlockHashes.ToImmutableArray();
        }

        public CompactedBlockModel(JObject blockJson) : base(blockJson)
        {
            NextBlockIndex = blockJson.Value<int>(nameof(NextBlockIndex));
            NextBlockHashBase58 = blockJson.Value<string>(nameof(NextBlockHashBase58));
            CompactedBlockHashes = blockJson[nameof(CompactedBlockHashes)].ToObject<IEnumerable<string>>().ToImmutableArray();
        }

        public static CompactedBlockModel FromBlocks(ImmutableArray<BlockModel> blocks, string nextBlockhashBase58)
        {
            // Take an ordered list of blocks and build up the final change in state due to these blocks
            var balanceChanges = new Dictionary<string, BigInteger>();
            var nftChanges = new Dictionary<string, (string fromStart, string toFinal, string contents)>(); // Key => TokenId, Value = public addresses
            var aliasChanges = new Dictionary<string, string>();
            var disabledUsers = new HashSet<string>();
            var enabledUsers = new HashSet<string>();
            var userDesignations = new Dictionary<string, UserDesignation>();

            void AddToUserBalance(string userPublicKeyBase58, BigInteger amount)
            {
                if (balanceChanges.TryGetValue(userPublicKeyBase58, out var balance))
                {
                    balanceChanges[userPublicKeyBase58] = balance + amount;
                }
                else
                {
                    balanceChanges[userPublicKeyBase58] = amount;
                }
            }

            void ProcessTransaction(TransactionModel? transaction)
            {
                if (transaction is SetAliasTransaction setAliasTransaction)
                {
                    aliasChanges[setAliasTransaction.PublicKeyBase58] = setAliasTransaction.Alias;
                }
                else if (transaction is AddToUserBalanceTransaction addToUserBalanceTransaction)
                {
                    AddToUserBalance(addToUserBalanceTransaction.PublicKeyBase58, addToUserBalanceTransaction.Amount);
                }
                else if (transaction is TransferToUserBalanceTransaction transferToUserBalanceTransaction)
                {
                    AddToUserBalance(transferToUserBalanceTransaction.FromPublicKeyBase58, -1 * transferToUserBalanceTransaction.Amount);
                    AddToUserBalance(transferToUserBalanceTransaction.ToPublicKeyBase58, transferToUserBalanceTransaction.Amount);
                }
                else if (transaction is AddTokenTransaction addTokenTransaction)
                {
                    nftChanges[addTokenTransaction.TokenId] = (string.Empty, addTokenTransaction.PublicKeyBase58, addTokenTransaction.Contents);
                }
                else if (transaction is RemoveTokenTransaction removeTokenTransaction)
                {
                    if (nftChanges.ContainsKey(removeTokenTransaction.TokenId))
                    {
                        nftChanges.Remove(removeTokenTransaction.TokenId);
                    }
                }
                else if (transaction is TransferTokenTransaction transferTokenTransaction)
                {
                    if (nftChanges.TryGetValue(transferTokenTransaction.TokenId, out var tokenInfo))
                    {
                        nftChanges[transferTokenTransaction.TokenId] = (tokenInfo.fromStart, transferTokenTransaction.ToPublicKeyBase58, string.Empty);
                    }
                    else
                    {
                        nftChanges[transferTokenTransaction.TokenId] = (transferTokenTransaction.FromPublicKeyBase58, transferTokenTransaction.ToPublicKeyBase58, string.Empty);
                    }
                }
                else if (transaction is EnableUserTransaction enableUserTransaction)
                {
                    disabledUsers.Remove(enableUserTransaction.PublicKeyBase58);
                    enabledUsers.Add(enableUserTransaction.PublicKeyBase58);
                }
                else if (transaction is DisableUserTransaction disableUserTransaction)
                {
                    enabledUsers.Remove(disableUserTransaction.PublicKeyBase58);
                    disabledUsers.Add(disableUserTransaction.PublicKeyBase58);
                }
                else if (transaction is DesignateUserTransaction designateUserTransaction)
                {
                    userDesignations[designateUserTransaction.PublicKeyBase58] = designateUserTransaction.UserDesignation;
                }
            }

            foreach (var block in blocks)
            {
                foreach (var transaction in block.Transactions)
                {
                    if (transaction is LumpedTransaction lumpedTransaction)
                    {
                        foreach (var entry in lumpedTransaction.Transactions)
                        {
                            ProcessTransaction(entry);
                        }
                    }
                    else
                    {
                        ProcessTransaction(transaction);
                    }
                }
            }

            // Construct minimal transactions to summarize changes
            var transactions = new List<TransactionModel>();
            foreach (var (user, amount) in balanceChanges)
            {
                var transaction = new AddToUserBalanceTransaction(user, amount);
                transactions.Add(transaction);
            }
            foreach (var (tokenId, (fromUser, toUser, contents)) in nftChanges)
            {
                if (!string.IsNullOrWhiteSpace(fromUser))
                {
                    var transaction = new TransferTokenTransaction(fromUser, toUser, tokenId);
                    transactions.Add(transaction);
                } 
                else
                {
                    var transaction = new AddTokenTransaction(toUser, tokenId, contents);
                    transactions.Add(transaction);
                }
            }
            foreach (var (user, alias) in aliasChanges)
            {
                var transaction = new SetAliasTransaction(user, alias);
                transactions.Add(transaction);
            }
            foreach (var user in disabledUsers)
            {
                var transaction = new DisableUserTransaction(user);
                transactions.Add(transaction);
            }
            foreach (var user in enabledUsers)
            {
                var transaction = new EnableUserTransaction(user);
                transactions.Add(transaction);
            }
            foreach (var (user, designation) in userDesignations)
            {
                var transaction = new DesignateUserTransaction(user, designation);
                transactions.Add(transaction);
            }

            return new CompactedBlockModel(
                blocks.First().BlockIndex,
                blocks.Last().BlockIndex + 1,
                blocks.First().PreviousBlockHashBase58,
                nextBlockhashBase58,
                transactions,
                blocks.Select(b => b.GetHashString()).ToImmutableArray());
        }

        public override byte[] GetHash()
        {
            // Return a SHA-256 hash of the block
            using var contentsToHash = new MemoryStream();
            contentsToHash.Write(BitConverter.GetBytes(IsCompacted));
            contentsToHash.Write(BitConverter.GetBytes(BlockIndex));
            contentsToHash.Write(BitConverter.GetBytes(NextBlockIndex));
            contentsToHash.Write(Encoding.ASCII.GetBytes(PreviousBlockHashBase58));
            contentsToHash.Write(Encoding.ASCII.GetBytes(NextBlockHashBase58));
            foreach (var blockHash in CompactedBlockHashes)
            {
                contentsToHash.Write(Encoding.ASCII.GetBytes(blockHash));
            }
            foreach (var transaction in Transactions)
            {
                contentsToHash.Write(transaction.GetBytes());
            }

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(contentsToHash.ToArray());
            return hash;
        }
    }
}
