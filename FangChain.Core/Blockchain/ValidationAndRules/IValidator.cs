using System.Collections.Immutable;

namespace FangChain
{
    public interface IValidator
    {
        bool IsBlockAdditionValid(IEnumerable<BlockModel> blockchain, BlockModel proposedBlock);
        bool IsBlockchainValid(IEnumerable<BlockModel> blockchain);
        bool IsBlockValid(BlockModel block);
        bool IsTransactionValid(TransactionModel transaction);
    }
}