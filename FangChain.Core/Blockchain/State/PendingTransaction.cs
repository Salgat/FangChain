using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public class PendingTransaction
    {
        private string _transactionJson;
        private TransactionModel _transaction;

        public DateTimeOffset DateTimeRecieved { get; set; }
        public DateTimeOffset ExpireAfter { get; set; }
        public long MaxBlockIndexToAddTo { get; set; }
        public string TransactionJson 
        { 
            get 
            { 
                return _transactionJson;
            } 
            set 
            {
                _transactionJson = value;
                _transaction = ParseTransaction(value);
            } 
        }
        public TransactionModel? Transaction => _transaction;

        private static TransactionModel ParseTransaction(string transactionJson)
        {
            return Loader.DeserializeTransaction(JObject.Parse(transactionJson));
        }
    }
}
