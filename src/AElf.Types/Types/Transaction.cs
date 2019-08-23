using System;
using Google.Protobuf;

namespace AElf.Types
{
    public partial class Transaction
    {
        private Hash _transactionId;

        public Hash GetHash()
        {
            if (_transactionId == null)
                _transactionId = Hash.FromRawBytes(GetSignatureData());

            return _transactionId;
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