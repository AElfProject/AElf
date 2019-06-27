using System;
using Google.Protobuf;

namespace AElf.Types
{
    public partial class Transaction
    {
        public Hash GetHash()
        {
            return Hash.FromRawBytes(GetSignatureData());
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