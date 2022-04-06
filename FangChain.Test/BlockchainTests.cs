using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
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
            await CreateKeys(_credentialsPath);
            await CreateBlockchain(_credentialsPath, _testDirectory);
        }

        private static async Task CreateKeys(string credentialsPath)
        {
            var args = new[]
            {
                "create-keys",
                "--credentials-path",
                credentialsPath,
            };
            var result = await CLI.Program.Main(args);
            Assert.Equal(0, result);
        }

        private static async Task CreateBlockchain(string credentialsPath, string blockchainDirectory)
        {
            var args = new[]
            {
                "create-blockchain",
                "--credentials-path",
                credentialsPath,
                "--blockchain-path",
                blockchainDirectory
            };
            var result = await CLI.Program.Main(args);
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
            var blockIndex = firstBlock.Value<int>(nameof(BlockModel.BlockIndex));
            var previousBlockHashBase58 = firstBlock.Value<string>(nameof(BlockModel.PreviousBlockHashBase58));
            var transactions = firstBlock[nameof(BlockModel.Transactions)] as JArray;
            var signatures = firstBlock[nameof(BlockModel.Signatures)] as JArray;
            Assert.Equal(0, blockIndex);
            Assert.Equal(string.Empty, previousBlockHashBase58);
            Assert.NotNull(transactions);
            Assert.Equal(2, transactions.Count);
            Assert.NotNull(signatures);
            Assert.Equal(1, signatures.Count);
            Assert.Equal(creatorKeys.PublicKeyBase58, signatures[0].Value<string>(nameof(Base58PublicAndPrivateKeys.PublicKeyBase58)));

            var promoteUserTransaction = transactions.SingleOrDefault(t => (TransactionType)t.Value<int>(nameof(TransactionType)) == TransactionType.PromoteUser);
            Assert.NotNull(promoteUserTransaction);
            Assert.Equal(UserDesignation.SuperAdministrator, (UserDesignation)promoteUserTransaction.Value<int>(nameof(UserDesignation)));

            var addAliasTransaction = transactions.SingleOrDefault(t => (TransactionType)t.Value<int>(nameof(TransactionType)) == TransactionType.AddAlias);
            Assert.NotNull(addAliasTransaction);
            Assert.Equal(BlockModel.CreatorAlias, addAliasTransaction.Value<string>(nameof(AddAliasTransaction.Alias)));
        }

        [Fact]
        public async Task ProposeTransaction_PromoteUser_Verified()
        {
            await CreateAndInitializeBlockchain();
            var client = _factory.CreateClient();
            var secondUserCredentialsPath = Path.Combine(_testDirectory, $"testCredentials-{Guid.NewGuid():N}.json");
            await CreateKeys(secondUserCredentialsPath);

            var creatorKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(_credentialsPath);
            var secondUserKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(secondUserCredentialsPath);

            // Promote second User to verified
            var promoteUserTransaction = new PromoteUserTransaction(creatorKeys.PublicKeyBase58, UserDesignation.Verified);
            promoteUserTransaction.AddSignature(PublicAndPrivateKeys.FromBase58(creatorKeys));
            var proposedTransaction = new PendingTransaction()
            {
                DateTimeRecieved = DateTime.UtcNow,
                ExpireAfter = DateTimeOffset.MaxValue,
                MaxBlockIndexToAddTo = long.MaxValue,
                TransactionJson = JObject.FromObject(promoteUserTransaction).ToString()
                // TODO: Add signatures to PendingTransaction that must match the Transaction.
                // Even if transaction is signed, there's nothing stopping
                // a discarded transaction from being proposed by a malicious actor
            };
            var response = await client.PostAsJsonAsync($"/transaction", proposedTransaction);
            Assert.True(response.IsSuccessStatusCode);
            await WaitUntil(async () => 
            {
                return await client.GetFromJsonAsync<bool>($"/transaction/confirmed?transactionHash={promoteUserTransaction.GetHashString()}");
            });

            // Check that transaction has been added in next block
            response = await client.GetAsync($"/blockchain/blocks?fromIndex=0&toIndex=50");
            var responseJson = JArray.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(2, responseJson.Count);

            var secondBlock = (JObject)responseJson[1];
            var blockModel = new BlockModel(secondBlock);
            var blockIndex = secondBlock.Value<int>(nameof(BlockModel.BlockIndex));
            var previousBlockHashBase58 = secondBlock.Value<string>(nameof(BlockModel.PreviousBlockHashBase58));
            var transactions = secondBlock[nameof(BlockModel.Transactions)] as JArray;
            var signatures = secondBlock[nameof(BlockModel.Signatures)] as JArray;
            Assert.Equal(1, blockIndex);
        }

        [Fact]
        public async Task AddUserBalanceTransaction_Verified()
        {
            await CreateAndInitializeBlockchain();
            var client = _factory.CreateClient();
            var secondUserCredentialsPath = Path.Combine(_testDirectory, $"testCredentials-{Guid.NewGuid():N}.json");
            await CreateKeys(secondUserCredentialsPath);

            // TODO

        }

        private static async Task WaitUntil(Func<Task<bool>> condition, TimeSpan? maxWaitTime = default)
        {
            var waitTime = maxWaitTime ?? TimeSpan.FromSeconds(5);
            var start = DateTimeOffset.UtcNow;
            while (DateTimeOffset.UtcNow - start < waitTime)
            {
                await Task.Delay(500);
                try
                {
                    if (await condition()) return;
                } catch (Exception) { /* swallow */ }
            }
            throw new TimeoutException($"Condition exceeded max wait time.");
        }
    }
}