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
    .AddNewtonsoftJson(config => config.UseMemberCasing())
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
var manualBlockCreationOption = new Option<bool?>("--manual-blockcreationtrigger", "Requires an action to trigger the creation of a new block.");

var hostCommand = new Command("host", "Hosts the given blockchain.")
{
    blockchainPathOption,
    credentialsRequiredOption,
    manualBlockCreationOption
};
hostCommand.SetHandler(async (string? blockchainPath, string credentialsPath, bool? manualBlockCreation, CancellationToken cancellationToken) =>
{
    Console.WriteLine($"Starting server.");

    // Load and validate blockchain
    blockchainPath ??= defaultDirectory;
    var loader = serviceProvider.GetRequiredService<ILoader>();
    var hostCredentials = await loader.LoadKeysAsync(credentialsPath, cancellationToken);
    serviceProvider.GetRequiredService<ICredentialsManager>().SetHostCredentials(hostCredentials);
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
    if (manualBlockCreation != true)
    {
        var blockchainAppender = serviceProvider.GetRequiredService<IBlockchainMutator>();
        var _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000);
                try
                {
                    blockchainAppender.ProcessPendingTransactions();
                }
                catch (Exception ex)
                {
                    // TODO: Use a proper logger
                    Console.WriteLine($"ERROR - Failed to process pending transactions");
                }
            }
        }, cancellationToken);
    }
    #endregion

    await application.RunAsync(cancellationToken);
}, blockchainPathOption, credentialsRequiredOption, manualBlockCreationOption);

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