using System;
using AElf.Kernel.Extensions;
using Google.Protobuf;

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
    }
}
