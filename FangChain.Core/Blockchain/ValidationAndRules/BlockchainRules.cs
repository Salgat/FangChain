using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        public bool IsBlockAdditionValid(BlockModel proposedBlock)
        {
            var updatedUserBalances = new Dictionary<string, BigInteger>(); // Used to track if users will reach below minimum required balance
            foreach (var transaction in proposedBlock.Transactions)
            {
                foreach (var signature in transaction.Signatures)
                {
                    if (_blockchainState.UserSummaries.TryGetValue(signature.PublicKeyBase58, out var userSummary) &&
                        userSummary.Disabled)
                    {
                        // A disabled user cannot sign transactions
                        return false;
                    }
                }

                if (transaction.TransactionType is TransactionType.PromoteUser ||
                    transaction.TransactionType is TransactionType.AddToUserBalance)
                {
                    // Ensure quorum
                    var hasQuorum = HasQuorum(transaction, UserDesignation.SuperAdministrator);
                    if (!hasQuorum) return false;
                }
                else if (transaction.TransactionType is TransactionType.TransferToUserBalance)
                {
                    var transferToUserBalanceTransaction = (TransferToUserBalanceTransaction)transaction;

                    // Ensure sending user signed transaction
                    if (!transaction.Signatures.Any(s => s.PublicKeyBase58 == transferToUserBalanceTransaction.FromPublicKeyBase58))
                    {
                        return false;
                    }

                    // Ensure user has sufficient balance to send
                    if (!updatedUserBalances.TryGetValue(transferToUserBalanceTransaction.FromPublicKeyBase58, out var userBalance))
                    {
                        if (_blockchainState.UserSummaries.TryGetValue(transferToUserBalanceTransaction.FromPublicKeyBase58, out var userSummary))
                        {
                            userBalance = userSummary.Balance;
                        }
                        else
                        {
                            userBalance = 0;
                        }
                    }
                    var updatedUserbalance = userBalance - transferToUserBalanceTransaction.Amount;
                    if (updatedUserbalance < 0) return false;
                    updatedUserBalances[transferToUserBalanceTransaction.FromPublicKeyBase58] = updatedUserbalance;
                }
                else if (transaction.TransactionType is TransactionType.DisableUser)
                {
                    var disableUserTransaction = (DisableUserTransaction)transaction;
                    if (_blockchainState.UserSummaries.TryGetValue(disableUserTransaction.PublicKeyBase58, out var userSummary) &&
                        userSummary.Disabled == true)
                    {
                        // Avoid redundant disable
                        return false;
                    }
                }
                else if (transaction.TransactionType is TransactionType.EnableUser)
                {
                    var enableUserTransaction = (EnableUserTransaction)transaction;
                    if (_blockchainState.UserSummaries.TryGetValue(enableUserTransaction.PublicKeyBase58, out var userSummary) &&
                        userSummary.Disabled == false)
                    {
                        // Avoid redundant enable
                        return false;
                    }
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
