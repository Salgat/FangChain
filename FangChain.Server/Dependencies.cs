using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.Server
{
    public static class Dependencies
    {
        public static void AddFangChainServerDependencies(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IBlockchainState, BlockchainState>();
        }
    }
}
