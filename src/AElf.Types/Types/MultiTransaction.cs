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
        
        public ValidationStatus VerifyFields()
        {
            if (Transactions.Count < 2)
                return ValidationStatus.OnlyOneTransaction;

            if (!AllTransactionsHaveSameFrom())
                return ValidationStatus.MoreThanOneFrom;

            if (Transactions.Any(transaction => string.IsNullOrEmpty(transaction.Transaction.MethodName)))
                return ValidationStatus.MethodNameIsEmpty;

            if (Transactions.Any(transaction => transaction.Transaction.Signature.IsEmpty))
            {
                return ValidationStatus.UserSignatureIsEmpty;
            }

            return ValidationStatus.Success;
        }
        
        public enum ValidationStatus
        {
            Success,
            OnlyOneTransaction,
            MoreThanOneFrom,
            MethodNameIsEmpty,
            UserSignatureIsEmpty
        }

        private bool AllTransactionsHaveSameFrom()
        {
            var firstFrom = Transactions[0].Transaction.From;
            return Transactions.All(transaction => transaction.Transaction.From == firstFrom);
        }

        private byte[] GetSignatureData()
        {
            var verifyResult = VerifyFields();
            if (verifyResult != ValidationStatus.Success)
                throw new InvalidOperationException($"Invalid multi transaction, {verifyResult.ToString()}: {this}");

            if (Signature.IsEmpty)
                return this.ToByteArray();

            var multiTransaction = Clone();
            multiTransaction.Signature = ByteString.Empty;
            return multiTransaction.ToByteArray();
        }
    }
}