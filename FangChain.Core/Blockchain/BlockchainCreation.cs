using Newtonsoft.Json;
using NokitaKaze.Base58Check;
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task PersistBlockAsync(DirectoryInfo blockchainDirectory, BlockModel block, CancellationToken cancellationToken = default)
        {
            var destinationDirectory = Path.GetFullPath(blockchainDirectory.FullName);
            var destination = Path.Join(destinationDirectory, GenerateBlockName(block));
            if (File.Exists(destination)) throw new Exception($"Attempting to write block to destination '{destination}' but a block already exists.");

            var blockJson = JsonConvert.SerializeObject(block, Formatting.Indented);
            await File.WriteAllTextAsync(destination, blockJson, cancellationToken);
        }

        public async Task<BlockModel> ReadBlockAsync(string blockPath, CancellationToken cancellationToken = default)
        {
            var blockJson = await File.ReadAllTextAsync(blockPath, cancellationToken);
            var block = System.Text.Json.JsonSerializer.Deserialize<BlockModel>(blockJson);
            if (block is null) throw new Exception($"Failed to deserialize block '{blockPath}'.");
            return block;
        }

        private static string GenerateBlockName(BlockModel block) => $"{block.BlockIndex}.block.json";
    }
}
