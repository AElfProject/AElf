using System;
using Google.Protobuf;

namespace AElf.Types
{
    public partial class InlineTransaction
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
            if (To == null || From == null)
                return false;

            if (string.IsNullOrEmpty(MethodName))
                return false;

            if (OriginTransactionId == null)
                return false;
            return true;
        }

        private byte[] GetSignatureData()
        {
            if (!VerifyFields())
                throw new InvalidOperationException($"Invalid transaction: {this}");
            return this.ToByteArray();
        }
    }
}