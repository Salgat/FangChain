using System.Numerics;

namespace FangChain.Server
{
    public record struct UserBalanceResponse(string PublicKeyBase58, BigInteger UserBalance);
}
