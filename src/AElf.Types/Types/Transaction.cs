using System;
using System.Security.Cryptography;
using Google.Protobuf;

namespace AElf.Types
{
    public partial class Transaction
    {
        private Hash _transactionHash;

        public Hash GetHash()
        {
            if (_transactionHash == null)
                _transactionHash = Hash.FromRawBytes(GetSignatureData());

            return _transactionHash;
        }

        public byte[] GetHashBytes()
        {
            return SHA256.Create().ComputeHash(GetSignatureData());
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