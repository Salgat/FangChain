using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FangChain.Test
{
    public class BlockchainTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _testDirectory;
        private readonly string _credentialsPath;
        private static readonly object _lock = new();
        private static bool _firstRun = true;

        public BlockchainTests(WebApplicationFactory<Program> factory)
        {
            // Create test folder
            var currentDirectory = Directory.GetCurrentDirectory();
            var testsFolderName = $"{nameof(FangChain)}-{nameof(FangChain.Test)}";
            var currentTestFolderName = $"{nameof(BlockchainTests)}-{Guid.NewGuid():N}";
            
            var testsFolder = Path.Combine(currentDirectory, testsFolderName);
            lock (_lock)
            {
                if (_firstRun)
                {
                    // Cleanup tests from previous run
                    if (Directory.Exists(testsFolder)) Directory.Delete(testsFolder, true);
                    _firstRun = false;
                }

                if (!Directory.Exists(testsFolder)) Directory.CreateDirectory(testsFolder);
            }
            var testDirectory = Path.Combine(currentDirectory, testsFolderName, currentTestFolderName);
            if (!Directory.Exists(testDirectory)) Directory.CreateDirectory(testDirectory);
            _testDirectory = testDirectory;
            _credentialsPath = Path.Combine(_testDirectory, $"keys-{Guid.NewGuid():N}.json");

            // Configure test server
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var hostArgs = new[]
            {
                "host",
                "--credentials-path",
                _credentialsPath,
                "--blockchain-path",
                _testDirectory
            };
            configuration["commandOverride"] = String.Join(" ", hostArgs);
            factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseConfiguration(configuration);
            });
            _factory = factory;
        }

        private async Task CreateAndInitializeBlockchain()
        {
            // First create credentials
            var args = new[] 
            {
                "create-keys",
                "--credentials-path",
                _credentialsPath,
            };
            var result = await CLI.Program.Main(args);
            Assert.Equal(0, result);

            // Then create blockchain
            args = new[] 
            {
                "create-blockchain",
                "--credentials-path",
                _credentialsPath,
                "--blockchain-path",
                _testDirectory
            };
            result = await CLI.Program.Main(args);
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CreateInitialBlockChain()
        {
            await CreateAndInitializeBlockchain();

            var client = _factory.CreateClient();
            var creatorKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(_credentialsPath);

            var response = await client.GetAsync($"/blockchain/blocks?fromIndex=0&toIndex=50");
            var responseJson = JArray.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(1, responseJson.Count);

            var firstBlock = responseJson.Single();
            var blockIndex = firstBlock.Value<int>("blockIndex");
            var previousBlockHashBase58 = firstBlock.Value<string>("previousBlockHashBase58");
            var transactions = firstBlock["transactions"] as JArray;
            var signatures = firstBlock["signatures"] as JArray;
            Assert.Equal(0, blockIndex);
            Assert.Equal(string.Empty, previousBlockHashBase58);
            Assert.NotNull(transactions);
            Assert.Equal(2, transactions.Count);
            Assert.NotNull(signatures);
            Assert.Equal(1, signatures.Count);
            Assert.Equal(creatorKeys.PublicKeyBase58, signatures[0].Value<string>("publicKeyBase58"));

            var promoteUserTransaction = transactions.SingleOrDefault(t => (TransactionType)t.Value<int>("transactionType") == TransactionType.PromoteUser);
            Assert.NotNull(promoteUserTransaction);
            Assert.Equal(UserDesignation.SuperAdministrator, (UserDesignation)promoteUserTransaction.Value<int>("userDesignation"));

            var addAliasTransaction = transactions.SingleOrDefault(t => (TransactionType)t.Value<int>("transactionType") == TransactionType.AddAlias);
            Assert.NotNull(addAliasTransaction);
            Assert.Equal(BlockModel.CreatorAlias, addAliasTransaction.Value<string>("alias"));
        }
    }
}