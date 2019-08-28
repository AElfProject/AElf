using System;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => TransactionIds.Count;
        private Hash _blockBodyHash;

        private Hash CalculateBodyHash()
        {
            // TODO: BlockHeader is useless.
            if (!VerifyFields())
                throw new InvalidOperationException($"Invalid block body.");

            _blockBodyHash = Hash.FromRawBytes(this.ToByteArray());
            return _blockBodyHash;
        }

        public bool VerifyFields()
        {
            if (TransactionIds.Count == 0)
                return false;

            if (BlockHeader == null)
                return false;

            return true;
        }

        public Hash GetHash()
        {
            return _blockBodyHash ?? CalculateBodyHash();
        }
    }
}