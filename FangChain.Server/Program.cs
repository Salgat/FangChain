using FangChain;
using FangChain.Server;
using System.CommandLine;

#region http server setup
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

    await application.RunAsync(cancellationToken);
}, blockchainPathOption, credentialsRequiredOption);

var rootCommand = new RootCommand
{
    hostCommand
};
rootCommand.Description = "FangChain Server";
#endregion

return await rootCommand.InvokeAsync(args);
