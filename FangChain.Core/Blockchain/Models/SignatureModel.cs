using NokitaKaze.Base58Check;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public record struct SignatureModel(string PublicKeyBase58, string SignatureBase58)
    {
        public byte[] GetBytes() 
            => Encoding.ASCII.GetBytes(PublicKeyBase58)
                .Concat(Encoding.ASCII.GetBytes(SignatureBase58))
                .ToArray();

        public byte[] GetPublicKey()
        {
            var publicKey = Base58CheckEncoding.Decode(PublicKeyBase58);
            Array.Resize(ref publicKey, 64);
            return publicKey;
        }
            
        public byte[] GetSignature() => Base58CheckEncoding.Decode(SignatureBase58);
    }
}
