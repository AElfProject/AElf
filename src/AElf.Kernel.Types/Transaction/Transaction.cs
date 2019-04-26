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

        // TODO: remove
        public byte[] GetHashBytes()
        {
            return SHA256.Create().ComputeHash(GetSignatureData());
        }

        // TODO: change to clone
        private byte[] GetSignatureData()
        {
            var txData = new Transaction
            {
                From = From.Clone(),
                To = To.Clone(),
                MethodName = MethodName
            };
            txData.Params = Params;
            txData.RefBlockNumber = RefBlockNumber;
            txData.RefBlockPrefix = RefBlockPrefix;
            return txData.ToByteArray();
        }
    }
}