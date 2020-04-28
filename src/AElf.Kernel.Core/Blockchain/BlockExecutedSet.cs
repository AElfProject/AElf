using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.Blockchain
{
    public class BlockExecutedSet
    {
        public Block Block { get; set; }
        public IDictionary<Hash, TransactionResult> TransactionResultMap { get; set; }

        public IDictionary<Hash, Transaction> TransactionMap { get; set; }

        public long Height => Block.Height;

        public Hash GetHash()
        {
            return Block.GetHash();
        }

        public IEnumerable<Hash> TransactionIds => Block.TransactionIds;
    }
}