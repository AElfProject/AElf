using System;
using System.Security.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;
using Google.Protobuf;
using Org.BouncyCastle.Math;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Transaction : ITransaction
    {
        public Hash GetHash()
        {
            return SHA256.Create().ComputeHash(GetSignatureData());
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }

        public ITransactionParallelMetaData GetParallelMetaData()
        {
            throw new NotImplementedException();
        }

        public Hash LastBlockHashWhenCreating()
        {
            throw new NotImplementedException();
        }

        public ECSignature GetSignature()
        {
            BigInteger[] sig = new BigInteger[2];
            sig[0] = new BigInteger(R.ToByteArray());
            sig[1] = new BigInteger(S.ToByteArray());
            
            return new ECSignature(sig);
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
            };
            if (Params.Length != 0)
                txData.Params = Params;
            return txData.ToByteArray();
        }
    }
}
