
namespace FangChain
{
    public interface IBlockchainCreation
    {
        Task CreateBlockChainAsync(DirectoryInfo blockchainDirectory, string credentialsPath, CancellationToken cancellationToken = default);
        Task<bool> PersistBlockAsync(DirectoryInfo blockchainDirectory, BlockModel block, CancellationToken cancellationToken = default);
        Task<BlockModel> ReadBlockAsync(string blockPath, CancellationToken cancellationToken = default);
    }
}