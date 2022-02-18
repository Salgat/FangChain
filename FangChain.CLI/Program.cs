using FangChain.CLI;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using System.IO;


var services = new ServiceCollection();
services.AddTransient<IKeyCreation, KeyCreation>();
services.AddTransient<IBlockchainCreation, BlockchainCreation>();
services.AddTransient<IValidator, Validator>();
services.AddTransient<ILoader, Loader>();
var serviceProvider = services.BuildServiceProvider();

var argument = new Argument<string>("command", "Command name.");
var credentialsOption = new Option<string?>("--credentials-path", "The path of the credentials to create or read from. Defaults to current working directory.");
var blockchainPathOption = new Option<string?>("--blockchain-path", "The path of the blockchain to create or read from. Defaults to the current working directory.");
var transactionPathOption = new Option<string?>("--transaction-path", "The path of the transaction to read and write to.");
var command = new RootCommand
{
    argument, 
    credentialsOption,
    blockchainPathOption,
    transactionPathOption
};
command.Description = "FangChain CLI";

command.SetHandler(async (string argument, string? credentialsPath, string? blockchainPath, string? transactionPath, CancellationToken cancellationToken) => 
{
    try
    {
        argument = argument.ToLowerInvariant();
        var defaultDirectory = Directory.GetCurrentDirectory();
        blockchainPath ??= defaultDirectory;
        var loader = serviceProvider.GetRequiredService<ILoader>();

        if (argument == "create-keys")
        {
            Console.WriteLine($"Creating public and private keys.");
            var keyCreation = serviceProvider.GetRequiredService<IKeyCreation>();
            var keys = keyCreation.CreatePublicAndPrivateKeys();

            var destination = credentialsPath ?? Path.Join(defaultDirectory, $"keys-{DateTimeOffset.UtcNow:yyyyMMddTHHmmss}.json");
            await keyCreation.StoreKeysAsync(destination, keys, cancellationToken);
            Console.WriteLine($"Public and private keys stored in file path '{destination}'.");
        }
        else if (argument == "create-blockchain")
        {
            Console.WriteLine("Creating blockchain.");
            if (credentialsPath is null)
            {
                throw new ArgumentException($"The '{credentialsOption.Name}' option must be populated with the base58 public key.");
            }

            var blockchainCreation = serviceProvider.GetRequiredService<IBlockchainCreation>();
            var blockchainDirectory = new DirectoryInfo(blockchainPath);
            await blockchainCreation.CreateBlockChainAsync(blockchainDirectory, credentialsPath, cancellationToken);
            Console.WriteLine($"Blockchain created at location '{Path.GetFullPath(blockchainDirectory.FullName)}'.");
        }
        else if (argument == "validate-blockchain")
        {
            Console.WriteLine($"Validating blockchain at '{blockchainPath}'.");
            var blockchain = await loader.LoadBlockchainAsync(blockchainPath, cancellationToken);

            var validator = serviceProvider.GetRequiredService<IValidator>();
            if (validator.IsBlockchainValid(blockchain))
            {
                Console.WriteLine($"Blockchain is valid.");
            } 
            else
            {
                Console.WriteLine($"Blockchain is invalid.");
            }
        }
        else if (argument == "sign-transaction")
        {
            if (transactionPath is null)
            {
                throw new ArgumentException($"Transaction path must be provided.");
            }

            Console.WriteLine($"Signing transaction at '{transactionPath}'.");

            var transaction = await loader.LoadTransactionAsync(transactionPath, cancellationToken);
            var keys = await loader.LoadKeysAsync(credentialsPath, cancellationToken);
            var signature = transaction.CreateSignature(PublicAndPrivateKeys.FromBase58(keys));
            transaction.AddSignature(signature);
            await File.WriteAllTextAsync(transactionPath, JObject.FromObject(transaction).ToString(), cancellationToken);

            Console.WriteLine($"Transactioned signed by user '{signature.PublicKeyBase58}'.");
        }
    } 
    catch (Exception e)
    {
        Console.WriteLine($"Error Occurred: {e.Message}");
    }
}, argument, credentialsOption, blockchainPathOption, transactionPathOption);

return await command.InvokeAsync(args);