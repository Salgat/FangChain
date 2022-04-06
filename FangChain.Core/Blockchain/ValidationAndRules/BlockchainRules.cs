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
            foreach (var transaction in proposedBlock.Transactions)
            {
                if (transaction.TransactionType is TransactionType.PromoteUser ||
                    transaction.TransactionType is TransactionType.AddToUserBalance)
                {
                    // Ensure quorum
                    var hasQuorum = HasQuorum(transaction, UserDesignation.SuperAdministrator);
                    if (!hasQuorum) return false;
                }
            }
            return true;
        }

        private bool HasQuorum(TransactionModel transaction, params UserDesignation[] designations)
        {
            var quorumMembers = _blockchainState.UserSummaries.Where(u => designations.Contains(u.Value.Designation)).Select(u => u.Key).ToHashSet();
            var quorumMembersSignatureCount = transaction.Signatures.Where(s => quorumMembers.Contains(s.PublicKeyBase58)).Count();
            var quorumRequirement = quorumMembers.Count / 2 + 1;
            return quorumMembersSignatureCount >= quorumRequirement;
        }
    }
}
