﻿using Microsoft.AspNetCore.Mvc;

namespace FangChain.Server
{
    [ApiController]
    [Route("blockchain")]
    public class BlockchainController : ControllerBase
    {
        private const int MaxPageSize = 100;

        private readonly IBlockchainState _blockchainState;

        public BlockchainController(IBlockchainState blockchainState) 
        {
            _blockchainState = blockchainState;
        }

        [HttpGet]
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
    }
}