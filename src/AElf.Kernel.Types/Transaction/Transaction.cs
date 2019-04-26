using System;
using System.Security.Cryptography;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class Transaction
    {
        public Hash GetHash()
        {
            return Hash.FromRawBytes(GetSignatureData());
        }

        public byte[] GetHashBytes()
        {
            return SHA256.Create().ComputeHash(GetSignatureData());
        }

        private byte[] GetSignatureData()
        {
            if (this.Signature == ByteString.Empty)
                return this.ToByteArray();

            var transaction = this.Clone();
            transaction.Signature = ByteString.Empty;
            return transaction.ToByteArray();
        }
    }
}