using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class Compactor : ICompactor
    {
        private readonly PublicAndPrivateKeys _hostKeys;

        public Compactor(ICredentialsManager credentialsManager)
        {
            _hostKeys = credentialsManager.GetHostCredentials();
        }

        public ImmutableArray<BlockModel> Compact(ImmutableArray<BlockModel> blockchain, long fromIndex, long toIndex)
        {
            var previousBlocks = blockchain.Where(b => b.BlockIndex < fromIndex);
            var nextBlocks = blockchain.Where(b => b.BlockIndex > toIndex);
            var nextBlock = nextBlocks.FirstOrDefault();
            if (nextBlock is null)
            {
                throw new InvalidOperationException("Compacting blocks must not include the last block in the blockchain.");
            }

            var blocksToCompact = blockchain
                .Where(block => block.BlockIndex >= fromIndex && block.BlockIndex <= toIndex)
                .ToImmutableArray();
            var compactedBlock = CompactedBlockModel.FromBlocks(blocksToCompact, nextBlock.GetHashString());
            var compactedBlockchain = previousBlocks
                .Concat(new[] { compactedBlock })
                .Concat(nextBlocks);
            return compactedBlockchain.ToImmutableArray();
        }
    }
}
