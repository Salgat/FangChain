using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class PendingTransactionsInMemory : IPendingTransactions
    {
        private readonly ConcurrentDictionary<string, PendingTransaction> _pendingTransactions = new(); // Key = SHA256 hash string
        private readonly IValidator _validator;

        public IEnumerable<PendingTransaction> PendingTransactions { get => _pendingTransactions.Values; }

        public PendingTransactionsInMemory(IValidator validator)
        {
            _validator = validator;
        }

        public bool TryAdd(PendingTransaction transaction)
        {
            if (!_validator.IsTransactionValid(transaction.Transaction)) return false;

            var transactionHash = transaction.Transaction.GetHashString();
            _pendingTransactions.TryAdd(transactionHash, transaction);
            return true;
        }

        public bool TryRemove(PendingTransaction transaction)
            => _pendingTransactions.TryRemove(transaction.Transaction.GetHashString(), out _);

        public void PurgeExpiredTransactions(DateTimeOffset dateTimeOffset, long currentBlockIndex)
        {
            foreach (var transaction in _pendingTransactions)
            {
                if (transaction.Value.ExpireAfter <= dateTimeOffset || transaction.Value.MaxBlockIndexToAddTo < currentBlockIndex)
                {
                    _pendingTransactions.TryRemove(transaction.Key, out _);
                }
            }
        }
    }
}
