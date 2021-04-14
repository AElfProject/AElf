using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel
{
    public class BlockMinedEventData
    {
        public BlockHeader BlockHeader { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}