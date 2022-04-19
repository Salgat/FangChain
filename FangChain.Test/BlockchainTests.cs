using FangChain.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
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

            var designateUserTransaction = transactions.SingleOrDefault(t => (TransactionType)t.Value<int>(nameof(TransactionType)) == TransactionType.DesignateUser);
            Assert.NotNull(designateUserTransaction);
            Assert.Equal(UserDesignation.SuperAdministrator, (UserDesignation)designateUserTransaction.Value<int>(nameof(UserDesignation)));

            var setAliasTransaction = transactions.SingleOrDefault(t => (TransactionType)t.Value<int>(nameof(TransactionType)) == TransactionType.SetAlias);
            Assert.NotNull(setAliasTransaction);
            Assert.Equal(BlockModel.CreatorAlias, setAliasTransaction.Value<string>(nameof(SetAliasTransaction.Alias)));
        }

        [Fact]
        public async Task ProposeTransaction_DesignateUser_Verified()
        {
            await CreateAndInitializeBlockchain();
            var client = _factory.CreateClient();
            var secondUserCredentialsPath = Path.Combine(_testDirectory, $"testCredentials-{Guid.NewGuid():N}.json");
            await CreateKeys(secondUserCredentialsPath);

            var creatorKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(_credentialsPath);
            var secondUserKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(secondUserCredentialsPath);

            // designate second User to verified
            var designateUserTransaction = new DesignateUserTransaction(creatorKeys.PublicKeyBase58, UserDesignation.Verified);
            await PrepareAndAwaitTransaction(client, designateUserTransaction, creatorKeys);

            // Check that transaction has been added in next block
            var response = await client.GetAsync($"/blockchain/blocks?fromIndex=0&toIndex=50");
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

            var creatorKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(_credentialsPath);
            var secondUserKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(secondUserCredentialsPath);

            // Add balance to newly created user
            BigInteger amountToAdd = 123456789;
            var addBalanceToUserTransaction = new AddToUserBalanceTransaction(secondUserKeys.PublicKeyBase58, amountToAdd);
            await PrepareAndAwaitTransaction(client, addBalanceToUserTransaction, creatorKeys);

            var amountQueried = await client.GetStringAsync($"/user/balance?userId={secondUserKeys.PublicKeyBase58}");
            var amountQueriedParsed = JObject.Parse(amountQueried).ToObject<UserBalanceResponse>();
            Assert.Equal(secondUserKeys.PublicKeyBase58, amountQueriedParsed.PublicKeyBase58);
            Assert.Equal(amountToAdd, amountQueriedParsed.UserBalance);
        }

        [Fact]
        public async Task CompactBlocks_Success()
        {
            await CreateAndInitializeBlockchain();
            var client = _factory.CreateClient();
            var secondUserCredentialsPath = Path.Combine(_testDirectory, $"testCredentials-{Guid.NewGuid():N}.json");
            await CreateKeys(secondUserCredentialsPath);

            var creatorKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(_credentialsPath);
            var secondUserKeys = await _factory.Services.GetRequiredService<ILoader>().LoadKeysAsync(secondUserCredentialsPath);

            // Create blockchain with 500 add transactions spread among 5 blocks
            BigInteger total = 0;
            var random = new Random((int)DateTimeOffset.UtcNow.Ticks);
            for (var i = 0; i < 5; ++i)
            {
                var addTransactions = Enumerable
                    .Range(0, 100)
                    .Select(_ =>
                    {
                        var amountToAdd = random.Next();
                        total += amountToAdd;
                        return new AddToUserBalanceTransaction(secondUserKeys.PublicKeyBase58, amountToAdd);
                    });
                await PrepareAndAwaitTransactions(client, addTransactions, creatorKeys);
            }

            var response = await client.GetAsync($"/blockchain/blocks?fromIndex=0&toIndex=99");
            var responseJson = JArray.Parse(await response.Content.ReadAsStringAsync());
        }

        #region Helper Methods
        private static Task PrepareAndAwaitTransaction<TTransaction>(HttpClient client, TTransaction transaction,
            params Base58PublicAndPrivateKeys[] keysToSignWith) where TTransaction : TransactionModel
            => PrepareAndAwaitTransactions(client, new[] { transaction }, keysToSignWith);

        private static async Task PrepareAndAwaitTransactions<TTransaction>(HttpClient client, IEnumerable<TTransaction> transactions, 
            params Base58PublicAndPrivateKeys[] keysToSignWith) where TTransaction : TransactionModel
        {
            foreach (var transaction in transactions)
            {
                foreach (var key in keysToSignWith)
                {
                    transaction.AddSignature(PublicAndPrivateKeys.FromBase58(key));
                }

                var proposedTransaction = new PendingTransaction()
                {
                    DateTimeRecieved = DateTime.UtcNow,
                    ExpireAfter = DateTimeOffset.MaxValue,
                    MaxBlockIndexToAddTo = long.MaxValue,
                    TransactionJson = JObject.FromObject(transaction).ToString()
                    // TODO: Add signatures to PendingTransaction that must match the Transaction.
                    // Even if transaction is signed, there's nothing stopping
                    // a discarded transaction from being proposed by a malicious actor
                };

                var response = await client.PostAsJsonAsync($"/transaction", proposedTransaction);
                Assert.True(response.IsSuccessStatusCode);
            }
            await WaitUntil(async () =>
            {
                var transactionsToCheck = transactions.Select(t 
                    => client.GetFromJsonAsync<bool>($"/transaction/confirmed?transactionHash={t.GetHashString()}")).ToList();
                await Task.WhenAll(transactionsToCheck);
                return transactionsToCheck.All(t => t.Result);
            });
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
        #endregion
    }
}