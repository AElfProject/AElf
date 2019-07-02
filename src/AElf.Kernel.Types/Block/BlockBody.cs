using System;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => Transactions.Count;

        /// <inheritdoc/>
        public Hash GetHash()
        {
            return Hash.FromRawBytes(this.ToByteArray());
        }
    }
}