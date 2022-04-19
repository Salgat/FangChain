using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class Compactor : ICompactor
    {
        public ImmutableArray<BlockModel> Compact(ImmutableArray<BlockModel> blockchain, long fromIndex, long toIndex)
        {
            var blocksToCompact = blockchain.Where(block => block.BlockIndex >= fromIndex && block.BlockIndex <= toIndex);
            var compactedBlock = new CompactedBlockModel();

            throw new NotImplementedException();
        }
    }
}
