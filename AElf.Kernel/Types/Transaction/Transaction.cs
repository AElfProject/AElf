using System;
using System.Security.Cryptography;
using AElf.Kernel.Crypto.ECDSA;
using AElf.Kernel.Extensions;
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
            Transaction txData = new Transaction();
            txData.From = From.Clone();
            txData.To = To.Clone();
            txData.IncrementId = IncrementId;
                
            return txData.ToByteArray();
        }
    }
}
