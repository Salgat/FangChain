using System.Collections.Immutable;

namespace FangChain.CLI
{
    public interface IValidator
    {
        bool IsBlockchainValid(ImmutableArray<BlockModel> blocks);
        bool IsBlockValid(BlockModel block);
        bool IsTransactionValid(TransactionModel transaction);
    }
}