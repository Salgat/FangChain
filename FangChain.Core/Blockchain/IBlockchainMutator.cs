using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public interface IBlockchainMutator
    {
        public void ProcessPendingTransactions();
        public void CompactBlockchain(long fromIndex, long toIndex);
    }
}
