
namespace FangChain.CLI
{
    public interface IBlockchainCreation
    {
        Task CreateBlockChainAsync(DirectoryInfo blockchainDirectory, string credentialsPath);
        Task PersistBlockAsync(DirectoryInfo blockchainDirectory, BlockModel block);
        Task<BlockModel> ReadBlockAsync(string blockPath);
    }
}