using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;
using Google.Protobuf;
using Org.BouncyCastle.Math;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Transaction
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
            return Hash.FromRawBytes(GetSignatureData());
        }

        public byte[] GetHashBytes()
        {
            return SHA256.Create().ComputeHash(GetSignatureData());
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }

        public int Size()
        {
            return CalculateSize();
        }

        private byte[] GetSignatureData()
        {
            Transaction txData = new Transaction
            {
                From = From.Clone(),
                To = To.Clone(),
                MethodName = MethodName,
                Type = Type,
                RefBlockNumber = RefBlockNumber,
                RefBlockPrefix = RefBlockPrefix
            };
            if (Params.Length != 0)
                txData.Params = Params;
            if (Type == TransactionType.MsigTransaction) 
                return txData.ToByteArray();
            return txData.ToByteArray();
        }
    }
}
