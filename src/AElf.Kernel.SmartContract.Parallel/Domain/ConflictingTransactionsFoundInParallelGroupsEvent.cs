using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ConflictingTransactionsFoundInParallelGroupsEvent
    {
        public ConflictingTransactionsFoundInParallelGroupsEvent(
            long previousBlockHeight,
            Hash previousBlockHash,
            List<ExecutionReturnSet> existingSets,
            List<ExecutionReturnSet> conflictingSets)
        {
            PreviousBlockHeight = previousBlockHeight;
            PreviousBlockHash = previousBlockHash;
            ExistingSets = existingSets;
            ConflictingSets = conflictingSets;
        }

        public long PreviousBlockHeight { get; }
        public Hash PreviousBlockHash { get; }
        public List<ExecutionReturnSet> ExistingSets { get; }
        public List<ExecutionReturnSet> ConflictingSets { get; }
    }
}