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
    public partial class Transaction : ITransaction
    {

        public Hash GetHash()
        {
            return new Hash(this.CalculateHash());
        }

        public ITransactionParallelMetaData GetParallelMetaData()
        {
            throw new NotImplementedException();
        }

        public string MethodName { get; set; }
        public object[] Params { get; set; }
        public Hash From { get; set; }
        public Hash To { get; set; }
        public ulong IncrementId { get; set; }

        public Hash LastBlockHashWhenCreating()
        {
            throw new NotImplementedException();
        }

    }
}
