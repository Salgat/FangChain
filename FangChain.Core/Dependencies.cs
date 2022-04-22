using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public static class Dependencies
    {
        public static void AddFangChainDependencies(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IKeyCreation, KeyCreation>();
            serviceCollection.AddTransient<IBlockchainCreation, BlockchainCreation>();
            serviceCollection.AddTransient<IValidator, Validator>();
            serviceCollection.AddTransient<IBlockchainRules, BlockchainRules>();
            serviceCollection.AddTransient<ILoader, Loader>();
            serviceCollection.AddTransient<ICompactor, Compactor>();

            // In-memory state
            serviceCollection.AddSingleton<ICredentialsManager, CredentialsManager>();
            serviceCollection.AddSingleton<IBlockchainMutator, BlockchainMutator>();
            serviceCollection.AddSingleton<IBlockchainState, BlockchainStateInMemory>();
            serviceCollection.AddSingleton<IPendingTransactions, PendingTransactionsInMemory>();
        }
    }
}
