using System.Collections.Immutable;

namespace FangChain.Server
{
    public interface IBlockchainState
    {
        ImmutableArray<BlockModel> GetBlockchain();
        void SetBlockchain(IEnumerable<BlockModel> blockchain);
    }
}