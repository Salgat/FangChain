using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    public record struct SignatureModel(string PublicKeyBase58, string SignatureBase58)
    {
        public byte[] GetBytes() 
            => Encoding.ASCII.GetBytes(PublicKeyBase58)
                .Concat(Encoding.ASCII.GetBytes(SignatureBase58))
                .ToArray();
    }
}
