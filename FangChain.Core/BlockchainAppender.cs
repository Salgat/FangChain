using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class BlockchainAppender : IBlockchainAppender
    {
        private readonly IBlockchainState _blockchainState;
        private readonly IBlockchainRules _blockchainRules;
        private readonly IPendingTransactions _pendingTransactions;
        private readonly ICredentialsManager _credentialsManager;
        private readonly object _lock = new ();

        public BlockchainAppender(
            IBlockchainState blockchainState,
            IBlockchainRules blockchainRules,
            IPendingTransactions pendingTransactions,
            ICredentialsManager credentialsManager)
        {
            _blockchainState = blockchainState;
            _blockchainRules = blockchainRules;
            _pendingTransactions = pendingTransactions;
            _credentialsManager = credentialsManager;
        }

        public void ProcessPendingTransactions()
        {
            lock (_lock)
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
            }
        }
    }
}
