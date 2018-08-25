using System;
using System.Security.Cryptography;
using System.Threading;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;
using Google.Protobuf;
using Org.BouncyCastle.Math;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Transaction : ITransaction
    {
        private int _claimed;

        public bool Claim()
        {
            var res = Interlocked.CompareExchange(ref _claimed, 1, 0);
            return res == 0;
        }

        public bool Unclaim()
        {
            var res = Interlocked.CompareExchange(ref _claimed, 0, 1);
            return res == 1;
        }

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
                RefBlockNumber = RefBlockNumber,
                RefBlockPrefix = RefBlockPrefix,
                MethodName = MethodName,
                Type = Type
            };
            if (Params.Length != 0)
                txData.Params = Params;
            return txData.ToByteArray();
        }
    }
}
