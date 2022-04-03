using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class UserSummary
    {
        public string Alias { get; set; } = "";
        public UserDesignation Designation { get; set; } = UserDesignation.Anonymous;
        public BigInteger Balance { get; set; } = BigInteger.Zero;
    }
}
