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

            foreach (var transaction in transactions)
            {
                _confirmedTransactions[transaction.GetHashString()] = transaction;
                if (transaction.TransactionType is TransactionType.AddAlias)
                {
                    var addAliasTransaction = (AddAliasTransaction)transaction;
                    UpdateUserSummary(addAliasTransaction.PublicKeyBase58, 
                        userSummary => userSummary.Alias = addAliasTransaction.Alias);
                    UserAliasToPublicKeyBase58[addAliasTransaction.Alias] = addAliasTransaction.PublicKeyBase58;
                }
                else if (transaction.TransactionType is TransactionType.PromoteUser)
                {
                    var promoteUserTransaction = (PromoteUserTransaction)transaction;
                    UpdateUserSummary(promoteUserTransaction.PublicKeyBase58, 
                        userSummary => userSummary.Designation = promoteUserTransaction.UserDesignation);
                }
                else if (transaction.TransactionType is TransactionType.AddToUserBalance)
                {
                    var addToUserBalanceTransaction = (AddToUserBalanceTransaction)transaction;
                    UpdateUserSummary(addToUserBalanceTransaction.PublicKeyBase58,
                        userSummary => userSummary.Balance += addToUserBalanceTransaction.Amount);
                }
            }
        }
    }
}
