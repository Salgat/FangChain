using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    /// <summary>
    /// Maintains the state of pending transactions that have not been discarded. These pending transactions are considered for this and future proposed blocks.
    /// </summary>
    public interface IPendingTransactions
    {
        IEnumerable<PendingTransaction> PendingTransactions { get; }

        bool TryAdd(PendingTransaction transaction);
        bool TryRemove(PendingTransaction transaction);
        void PurgeExpiredTransactions(DateTimeOffset dateTimeOffset, long currentBlockIndex);
    }
}
