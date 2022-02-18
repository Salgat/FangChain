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
    var keys = keyCreation.CreatePublicAndPrivateKeys();

    var destination = credentialsPath ?? Path.Join(defaultDirectory, $"keys-{DateTimeOffset.UtcNow:yyyyMMddTHHmmss}.json");
    await keyCreation.StoreKeysAsync(destination, keys, cancellationToken);
    Console.WriteLine($"Public and private keys stored in file path '{destination}'.");
}, credentialsOption);

// Create Blockchain
var createBlockchainCommand = 
    new Command("create-blockchain", "Creates a new blockchain where the credential provider is the initial authoritative user.")
    {
        credentialsOption,
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
    var blockchainDirectory = new DirectoryInfo(blockchainPath);
    await blockchainCreation.CreateBlockChainAsync(blockchainDirectory, credentialsPath, cancellationToken);
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
}, blockchainPathOption);

// Sign Transaction
var signTransactionCOmmand =
    new Command("sign-transaction", "Adds a signature to the transaction.")
    {
        credentialsOption,
        transactionPathOption
    };
signTransactionCOmmand.SetHandler(async (string credentialsPath, string transactionPath, CancellationToken cancellationToken) =>
{
    Console.WriteLine($"Signing transaction at '{transactionPath}'.");

    var transaction = await loader.LoadTransactionAsync(transactionPath, cancellationToken);
    var keys = await loader.LoadKeysAsync(credentialsPath, cancellationToken);
    var signature = transaction.CreateSignature(PublicAndPrivateKeys.FromBase58(keys));
    transaction.AddSignature(signature);
    await File.WriteAllTextAsync(transactionPath, JObject.FromObject(transaction).ToString(), cancellationToken);

    Console.WriteLine($"Transactioned signed by user '{signature.PublicKeyBase58}'.");
}, credentialsRequiredOption, transactionPathOption);
#endregion

var rootCommand = new RootCommand
{
    createKeysCommand,
    createBlockchainCommand,
    validateBlockchainCommand,
    signTransactionCOmmand
};
rootCommand.Description = "FangChain CLI";

return await rootCommand.InvokeAsync(args);