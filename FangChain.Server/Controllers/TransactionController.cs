using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace FangChain.Server
{
    [ApiController]
    [Route("transaction")]
    public class TransactionController : ControllerBase
    {
        private readonly IPendingTransactions _pendingTransactions;
        private readonly IBlockchainState _blockchainState;
        private readonly IBlockchainMutator _blockchainAppender;

        public TransactionController(
            IPendingTransactions pendingTransactions, 
            IBlockchainState blockchainState,
            IBlockchainMutator blockchainAppender)
        {
            _pendingTransactions = pendingTransactions;
            _blockchainState = blockchainState;
            _blockchainAppender = blockchainAppender;
        }

        [HttpPost]
        public void ProposeTransaction([FromBody] PendingTransaction transaction)
        {
            if (transaction == null) throw new Exception($"Transaction must be defined in post body.");

            transaction.DateTimeRecieved = DateTime.UtcNow;
            if (!_pendingTransactions.TryAdd(transaction)) throw new Exception($"Failed to accept proposed transaction.");
        }

        /// <summary>
        /// Queries whether transaction has been processed (and either accepted or rejected).
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <returns>Returns whether transaction has been processed. Does not indicate whether it was accepted into the blockchain.</returns>
        [HttpGet]
        [Route("confirmed")]
        public bool IsTransactionConfirmed([FromQuery] string transactionHash)
            => _blockchainState.IsTransactionConfirmed(transactionHash);

        [HttpPost]
        [Route("process")]
        public void ProcessPendingTransactions()
            => _blockchainAppender.ProcessPendingTransactions();
    }
}
