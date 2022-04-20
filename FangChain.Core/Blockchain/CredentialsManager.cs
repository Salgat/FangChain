using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class CredentialsManager : ICredentialsManager
    {
        private PublicAndPrivateKeys _hostPublicAndPrivateKeys;

        public PublicAndPrivateKeys GetHostCredentials()
            => _hostPublicAndPrivateKeys;

        public void SetHostCredentials(PublicAndPrivateKeys hostCredentials)
        {
            _hostPublicAndPrivateKeys = hostCredentials;
        }

        public void SetHostCredentials(Base58PublicAndPrivateKeys hostCredentials)
        {
            _hostPublicAndPrivateKeys = PublicAndPrivateKeys.FromBase58(hostCredentials);
        }
    }
}
