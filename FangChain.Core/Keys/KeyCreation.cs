using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NokitaKaze.Base58Check;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace FangChain
{
    public class KeyCreation : IKeyCreation
    {
        public PublicAndPrivateKeys CreatePublicAndPrivateKeys()
        {
            // Private Key
            var privateKey = new byte[32];
            var random = RandomNumberGenerator.Create();
            do
            {
                random.GetBytes(privateKey);
            } while (!Secp256k1Singleton.Execute(secp256k1 => secp256k1.SecretKeyVerify(privateKey)));

            // Public Key
            var publicKey = new byte[64];
            if (!Secp256k1Singleton.Execute(secp256k1 => secp256k1.PublicKeyCreate(publicKey, privateKey)))
            {
                throw new Exception("Failed to create a valid private key.");
            }

            return new PublicAndPrivateKeys
            {
                PrivateKey = privateKey,
                PublicKey = publicKey
            };
        }

        public async Task StoreKeysAsync(string destinationPath, PublicAndPrivateKeys publicAndPrivateKeys, CancellationToken cancellationToken = default)
        {
            var publicKeyBase58 = Base58CheckEncoding.Encode(publicAndPrivateKeys.PublicKey);
            var privateKeyBase58 = Base58CheckEncoding.Encode(publicAndPrivateKeys.PrivateKey);
            var fileContents = new Base58PublicAndPrivateKeys
            {
                PublicKeyBase58 = publicKeyBase58,
                PrivateKeyBase58 = privateKeyBase58
            };
            var fileContentsJson = JsonConvert.SerializeObject(fileContents, Formatting.Indented); // JSON.NET used because it will properly serialize derived types
            await File.WriteAllTextAsync(destinationPath, fileContentsJson, cancellationToken);
        }

        public async Task<PublicAndPrivateKeys> LoadKeysAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            var fileContentsJson = await File.ReadAllTextAsync(sourcePath, cancellationToken);
            var keysBase58 = System.Text.Json.JsonSerializer.Deserialize<Base58PublicAndPrivateKeys>(fileContentsJson);
            var publicKey = Base58CheckEncoding.Decode(keysBase58.PublicKeyBase58);
            var privateKey = Base58CheckEncoding.Decode(keysBase58.PrivateKeyBase58);
            return new PublicAndPrivateKeys
            {
                PublicKey = publicKey,
                PrivateKey = privateKey
            };
        }
    }
}
