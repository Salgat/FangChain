using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class LumpedTransaction : TransactionModel
    {
        public override TransactionType TransactionType { get => TransactionType.Lumped; }
        public IEnumerable<TransactionModel> Transactions { get; set; }

        public LumpedTransaction(IEnumerable<TransactionModel> transactions, string transactionId) : base(transactionId)
        {
            Transactions = transactions;
        }

        protected override void PopulateBytesFromProperties(MemoryStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(Id));
            stream.Write(BitConverter.GetBytes((int)TransactionType));
            foreach (var transaction in Transactions)
            {
                var transactionBytes = transaction.GetBytes();
                stream.Write(transactionBytes);
            }
        }
    }
}
