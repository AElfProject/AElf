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
        /// <summary>
        /// Use timestamp to prevent a over-time tx received by a block.
        /// </summary>
        public long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

        public Hash GetHash()
        {
            return this.CalculateHash();
        }

        public ITransactionParallelMetaData GetParallelMetaData()
        {
            throw new NotImplementedException();
        }

        public Hash LastBlockHashWhenCreating()
        {
            throw new NotImplementedException();
        }

    }
}
