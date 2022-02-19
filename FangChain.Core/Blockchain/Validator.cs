using NokitaKaze.Base58Check;
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class Validator : IValidator
    {
        public bool IsBlockchainValid(ImmutableArray<BlockModel> blocks)
        {
            var expectedPreviousHashBase58 = string.Empty;
            long previousBlockIndex = -1;
            foreach (var block in blocks)
            {
                if (expectedPreviousHashBase58 != block.PreviousBlockHashBase58) return false;
                if (!IsBlockValid(block)) return false;
                if (block.BlockIndex <= previousBlockIndex) return false;

                expectedPreviousHashBase58 = Base58CheckEncoding.Encode(block.GetHash());
                previousBlockIndex = block.BlockIndex;
            }

            return true;
        }

        public bool IsBlockValid(BlockModel block)
        {
            if (block == null) throw new ArgumentException($"{nameof(BlockModel)} cannot have a null value.");

            // Validate transactions for block
            if (block.Transactions == null) throw new ArgumentException($"{nameof(BlockModel)}.{nameof(BlockModel.Transactions)} cannot have a null value.");
            foreach (var transaction in block.Transactions)
            {
                if (!IsTransactionValid(transaction)) return false;
            }

            // Validate signature for block
            using var secp256k1 = new Secp256k1();
            var blockHash = block.GetHash();
            foreach (var signature in block.Signatures)
            {
                if (!secp256k1.Verify(signature.GetSignature(), blockHash, signature.GetPublicKey())) return false;
            }

            return true;
        }

        public bool IsTransactionValid(TransactionModel transaction)
        {
            if (transaction == null) throw new ArgumentException($"{nameof(TransactionModel)} cannot have a null value.");

            using var secp256k1 = new Secp256k1();
            var transactionHash = transaction.GetHash();
            foreach (var signature in transaction.Signatures)
            {
                if (!secp256k1.Verify(signature.GetSignature(), transactionHash, signature.GetPublicKey())) return false;
            }

            return true;
        }
    }
}
