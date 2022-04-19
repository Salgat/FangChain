using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace FangChain
{
    public class BlockchainStateInMemory : IBlockchainState
    {
        private readonly object _lock = new();
        private readonly IValidator _validator;

        private List<BlockModel> _blockchain = null;
        private readonly ConcurrentDictionary<string, TransactionModel> _confirmedTransactions = new();

        public BlockchainStateInMemory(IValidator validator)
        {
            _validator = validator;
        }

        public ImmutableArray<BlockModel> GetBlockchain() => _blockchain?.ToImmutableArray() ?? ImmutableArray<BlockModel>.Empty;

        public void SetBlockchain(IEnumerable<BlockModel> blockchain) 
        {
            lock (_lock)
            {
                _blockchain = new List<BlockModel>(blockchain);
                foreach (var block in blockchain)
                {
                    UpdateState(block.Transactions);
                }
            }
        }

        public ConcurrentDictionary<string, string> UserAliasToPublicKeyBase58 { get; } = new();
        public ConcurrentDictionary<string, UserSummary> UserSummaries { get; set; } = new(); // Key = PublicKeyBase58
        public ConcurrentDictionary<string, string> TokenOwners { get; set; } = new(); // TokenId => PublicKeyBase58

        /// <summary>
        /// Tries adding the block to the blockchain. If validation fails, the blockchain remains unchanged and returns false.
        /// </summary>
        /// <param name="block"></param>
        /// <exception cref="NotImplementedException"></exception>
        public bool TryAddBlock(BlockModel block)
        {
            lock (_lock)
            {
                if (!_validator.IsBlockAdditionValid(_blockchain, block)) return false;
                _blockchain.Add(block);
                UpdateState(block.Transactions);
            }

            return true;
        }

        public bool IsTransactionConfirmed(string hash) => _confirmedTransactions.ContainsKey(hash);

        private void UpdateState(ImmutableArray<TransactionModel> transactions)
        {
            void UpdateUserSummary(string publicKeyBase58, Action<UserSummary> update)
            {
                UserSummaries.AddOrUpdate(publicKeyBase58, publicKey => 
                {
                    var userSummary = new UserSummary();
                    update(userSummary);
                    return userSummary;
                }, (publicKey, userSummary) =>
                {
                    update(userSummary);
                    return userSummary;
                });
            }

            void HandleTransaction(TransactionModel? transaction)
            {
                _confirmedTransactions[transaction.GetHashString()] = transaction;
                if (transaction is SetAliasTransaction setAliasTransaction)
                {
                    UpdateUserSummary(setAliasTransaction.PublicKeyBase58,
                        userSummary => userSummary.Alias = setAliasTransaction.Alias);
                    UserAliasToPublicKeyBase58[setAliasTransaction.Alias] = setAliasTransaction.PublicKeyBase58;
                }
                else if (transaction is DesignateUserTransaction designateUserTransaction)
                {
                    UpdateUserSummary(designateUserTransaction.PublicKeyBase58,
                        userSummary => userSummary.Designation = designateUserTransaction.UserDesignation);
                }
                else if (transaction is AddToUserBalanceTransaction addToUserBalanceTransaction)
                {
                    UpdateUserSummary(addToUserBalanceTransaction.PublicKeyBase58,
                        userSummary => userSummary.Balance += addToUserBalanceTransaction.Amount);
                }
                else if (transaction is EnableUserTransaction enableUserTransaction)
                {
                    UpdateUserSummary(enableUserTransaction.PublicKeyBase58,
                        userSummary => userSummary.Disabled = false);
                }
                else if (transaction is DisableUserTransaction disableUserTransaction)
                {
                    UpdateUserSummary(disableUserTransaction.PublicKeyBase58,
                        userSummary => userSummary.Disabled = true);
                }
                else if (transaction is AddTokenTransaction addTokenTransaction)
                {
                    UpdateUserSummary(addTokenTransaction.PublicKeyBase58,
                        userSummary => userSummary.Tokens = userSummary
                            .Tokens
                            .Add(addTokenTransaction.TokenId, addTokenTransaction.Contents));
                    TokenOwners[addTokenTransaction.TokenId] = addTokenTransaction.PublicKeyBase58;
                }
                else if (transaction is RemoveTokenTransaction removeTokenTransaction)
                {
                    UpdateUserSummary(removeTokenTransaction.PublicKeyBase58,
                        userSummary => userSummary.Tokens = userSummary
                            .Tokens
                            .Remove(removeTokenTransaction.TokenId));
                    TokenOwners.TryRemove(removeTokenTransaction.TokenId, out var _);
                }
                else if (transaction is TransferTokenTransaction transferTokenTransaction)
                {
                    var tokenContents = default(string);
                    UpdateUserSummary(transferTokenTransaction.FromPublicKeyBase58,
                        userSummary =>
                        {
                            tokenContents = userSummary.Tokens[transferTokenTransaction.TokenId];
                            userSummary.Tokens = userSummary
                                .Tokens
                                .Remove(transferTokenTransaction.TokenId);
                        });
                    UpdateUserSummary(transferTokenTransaction.ToPublicKeyBase58,
                        userSummary => userSummary.Tokens = userSummary
                            .Tokens
                            .Add(transferTokenTransaction.TokenId, tokenContents));
                    TokenOwners[transferTokenTransaction.TokenId] = transferTokenTransaction.ToPublicKeyBase58;
                }
            }

            foreach (var transaction in transactions)
            {
                if (transaction is LumpedTransaction lumpedTransaction)
                {
                    foreach (var entry in lumpedTransaction.Transactions)
                    {
                        HandleTransaction(entry);
                    }
                } 
                else
                {
                    HandleTransaction(transaction);
                }
            }
        }
    }
}
