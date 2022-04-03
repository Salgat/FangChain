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
                    foreach (var transaction in block.Transactions)
                    {
                        _confirmedTransactions[transaction.GetHashString()] = transaction;
                    }
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
                foreach (var transaction in block.Transactions)
                {
                    _confirmedTransactions[transaction.GetHashString()] = transaction;

                    // TODO: Update user summaries
                }
            }

            return true;
        }

        public bool IsTransactionConfirmed(string hash) => _confirmedTransactions.ContainsKey(hash);
    }
}
