using Microsoft.AspNetCore.Mvc;

namespace FangChain.Server
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly IBlockchainState _blockchainState;

        public UserController(IBlockchainState blockchainState)
        {
            _blockchainState = blockchainState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId">Can be either the user alias or base58 public key.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("balance")]
        public async Task<UserBalanceResponse> GetUserBalance(string userId)
        {
            if (_blockchainState.UserAliasToPublicKeyBase58.TryGetValue(userId, out var publicKeyBase58))
            {
                userId = publicKeyBase58;
            }
            if (_blockchainState.UserSummaries.TryGetValue(userId, out var userSummary))
            {
                return new UserBalanceResponse
                {
                    PublicKeyBase58 = userId,
                    UserBalance = userSummary.Balance
                };
            }
            return new UserBalanceResponse
            {
                PublicKeyBase58 = userId,
                UserBalance = 0
            };
        }
    }
}
