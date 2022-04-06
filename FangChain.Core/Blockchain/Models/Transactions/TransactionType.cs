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
        PromoteUser = 1,
        AddToUserBalance = 2,
        DisableUser = 3,
        EnableUser = 4,
    }
}
