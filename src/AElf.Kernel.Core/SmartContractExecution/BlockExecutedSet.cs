using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution
{
    public class BlockExecutedSet
    {
        public Block Block { get; set; }
        public IDictionary<Hash,TransactionResult> TransactionResultMap { get; set; }
        
        public IDictionary<Hash,Transaction> TransactionMap { get; set; }
    }
}