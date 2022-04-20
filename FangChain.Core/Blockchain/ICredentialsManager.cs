using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public interface ICredentialsManager
    {
        void SetHostCredentials(PublicAndPrivateKeys hostCredentials);
        void SetHostCredentials(Base58PublicAndPrivateKeys hostCredentials);
        PublicAndPrivateKeys GetHostCredentials();
    }
}
