using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FangChain
{
    public class BlockchainCreation : IBlockchainCreation
    {
        private readonly ILoader _loader;

        public BlockchainCreation(ILoader loader)
        {
            _loader = loader;
        }

        public async Task CreateBlockChainAsync(DirectoryInfo blockchainDirectory, string credentialsPath, CancellationToken cancellationToken = default)
        {
            var keysBase58 = await _loader.LoadKeysAsync(credentialsPath, cancellationToken);
            var keys = PublicAndPrivateKeys.FromBase58(keysBase58);
            var initialBlock = BlockModel.CreateInitialBlock(keys);
            await PersistBlockAsync(blockchainDirectory, initialBlock, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockchainDirectory"></param>
        /// <param name="block"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns false if the block already exists.</returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> PersistBlockAsync(DirectoryInfo blockchainDirectory, BlockModel block, CancellationToken cancellationToken = default)
        {
            var destinationDirectory = Path.GetFullPath(blockchainDirectory.FullName);
            var destination = Path.Join(destinationDirectory, GenerateBlockName(block));
            if (File.Exists(destination)) return false;

            var blockJson = JsonConvert.SerializeObject(block, Formatting.Indented);
            await File.WriteAllTextAsync(destination, blockJson, cancellationToken);
            return true;
        }

        public async Task<BlockModel> ReadBlockAsync(string blockPath, CancellationToken cancellationToken = default)
        {
            var blockJson = await File.ReadAllTextAsync(blockPath, cancellationToken);
            var blockJsonParsed = JObject.Parse(blockJson);
            BlockModel? block;
            if (blockJsonParsed?[nameof(BlockModel.IsCompacted)]?.Value<bool>() == true)
            {
                block = blockJsonParsed?.ToObject<CompactedBlockModel>();
            } 
            else
            {
                block = blockJsonParsed?.ToObject<BlockModel>();
            }

            if (block is null) throw new Exception($"Failed to deserialize block '{blockPath}'.");
            return block;
        }

        private static string GenerateBlockName(BlockModel block)
        {
            if (block is CompactedBlockModel compactedBlockModel)
            {
                return $"{compactedBlockModel.BlockIndex}-{compactedBlockModel.NextBlockIndex}.compacted.json";
            }
            else
            {
                return $"{block.BlockIndex}.block.json";
            }
        }
    }
}
