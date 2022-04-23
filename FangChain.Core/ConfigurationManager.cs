using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class ConfigurationManager : IConfigurationManager
    {
        private DirectoryInfo? _blockchainDirectory = null;

        public void SetBlockchainDirectory(DirectoryInfo directoryInfo)
            => _blockchainDirectory = directoryInfo;

        public DirectoryInfo? GetBlockchainDirectory()
            => _blockchainDirectory;
    }
}
