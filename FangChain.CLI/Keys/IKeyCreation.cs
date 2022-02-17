using NokitaKaze.Base58Check;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain.CLI
{
    public struct PublicAndPrivateKeys
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }

        public static PublicAndPrivateKeys FromBase58(Base58PublicAndPrivateKeys base58Keys) => new()
            {
                PublicKey = Base58CheckEncoding.Decode(base58Keys.PrivateKeyBase58),
                PrivateKey = Base58CheckEncoding.Decode(base58Keys.PrivateKeyBase58),
            };

        public string GetBase58PublicKey() => Base58CheckEncoding.Encode(PublicKey);
        public string GetBase58PrivateKey() => Base58CheckEncoding.Encode(PrivateKey);
    }

    public struct Base58PublicAndPrivateKeys
    {
        public string PublicKeyBase58 { get; set; }
        public string PrivateKeyBase58 { get; set; }
    }

    public interface IKeyCreation
    {
        PublicAndPrivateKeys CreatePublicAndPrivateKeys();
        Task StoreKeysAsync(string destinationPath, PublicAndPrivateKeys publicAndPrivateKeys, CancellationToken cancellationToken = default);
        Task<PublicAndPrivateKeys> LoadKeysAsync(string sourcePath, CancellationToken cancellationToken = default);
    }
}
