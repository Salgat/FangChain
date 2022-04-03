using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class BlockchainRules : IBlockchainRules
    {
        private readonly IBlockchainState _blockchainState;

        public BlockchainRules(IBlockchainState blockchainState)
        {
            _blockchainState = blockchainState;
        }

        public bool IsBlockAdditionValid(IEnumerable<BlockModel> blockchain, BlockModel proposedBlock)
        {
            // TODO: Check for quorum if promote user transaction
            var superAdministrators = _blockchainState.UserSummaries.Where(u => u.Value.Designation is UserDesignation.SuperAdministrator).Select(u => u.Key).ToHashSet();
            var promotionRequiredQuorumCount = superAdministrators.Count / 2 + 1;
            var proposedUserStatusChanges = new Dictionary<string, UserDesignation>();
            foreach (var transaction in proposedBlock.Transactions)
            {
                if (transaction.TransactionType is TransactionType.PromoteUser)
                {
                    // Ensure quorum
                    var superAdministratorSignatureCount = transaction.Signatures.Where(s => superAdministrators.Contains(s.PublicKeyBase58)).Count();
                    if (superAdministratorSignatureCount < promotionRequiredQuorumCount) return false;

                    var promotionTransaction = transaction as PromoteUserTransaction;
                    proposedUserStatusChanges[promotionTransaction.PublicKeyBase58] = promotionTransaction.UserDesignation;
                }
            }
            return true;
        }
    }
}
