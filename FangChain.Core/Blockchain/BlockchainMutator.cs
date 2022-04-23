using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class BlockchainMutator : IBlockchainMutator
    {
        private readonly IBlockchainState _blockchainState;
        private readonly IBlockchainRules _blockchainRules;
        private readonly IPendingTransactions _pendingTransactions;
        private readonly ICredentialsManager _credentialsManager;
        private readonly ICompactor _compactor;
        private readonly IBlockchainCreation _blockchainCreation;
        private readonly IConfigurationManager _configurationManager;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public BlockchainMutator(
            IBlockchainState blockchainState,
            IBlockchainRules blockchainRules,
            IPendingTransactions pendingTransactions,
            ICredentialsManager credentialsManager,
            ICompactor compactor,
            IBlockchainCreation blockchainCreation,
            IConfigurationManager configurationManager)
        {
            _blockchainState = blockchainState;
            _blockchainRules = blockchainRules;
            _pendingTransactions = pendingTransactions;
            _credentialsManager = credentialsManager;
            _compactor = compactor;
            _blockchainCreation = blockchainCreation;
            _configurationManager = configurationManager;
        }

        public async Task ProcessPendingTransactionsAsync(bool persistChanges, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var currentBlockchain = _blockchainState.GetBlockchain();
                if (!currentBlockchain.Any()) return;

                var nextBlockIndex = currentBlockchain.Last().BlockIndex + 1;
                var previousBlockHash = currentBlockchain.Last().GetHashString();

                var allowedTransactions = new List<TransactionModel>();
                BlockModel? proposedBlock = default;
                _pendingTransactions.PurgeExpiredTransactions(DateTimeOffset.UtcNow, nextBlockIndex);
                foreach (var proposedTransaction in _pendingTransactions.PendingTransactions)
                {
                    proposedBlock = new BlockModel(nextBlockIndex, previousBlockHash, allowedTransactions.Concat(new[] { proposedTransaction.Transaction }));
                    if (_blockchainRules.IsBlockAdditionValid(proposedBlock))
                    {
                        allowedTransactions.Add(proposedTransaction.Transaction);
                    }
                }
                if (!allowedTransactions.Any()) return;
                proposedBlock = new BlockModel(nextBlockIndex, previousBlockHash, allowedTransactions);
                proposedBlock.AddSignature(_credentialsManager.GetHostCredentials());

                // Add proposed transactions as a new block
                if (proposedBlock == default) return;
                var proposedBlockchain = currentBlockchain.Add(proposedBlock);
                _blockchainState.SetBlockchain(proposedBlockchain);

                // Flush pending transactions (as they are either included in the block or rejected)
                foreach (var proposedTransaction in _pendingTransactions.PendingTransactions)
                {
                    _pendingTransactions.TryRemove(proposedTransaction);
                }

                await PersistBlockchainAsync(proposedBlockchain, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task CompactBlockchainAsync(long fromIndex, long toIndex, bool persistChanges, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var blockchain = _blockchainState.GetBlockchain();
                var compactedBlockchain = _compactor.Compact(blockchain, fromIndex, toIndex);
                _blockchainState.SetBlockchain(compactedBlockchain);

                await PersistBlockchainAsync(compactedBlockchain, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task PersistBlockchainAsync(IEnumerable<BlockModel> blockchain, CancellationToken cancellationToken)
        {
            var blockchainDirectory = _configurationManager.GetBlockchainDirectory();
            if (blockchainDirectory is not null)
            {
                foreach (var block in blockchain)
                {
                    await _blockchainCreation.PersistBlockAsync(blockchainDirectory, block, cancellationToken);
                }
            }
        }
    }
}
