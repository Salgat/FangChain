using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public bool Disabled { get; set; } = false;
        public ImmutableDictionary<string, string> Tokens = ImmutableDictionary<string, string>.Empty; // Token Id => Token Contents
    }
}
