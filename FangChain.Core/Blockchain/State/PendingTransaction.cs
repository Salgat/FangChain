using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class PendingTransaction
    {
        public DateTimeOffset DateTimeRecieved { get; set; }
        public DateTimeOffset ExpireAfter { get; set; }
        public int MaxBlockIndexToAddTo { get; set; }
        public TransactionModel Transaction { get; set; }
    }
}
