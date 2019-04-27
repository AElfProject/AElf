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
            if (To == null || From == null || string.IsNullOrEmpty(MethodName) || RefBlockNumber < 0)
            {
                throw new InvalidOperationException($"Invalid trancation: {this}");
            }
            
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