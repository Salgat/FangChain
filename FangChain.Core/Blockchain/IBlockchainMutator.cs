using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public interface IBlockchainMutator
    {
        public Task ProcessPendingTransactionsAsync(bool persistChanges, CancellationToken cancellationToken);
        public Task CompactBlockchainAsync(long fromIndex, long toIndex, bool persistChanges, CancellationToken cancellationToken);
    }
}
