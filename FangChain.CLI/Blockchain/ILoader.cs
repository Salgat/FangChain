using System.Collections.Immutable;

namespace FangChain.CLI
{
    public interface ILoader
    {
        Task<ImmutableArray<BlockModel>> LoadBlockchainAsync(string directory);
    }
}