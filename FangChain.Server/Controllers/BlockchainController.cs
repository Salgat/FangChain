using Microsoft.AspNetCore.Mvc;

namespace FangChain
{
    [ApiController]
    [Route("blockchain")]
    public class BlockchainController : ControllerBase
    {
        private const int MaxPageSize = 100;

        private readonly IBlockchainState _blockchainState;
        private readonly IBlockchainMutator _blockchainMutator;

        public BlockchainController(
            IBlockchainState blockchainState,
            IBlockchainMutator blockchainMutator) 
        {
            _blockchainState = blockchainState;
            _blockchainMutator = blockchainMutator;
        }

        [HttpGet, Route("blocks")]
        public IEnumerable<BlockModel> GetBlocks(int fromIndex, int toIndex)
        {
            if (toIndex - fromIndex + 1 > MaxPageSize)
            {
                throw new ArgumentException($"Max index range of '{MaxPageSize}' exceeded.");
            }

            var blockchain = _blockchainState.GetBlockchain();
            var selectedBlocks = blockchain.Where(block => block.BlockIndex >= fromIndex && block.BlockIndex <= toIndex);
            return selectedBlocks;
        }

        [HttpPost, Route("compact")]
        public Task Compact(long fromIndex, long toIndex, CancellationToken cancellationToken)
            => _blockchainMutator.CompactBlockchainAsync(fromIndex, toIndex, true, cancellationToken);
    }
}
