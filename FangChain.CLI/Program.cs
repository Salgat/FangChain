using FangChain;
using FangChain.CLI;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using System.IO;

namespace FangChain.CLI
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddFangChainDependencies();
            var serviceProvider = services.BuildServiceProvider();

            var defaultDirectory = Directory.GetCurrentDirectory();
            var loader = serviceProvider.GetRequiredService<ILoader>();

            var credentialsOption = new Option<string?>("--credentials-path", "The path of the credentials to create. Defaults to current working directory.");
            var credentialsRequiredOption = new Option<string>("--credentials-path", "The path of the credentials to use.");
            var blockchainPathOption = new Option<string?>("--blockchain-path", "The path of the blockchain to create or read from. Defaults to the current working directory.");
            var transactionPathOption = new Option<string>("--transaction-path", "The path of the transaction to read and write to.");

            #region Commands
            // Create Keys
            var createKeysCommand =
                new Command("create-keys", "Creates the public and private keys for a new user.")
                {
                    credentialsOption
                };
            createKeysCommand.SetHandler(async (string? credentialsPath, CancellationToken cancellationToken) =>
            {
                Console.WriteLine($"Creating public and private keys.");
                var keyCreation = serviceProvider.GetRequiredService<IKeyCreation>();
                credentialsPath ??= Path.Join(defaultDirectory, $"keys-{DateTimeOffset.UtcNow:yyyyMMddTHHmmss}.json");
                await CLICommands.CreateKeysAsync(keyCreation, credentialsPath, cancellationToken);
                Console.WriteLine($"Public and private keys stored in file path '{credentialsPath}'.");
            }, credentialsOption);

            // Create Blockchain
            var createBlockchainCommand =
                new Command("create-blockchain", "Creates a new blockchain where the credential provider is the initial authoritative user.")
                {
                    credentialsRequiredOption,
                    blockchainPathOption
                };
            createBlockchainCommand.SetHandler(async (string credentialsPath, string blockchainPath, CancellationToken cancellationToken) =>
            {
                Console.WriteLine("Creating blockchain.");
                if (credentialsPath is null)
                {
                    throw new ArgumentException($"The '{credentialsOption.Name}' option must be populated with the base58 public key.");
                }

                var blockchainCreation = serviceProvider.GetRequiredService<IBlockchainCreation>();
                var blockchainDirectory = await CLICommands.CreateBlockchainAsync(blockchainCreation, credentialsPath, blockchainPath, cancellationToken);
                Console.WriteLine($"Blockchain created at location '{Path.GetFullPath(blockchainDirectory.FullName)}'.");
            }, credentialsRequiredOption, blockchainPathOption);

            // Validate Blockchain
            var validateBlockchainCommand =
                new Command("validate-blockchain", "Validates the blockchain state, including the signatures for each block.")
                {
                    blockchainPathOption
                };
            validateBlockchainCommand.SetHandler(async (string blockchainPath, CancellationToken cancellationToken) =>
            {
                blockchainPath ??= defaultDirectory;

                Console.WriteLine($"Validating blockchain at '{blockchainPath}'.");
                var validator = serviceProvider.GetRequiredService<IValidator>();
                var isValid = await CLICommands.ValidateBlockchainAsync(loader, validator, blockchainPath, cancellationToken);
                if (isValid)
                {
                    Console.WriteLine($"Blockchain is valid.");
                }
                else
                {
                    Console.WriteLine($"Blockchain is invalid.");
                }
            }, blockchainPathOption);

            // Sign Transaction
            var signTransactionCommand =
                new Command("sign-transaction", "Adds a signature to the transaction.")
                {
                    credentialsOption,
                    transactionPathOption
                };
            signTransactionCommand.SetHandler(async (string credentialsPath, string transactionPath, CancellationToken cancellationToken) =>
            {
                Console.WriteLine($"Signing transaction at '{transactionPath}'.");
                var signature = await CLICommands.SignTransactionAsync(loader, credentialsPath, transactionPath, cancellationToken);
                Console.WriteLine($"Transactioned signed by user '{signature.PublicKeyBase58}'.");
            }, credentialsRequiredOption, transactionPathOption);
            #endregion

            var rootCommand = new RootCommand
            {
                createKeysCommand,
                createBlockchainCommand,
                validateBlockchainCommand,
                signTransactionCommand
            };
            rootCommand.Description = "FangChain CLI";

            return await rootCommand.InvokeAsync(args);
        }
    }
}
