using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    public class Loader : ILoader
    {
        private static readonly ImmutableDictionary<string, Type> TransactionTypes = typeof(TransactionModel)
            .Assembly
            .GetTypes()
            .Where(type => typeof(TransactionModel).IsAssignableFrom(type))
            .ToImmutableDictionary(type => type.Name, type => type);

        public Loader() { }

        public async Task<ImmutableArray<BlockModel>> LoadBlockchainAsync(string directory)
        {
            var files = Directory.GetFiles(directory).OrderBy(fileName => fileName);
            var blocks = new List<BlockModel>();
            foreach (var file in files)
            {
                if (!file.EndsWith(".block.json")) continue;

                var blockJson = await File.ReadAllTextAsync(file);
                var block = DeserializeBlock(JObject.Parse(blockJson));
                blocks.Add(block);
            }

            return blocks.OrderBy(block => block.BlockIndex).ToImmutableArray();
        }

        private static BlockModel DeserializeBlock(JObject blockJson)
        {
            var index = blockJson[nameof(BlockModel.BlockIndex)].ToObject<long>();
            var previousBlockHashBase58 = blockJson[nameof(BlockModel.PreviousBlockHashBase58)].ToString();

            var transactionsJArray = blockJson[nameof(BlockModel.Transactions)];
            var transactions = new List<TransactionModel>();
            foreach (var transactionJObject in transactionsJArray)
            {
                var transactionEnum = (TransactionType)transactionJObject[nameof(TransactionModel.TransactionType)].ToObject<int>();
                var transactionTypeName = $"{transactionEnum}Transaction";
                var transactionType = TransactionTypes[transactionTypeName];
                var transaction = transactionJObject.ToObject(transactionType);
                transactions.Add((TransactionModel)transaction);
            }

            var signaturesJArray = blockJson[nameof(BlockModel.Signatures)];
            var signatures = signaturesJArray.ToObject<IEnumerable<SignatureModel>>();

            var block = new BlockModel(index, previousBlockHashBase58, transactions);
            block.SetSignatures(signatures);

            return block;
        }
    }
}
