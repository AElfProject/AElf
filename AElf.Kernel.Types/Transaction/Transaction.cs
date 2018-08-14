using System;
using System.Security.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;
using Google.Protobuf;
using Org.BouncyCastle.Math;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Transaction
    {
        public Hash GetHash()
        {
            return SHA256.Create().ComputeHash(GetSignatureData());
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }

        public ECSignature GetSignature()
        {
            return new ECSignature(R.ToByteArray(), S.ToByteArray());
        }

        public int Size()
        {
            return CalculateSize();
        }

        public byte[] GetSignatureData()
        {
            Transaction txData = new Transaction
            {
                From = From.Clone(),
                To = To.Clone(),
                IncrementId = IncrementId,
                MethodName = MethodName,
                Type = Type
            };
            if (Params.Length != 0)
                txData.Params = Params;
            return txData.ToByteArray();
        }
    }
}
