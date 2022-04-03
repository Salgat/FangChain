using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    /// <summary>
    /// Determines whether a block follows the rules of the blockchain. This is different from a blockchain validator, 
    /// since a validator only asserts the technical aspects of a blockchain, while a rule is arbitrary and often changing.
    /// This is mainly used to determine whether a proposed block should be accepted into the blockchain. An invalid
    /// block must always be rejected and if present in a blockchain, means the entire blockchain is invalid. A rule breaking 
    /// block just means the nodes should reject that block proposal, but if it gets added to the blockchain, does not mean the
    /// blockchain is invalid. Rules are almost exclusively used for block proposals.
    /// 
    /// Validation Examples: 
    ///     - All signatures are correct for a given block. 
    ///     - The hash for the previous block is valid.
    /// Rule Examples: 
    ///     - All transactions are for approved users. 
    ///     - A user does not exceed a certain negative balance they are allowed to have.
    /// </summary>
    public interface IBlockchainRules
    {
        bool IsBlockAdditionValid(IEnumerable<BlockModel> blockchain, BlockModel proposedBlock);
    }
}
