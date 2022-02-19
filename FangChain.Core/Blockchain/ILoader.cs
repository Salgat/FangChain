using System.Collections.Immutable;

namespace FangChain
{
    public interface ILoader
    {
        Task<ImmutableArray<BlockModel>> LoadBlockchainAsync(string directory, CancellationToken cancellationToken = default);
        Task<BlockModel> LoadBlockAsync(string blockPath, CancellationToken cancellationToken = default);
        Task<TransactionModel> LoadTransactionAsync(string transactionPath, CancellationToken cancellationToken = default);
        Task<Base58PublicAndPrivateKeys> LoadKeysAsync(string keysPath, CancellationToken cancellationToken = default);
    }
}