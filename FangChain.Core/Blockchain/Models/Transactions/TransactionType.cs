using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public enum TransactionType
    {
        AddAlias = 0,
        PromoteUser = 100,

        // Balance
        AddToUserBalance = 200,
        TransferToUserBalance = 300,

        // Enable/Disable
        DisableUser = 400,
        EnableUser = 500,

        // Tokens
        AddToken = 600,
        RemoveToken = 700,
        TransferToken = 800
    }
}
