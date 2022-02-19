using System.Collections.Immutable;

namespace FangChain.Server
{
    public class BlockchainState : IBlockchainState
    {
        private ImmutableArray<BlockModel> _blockChain = ImmutableArray<BlockModel>.Empty;

        public ImmutableArray<BlockModel> GetBlockchain() => _blockChain;
        public void SetBlockchain(IEnumerable<BlockModel> blockchain) => _blockChain = blockchain.ToImmutableArray();
    }
}
