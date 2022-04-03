using FangChain;
using FangChain.Server;
using System.CommandLine;
using System.Linq;

#region http server setup
var commandOverride = args.FirstOrDefault(argument => argument.StartsWith("--commandOverride="));
if (commandOverride != null)
{
    args = commandOverride
        .Split("=")[1]
        .Split(" ")
        .Where(argument => !string.IsNullOrWhiteSpace(argument))
        .Select(argument => argument.Trim())
        .ToArray();
}
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFangChainDependencies();
builder.Services.AddFangChainServerDependencies(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddMvc(options =>
    {
        options.EnableEndpointRouting = false;
    })
    .AddApplicationPart(typeof(Program).Assembly)
    .AddNewtonsoftJson()
    .AddControllersAsServices();

var application = builder.Build();
application.UseSwagger();
application.UseMvc();
application.UseSwaggerUI();
var serviceProvider = application.Services;
#endregion

var defaultDirectory = Directory.GetCurrentDirectory();

#region CLI setup
var blockchainPathOption = new Option<string?>("--blockchain-path", "The path of the blockchain to use. Defaults to the current working directory.");
var credentialsRequiredOption = new Option<string>("--credentials-path", "The path of the credentials to use.");

var hostCommand = new Command("host", "Hosts the given blockchain.")
{
    blockchainPathOption,
    credentialsRequiredOption
};
hostCommand.SetHandler(async (string? blockchainPath, string credentialsPath, CancellationToken cancellationToken) =>
{
    Console.WriteLine($"Starting server.");

    // Load and validate blockchain
    blockchainPath ??= defaultDirectory;
    var loader = serviceProvider.GetRequiredService<ILoader>();
    var blockchain = await loader.LoadBlockchainAsync(blockchainPath, cancellationToken);

    var validator = serviceProvider.GetRequiredService<IValidator>();
    if (!validator.IsBlockchainValid(blockchain))
    {
        throw new Exception($"Blockchain provided is invalid.");
    }

    // Set global state to validated blockchain
    var blockchainState = serviceProvider.GetRequiredService<IBlockchainState>();
    blockchainState.SetBlockchain(blockchain);

    #region background jobs
    // On a configured interval, process proposed jobs in a background thread
    // NOTE: This is the exclusive mutator of blockchain state, no other function does this.
    var pendingOperations = serviceProvider.GetRequiredService<IPendingTransactions>();
    var blockchainRules = serviceProvider.GetRequiredService<IBlockchainRules>();
    var _ = Task.Run(async () =>
    {
        while (true)
        {
            await Task.Delay(2500);
            try
            {
                var currentBlockchain = blockchainState.GetBlockchain();
                if (!currentBlockchain.Any()) continue;

                var nextBlockIndex = currentBlockchain.Last().BlockIndex + 1;
                var previousBlockHash = currentBlockchain.Last().GetHashString();

                var allowedTransactions = new List<TransactionModel>();
                BlockModel? proposedBlock = default;
                pendingOperations.PurgeExpiredTransactions(DateTimeOffset.UtcNow, nextBlockIndex);
                foreach (var proposedTransaction in pendingOperations.PendingTransactions)
                {
                    proposedBlock = new BlockModel(nextBlockIndex, previousBlockHash, allowedTransactions.Concat(new[] { proposedTransaction.Transaction }));
                    if (blockchainRules.IsBlockAdditionValid(currentBlockchain, proposedBlock))
                    {
                        allowedTransactions.Add(proposedTransaction.Transaction);
                    }
                }

                // Add proposed transactions as a new block
                if (proposedBlock == default) continue;
                var proposedBlockchain = currentBlockchain.Add(proposedBlock);
                blockchainState.SetBlockchain(proposedBlockchain);

                // Flush pending transactions (as they are either included in the block or rejected)
                foreach (var proposedTransaction in pendingOperations.PendingTransactions)
                {
                    pendingOperations.TryRemove(proposedTransaction);
                }
            }
            catch (Exception ex)
            {
                // TODO: Use a proper logger
                Console.WriteLine($"ERROR - Failed to process pending transactions");
            }
        }
    });
    #endregion

    await application.RunAsync(cancellationToken);
}, blockchainPathOption, credentialsRequiredOption);

var rootCommand = new RootCommand
{
    hostCommand
};
rootCommand.Description = "FangChain Server";
#endregion

return await rootCommand.InvokeAsync(args);

public partial class Program 
{
}