using System;
using Google.Protobuf;

namespace AElf.Types
{
    public partial class Transaction
    {
        private Hash _transactionId;

        public bool IsInlineTxWithId;
        private string _inlineTxIdFactor;

        public Hash GetHash()
        {
            if (_transactionId == null)
                _transactionId = HashHelper.ComputeFrom(GetSignatureData());

            return _transactionId;
        }

        public void SetInlineTxId(string inlineTxIdFactor)
        {
            _inlineTxIdFactor = inlineTxIdFactor;
            _transactionId = HashHelper.XorAndCompute(GetHash(), HashHelper.ComputeFrom(inlineTxIdFactor));
        }

        public string GetInlineTxIdFactor()
        {
            return _inlineTxIdFactor;
        }

        public bool VerifyFields()
        {
            if (To == null || From == null)
                return false;

            if (RefBlockNumber < 0)
                return false;

            if (string.IsNullOrEmpty(MethodName))
                return false;

            return true;
        }

        private byte[] GetSignatureData()
        {
            if (!VerifyFields())
                throw new InvalidOperationException($"Invalid transaction: {this}");

            if (Signature.IsEmpty)
                return this.ToByteArray();

            var transaction = Clone();
            transaction.Signature = ByteString.Empty;
            return transaction.ToByteArray();
        }
    }
}