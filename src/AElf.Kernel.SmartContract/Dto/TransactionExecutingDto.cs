using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public class TransactionExecutingDto
    {
        public BlockHeader BlockHeader { get; set; }
        public IEnumerable<Transaction> Transactions { get; set; }
        public BlockStateSet PartialBlockStateSet { get; set; }
    }
}