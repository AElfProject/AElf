using AElf.Kernel.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    public class Transaction : ITransaction
    {
        public Transaction() { }

        public IHash<ITransaction> GetHash()
        {
            return new Hash<ITransaction>(this.CalculateHash());
        }

        public ITransactionParallelMetaData GetParallelMetaData()
        {
            throw new NotImplementedException();
        }

        public string MethodName { get; set; }
        public object[] Params { get; set; }
        public byte[] From { get; set; }
        public byte[] To { get; set; }
        public ulong IncrementId { get; set; }

        public IHash<IBlockHeader> LastBlockHashWhenCreating()
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
