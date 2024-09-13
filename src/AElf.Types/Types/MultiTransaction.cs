using System;
using System.Linq;
using Google.Protobuf;

namespace AElf.Types
{
    public partial class MultiTransaction
    {
        private Hash _transactionId;

        public Hash GetHash()
        {
            if (_transactionId == null)
                _transactionId = HashHelper.ComputeFrom(GetSignatureData());

            return _transactionId;
        }
        
        public bool VerifyFields()
        {
            if (Transactions.Count < 2)
                return false;

            if (!AllTransactionsHaveSameFrom())
                return false;

            if (Transactions.Any(transaction => string.IsNullOrEmpty(transaction.Transaction.MethodName)))
                return false;

            return true;
        }

        private bool AllTransactionsHaveSameFrom()
        {
            var firstFrom = Transactions[0].Transaction.From;
            return Transactions.All(transaction => transaction.Transaction.From == firstFrom);
        }

        private byte[] GetSignatureData()
        {
            if (!VerifyFields())
                throw new InvalidOperationException($"Invalid x transaction: {this}");

            if (Signature.IsEmpty)
                return this.ToByteArray();

            var transaction = Clone();
            transaction.Signature = ByteString.Empty;
            return transaction.ToByteArray();
        }
    }
}