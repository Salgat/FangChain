using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace FangChain
{
    public interface IBlockchainState
    {
        ConcurrentDictionary<string, string> UserAliasToPublicKeyBase58 { get; }
        ConcurrentDictionary<string, UserSummary> UserSummaries { get; set; } 

        ImmutableArray<BlockModel> GetBlockchain();
        void SetBlockchain(IEnumerable<BlockModel> blockchain);
        bool IsTransactionConfirmed(string hash);
    }
}