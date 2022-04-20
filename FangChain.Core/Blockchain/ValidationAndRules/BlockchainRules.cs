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
            var updatedTokenOwners = new Dictionary<string, string>(); // TokenId => Owner's PublicKeyBase58
            var removedTokens = new HashSet<string>(); // TokenId

            bool ValidateTransaction(TransactionModel transaction)
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

                if (transaction is DesignateUserTransaction)
                {
                    // Ensure quorum
                    if (!HasQuorum(transaction, UserDesignation.SuperAdministrator)) return false;
                }
                else if (transaction is AddToUserBalanceTransaction addToUserBalanceTransaction)
                {
                    var userBalance = GetUserBalance(addToUserBalanceTransaction.PublicKeyBase58, updatedUserBalances);
                    updatedUserBalances[addToUserBalanceTransaction.PublicKeyBase58] = userBalance + addToUserBalanceTransaction.Amount;

                    // Ensure quorum
                    if (!HasQuorum(transaction, UserDesignation.SuperAdministrator)) return false;
                }
                else if (transaction is TransferToUserBalanceTransaction transferToUserBalanceTransaction)
                {
                    // Ensure sending user signed transaction
                    if (!transaction.Signatures.Any(s => s.PublicKeyBase58 == transferToUserBalanceTransaction.FromPublicKeyBase58))
                    {
                        return false;
                    }

                    // Ensure user has sufficient balance to send
                    var fromUserBalance = GetUserBalance(transferToUserBalanceTransaction.FromPublicKeyBase58, updatedUserBalances);
                    var updatedUserbalance = fromUserBalance - transferToUserBalanceTransaction.Amount;
                    if (updatedUserbalance < 0) return false;
                    updatedUserBalances[transferToUserBalanceTransaction.FromPublicKeyBase58] = updatedUserbalance;

                    var toUserBalance = GetUserBalance(transferToUserBalanceTransaction.ToPublicKeyBase58, updatedUserBalances);
                    updatedUserBalances[transferToUserBalanceTransaction.ToPublicKeyBase58] = toUserBalance + transferToUserBalanceTransaction.Amount;
                }
                else if (transaction is DisableUserTransaction disableUserTransaction)
                {
                    if (_blockchainState.UserSummaries.TryGetValue(disableUserTransaction.PublicKeyBase58, out var userSummary) &&
                        userSummary.Disabled == true)
                    {
                        // Avoid redundant disable
                        return false;
                    }

                    // Ensure quorum
                    if (!HasQuorum(transaction, UserDesignation.SuperAdministrator)) return false;
                }
                else if (transaction is EnableUserTransaction enableUserTransaction)
                {
                    if (_blockchainState.UserSummaries.TryGetValue(enableUserTransaction.PublicKeyBase58, out var userSummary) &&
                        userSummary.Disabled == false)
                    {
                        // Avoid redundant enable
                        return false;
                    }

                    // Ensure quorum
                    if (!HasQuorum(transaction, UserDesignation.SuperAdministrator)) return false;
                }
                else if (transaction is AddTokenTransaction addTokenTransaction)
                {
                    // Prevent duplicate tokens
                    if (_blockchainState.TokenOwners.ContainsKey(addTokenTransaction.TokenId) ||
                        !updatedTokenOwners.TryAdd(addTokenTransaction.TokenId, addTokenTransaction.PublicKeyBase58)) return false;
                    removedTokens.Remove(addTokenTransaction.TokenId);

                    // Ensure quorum
                    if (!HasQuorum(transaction, UserDesignation.SuperAdministrator)) return false;
                }
                else if (transaction is RemoveTokenTransaction removeTokenTransaction)
                {
                    if (removedTokens.Contains(removeTokenTransaction.TokenId))
                    {
                        // Token was already removed and not added back
                        return false;
                    }

                    if (!updatedTokenOwners.TryGetValue(removeTokenTransaction.TokenId, out var tokenOwner))
                    {
                        if (!_blockchainState.TokenOwners.TryGetValue(removeTokenTransaction.TokenId, out tokenOwner))
                        {
                            // No one owns this token
                            return false;
                        }
                    }
                    else
                    {
                        updatedTokenOwners.Remove(removeTokenTransaction.TokenId);
                    }
                    removedTokens.Add(removeTokenTransaction.TokenId);
                }
                else if (transaction is TransferTokenTransaction transferTokenTransaction)
                {
                    if (removedTokens.Contains(transferTokenTransaction.TokenId))
                    {
                        // Token was already removed and not added back
                        return false;
                    }

                    if (!updatedTokenOwners.TryGetValue(transferTokenTransaction.TokenId, out var tokenOwner))
                    {
                        if (!_blockchainState.TokenOwners.TryGetValue(transferTokenTransaction.TokenId, out tokenOwner))
                        {
                            // No one owns this token
                            return false;
                        }
                    }

                    // Only token owner can transfer token
                    if (tokenOwner != transferTokenTransaction.FromPublicKeyBase58) return false;

                    updatedTokenOwners[transferTokenTransaction.TokenId] = transferTokenTransaction.ToPublicKeyBase58;
                }
                return true;
            }

            foreach (var transaction in proposedBlock.Transactions)
            {
                if (transaction is LumpedTransaction lumpedTransaction)
                {
                    var signatures = lumpedTransaction.Signatures;
                    foreach (var entry in lumpedTransaction.Transactions)
                    {
                        entry.SetSignatures(signatures); // Override signatures
                        if (!ValidateTransaction(entry)) return false;
                    }
                }
                else
                {
                    if (!ValidateTransaction(transaction)) return false;
                }
            }
            return true;
        }

        private BigInteger GetUserBalance(string publicKeyBase58, Dictionary<string, BigInteger> updatedUserBalances)
        {
            if (!updatedUserBalances.TryGetValue(publicKeyBase58, out var userBalance))
            {
                if (_blockchainState.UserSummaries.TryGetValue(publicKeyBase58, out var userSummary))
                {
                    userBalance = userSummary.Balance;
                }
                else
                {
                    userBalance = 0;
                }
            }
            return userBalance;
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
