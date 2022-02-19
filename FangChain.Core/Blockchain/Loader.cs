using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class Loader : ILoader
    {
        private static readonly ImmutableDictionary<string, Type> TransactionTypes = typeof(TransactionModel)
            .Assembly
            .GetTypes()
            .Where(type => typeof(TransactionModel).IsAssignableFrom(type))
            .ToImmutableDictionary(type => type.Name, type => type);

        public Loader() { }

        public async Task<ImmutableArray<BlockModel>> LoadBlockchainAsync(string directory, CancellationToken cancellationToken = default)
        {
            var files = Directory.GetFiles(directory).OrderBy(fileName => fileName);
            var blocks = new List<BlockModel>();
            foreach (var file in files)
            {
                if (!file.EndsWith(".block.json")) continue;

                var block = await LoadBlockAsync(file, cancellationToken);
                blocks.Add(block);
            }

            return blocks.OrderBy(block => block.BlockIndex).ToImmutableArray();
        }

        public async Task<BlockModel> LoadBlockAsync(string blockPath, CancellationToken cancellationToken = default)
        {
            var blockJson = await File.ReadAllTextAsync(blockPath, cancellationToken);
            return DeserializeBlock(JObject.Parse(blockJson));
        }

        public async Task<TransactionModel> LoadTransactionAsync(string transactionPath, CancellationToken cancellationToken = default)
        {
            var transactionJson = await File.ReadAllTextAsync(transactionPath, cancellationToken);
            var transactionJObject = JObject.Parse(transactionJson);
            return DeserializeTransaction(transactionJObject);
        }

        public async Task<Base58PublicAndPrivateKeys> LoadKeysAsync(string keysPath, CancellationToken cancellationToken = default)
        {
            var keysJson = await File.ReadAllTextAsync(keysPath, cancellationToken);
            var keysJObject = JObject.Parse(keysJson);
            return keysJObject.ToObject<Base58PublicAndPrivateKeys>();
        }

        private static TransactionModel DeserializeTransaction(JObject transactionJObject)
        {
            var transactionEnum = (TransactionType)transactionJObject[nameof(TransactionModel.TransactionType)].ToObject<int>();
            var transactionTypeName = $"{transactionEnum}Transaction";
            var transactionType = TransactionTypes[transactionTypeName];
            var transaction = transactionJObject.ToObject(transactionType);
            return (TransactionModel)transaction;
        }

        private static BlockModel DeserializeBlock(JObject blockJson)
        {
            var index = blockJson[nameof(BlockModel.BlockIndex)].ToObject<long>();
            var previousBlockHashBase58 = blockJson[nameof(BlockModel.PreviousBlockHashBase58)].ToString();

            var transactionsJArray = blockJson[nameof(BlockModel.Transactions)];
            var transactions = new List<TransactionModel>();
            foreach (var transactionJObject in transactionsJArray)
            {
                var transaction = DeserializeTransaction((JObject)transactionJObject);
                transactions.Add(transaction);
            }

            var signaturesJArray = blockJson[nameof(BlockModel.Signatures)];
            var signatures = signaturesJArray.ToObject<IEnumerable<SignatureModel>>();

            var block = new BlockModel(index, previousBlockHashBase58, transactions);
            block.SetSignatures(signatures);

            return block;
        }
    }
}
