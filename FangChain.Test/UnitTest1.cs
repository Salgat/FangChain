using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace FangChain.Test
{
    public class TransactionProposalTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TransactionProposalTests(WebApplicationFactory<Program> factory)
        {
            // TODO: Create credentials and blockchain as part of initialization, might need to bootstrap in Program.cs
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build(); 
            configuration["commandOverride"] = 
                @"host 
                --credentials-path C:\Users\salga\Documents\projects\FangChain-Test\keys-20220216T030145.json
                --blockchain-path C:\Users\salga\Documents\projects\FangChain-Test";
            factory = factory.WithWebHostBuilder(builder => 
            {
                builder.UseConfiguration(configuration);
            });
            _factory = factory;
        }

        [Fact]
        public async Task Test1()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/blockchain/blocks?fromIndex=0&toIndex=50");
            var responseJson = JToken.Parse(await response.Content.ReadAsStringAsync());

        }
    }
}