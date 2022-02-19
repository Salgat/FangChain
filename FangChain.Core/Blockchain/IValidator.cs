﻿using System.Collections.Immutable;

namespace FangChain
{
    public interface IValidator
    {
        bool IsBlockchainValid(ImmutableArray<BlockModel> blocks);
        bool IsBlockValid(BlockModel block);
        bool IsTransactionValid(TransactionModel transaction);
    }
}