using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.Blockchain
{
    public class BlockExecutedSet
    {
        public Block Block { get; set; }
        public List<TransactionResult> TransactionResults { get; set; }
        public List<Transaction> Transactions { get; set; }

        public long Height => Block.Height;

        public Hash GetHash()
        {
            return Block.GetHash();
        }

        public IEnumerable<Hash> TransactionIds => Block.TransactionIds;
    }
}