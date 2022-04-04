using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace FangChain.Server.Controllers
{
    [ApiController]
    [Route("transaction")]
    public class TransactionController : ControllerBase
    {
        private readonly IPendingTransactions _pendingTransactions;
        private readonly IBlockchainState _blockchainState;

        public TransactionController(IPendingTransactions pendingTransactions, IBlockchainState blockchainState)
        {
            _pendingTransactions = pendingTransactions;
            _blockchainState = blockchainState;
        }

        [HttpPost]
        public void ProposeTransaction([FromBody] PendingTransaction transaction)
        {
            if (transaction == null) throw new Exception($"Transaction must be defined in post body.");

            transaction.DateTimeRecieved = DateTime.UtcNow;
            if (!_pendingTransactions.TryAdd(transaction)) throw new Exception($"Failed to accept proposed transaction.");
        }

        [HttpGet]
        [Route("confirmed")]
        public bool IsTransactionConfirmed([FromQuery] string transactionHash)
            => _blockchainState.IsTransactionConfirmed(transactionHash);
    }
}
