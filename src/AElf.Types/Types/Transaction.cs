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
            {
                _transactionHash = Hash.FromRawBytes(GetSignatureData());
            }

            return _transactionHash;
        }

        public byte[] GetHashBytes()
        {
            return SHA256.Create().ComputeHash(GetSignatureData());
        }

        private byte[] GetSignatureData()
        {
            if (To == null || From == null || string.IsNullOrEmpty(MethodName) || RefBlockNumber < 0)
            {
                throw new InvalidOperationException($"Invalid transaction: {this}");
            }

            if (Signature.IsEmpty)
                return this.ToByteArray();

            var transaction = Clone();
            transaction.Signature = ByteString.Empty;
            return transaction.ToByteArray();
        }
    }
}