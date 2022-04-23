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

        public async Task<ImmutableArray<BlockModel>> LoadBlockchainAsync(string directory, CancellationToken cancellationToken = default)
        {
            var files = GetBlockFilesToLoad(directory);
            var blocks = new List<BlockModel>();
            long currentIndex = 0;
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var (startIndex, endIndex) = GetBlockFileNameIndexRange(fileName);
                if (startIndex < currentIndex)
                {
                    continue;
                }
                else if (startIndex > currentIndex)
                {
                    throw new FileNotFoundException($"Blockchain missing block with expected index of {currentIndex}.");
                }

                var block = await LoadBlockAsync(file, cancellationToken);
                blocks.Add(block);

                currentIndex = (endIndex ?? startIndex) + 1;
            }

            return blocks.OrderBy(block => block.BlockIndex).ToImmutableArray();
        }

        private static List<string> GetBlockFilesToLoad(string directory)
        {
            var files = Directory
                .GetFiles(directory)
                .Where(file => file.EndsWith(".block.json"))
                .OrderBy(file =>
                {
                    // Order by starting index of each block
                    var fileName = Path.GetFileName(file);
                    return GetBlockFileNameIndexRange(fileName).StartIndex;
                })
                .ThenByDescending(file =>
                {
                    // Then order by the index with the largest range
                    var fileName = Path.GetFileName(file);
                    var (startIndex, endIndex) = GetBlockFileNameIndexRange(fileName);
                    return endIndex ?? startIndex;
                })
                .ToList();

            var filteredFiles = new List<string>();
            var indexesAlreadyHandled = new HashSet<long>();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var (startIndex, endIndex) = GetBlockFileNameIndexRange(fileName);
                if (indexesAlreadyHandled.Contains(startIndex)) continue;

                filteredFiles.Add(file);
                indexesAlreadyHandled.Add(startIndex);
            }

            return filteredFiles;
        }

        private static (long StartIndex, long? EndIndex) GetBlockFileNameIndexRange(string fileName)
        {
            var fileNameSplit = fileName.Split('.'); 
            if (fileNameSplit.Contains("compacted"))
            {
                var range = fileNameSplit[0].Split('-');
                var start = long.Parse(range[0]);
                var end = long.Parse(range[1]);
                return (start, end);
            }
            else
            {
                var start = long.Parse(fileNameSplit[0]);
                return (start, null);
            }
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

        public static TransactionModel DeserializeTransaction(JObject transactionJObject)
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

            BlockModel block;
            if (blockJson.Value<bool>(nameof(BlockModel.IsCompacted)))
            {
                var nextIndex = blockJson.Value<int>(nameof(CompactedBlockModel.NextBlockIndex));
                var nextBlockHashBase58 = blockJson.Value<string>(nameof(CompactedBlockModel.NextBlockHashBase58));
                var compactedBlockHashes = blockJson[nameof(CompactedBlockModel.CompactedBlockHashes)].ToObject<IEnumerable<string>>().ToImmutableArray();
                block = new CompactedBlockModel(index, nextIndex, previousBlockHashBase58, nextBlockHashBase58, transactions, compactedBlockHashes);
            } 
            else
            {
                block = new BlockModel(index, previousBlockHashBase58, transactions);
            }
            block.SetSignatures(signatures);

            return block;
        }
    }
}
