using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    public static class CLICommands
    {
        public static async Task CreateKeysAsync(
            IKeyCreation keyCreation, 
            string credentialsPath, 
            CancellationToken cancellationToken)
        {
            var keys = keyCreation.CreatePublicAndPrivateKeys();
            await keyCreation.StoreKeysAsync(credentialsPath, keys, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockchainCreation"></param>
        /// <param name="credentialsPath"></param>
        /// <param name="blockchainPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The directory information of the blockchain's location.</returns>
        public static async Task<DirectoryInfo> CreateBlockchainAsync(
            IBlockchainCreation blockchainCreation,
            string credentialsPath, 
            string blockchainPath, 
            CancellationToken cancellationToken)
        {
            var blockchainDirectory = new DirectoryInfo(blockchainPath);
            await blockchainCreation.CreateBlockChainAsync(blockchainDirectory, credentialsPath, cancellationToken);
            return blockchainDirectory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="validator"></param>
        /// <param name="blockchainPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns whether the blockchain is valid.</returns>
        public static async Task<bool> ValidateBlockchainAsync(
            ILoader loader,
            IValidator validator,
            string blockchainPath, 
            CancellationToken cancellationToken)
        {
            var blockchain = await loader.LoadBlockchainAsync(blockchainPath, cancellationToken);
            return validator.IsBlockchainValid(blockchain);
        }

        public static async Task<SignatureModel> SignTransactionAsync(
            ILoader loader,
            string credentialsPath, 
            string transactionPath, 
            CancellationToken cancellationToken)
        {
            var transaction = await loader.LoadTransactionAsync(transactionPath, cancellationToken);
            var keys = await loader.LoadKeysAsync(credentialsPath, cancellationToken);
            var signature = transaction.CreateSignature(PublicAndPrivateKeys.FromBase58(keys));
            transaction.AddSignature(signature);
            await File.WriteAllTextAsync(transactionPath, JObject.FromObject(transaction).ToString(), cancellationToken);

            return signature;
        }
    }
}
