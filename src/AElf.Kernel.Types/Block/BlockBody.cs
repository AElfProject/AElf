using System;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => Transactions.Count;
        private Hash _blockBodyHash;

        private Hash CalculateBodyHash()
        {
            // TODO: BlockHeader is useless.
            if (BlockHeader == null)
                throw new InvalidOperationException("Block header hash is null.");
            _blockBodyHash = Hash.FromRawBytes(this.ToByteArray());
            return _blockBodyHash;
        }

        /// <inheritdoc/>
        public Hash GetHash()
        {
            return _blockBodyHash ?? CalculateBodyHash();
        }

        public Hash GetHashWithoutCache()
        {
            _blockBodyHash = null;
            return GetHash();
        }
    }
}