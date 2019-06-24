using System.Collections.Generic;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ConflictingTransactionsFoundInParallelGroupsEvent
    {
        public ConflictingTransactionsFoundInParallelGroupsEvent(List<ExecutionReturnSet> existingSets,
            List<ExecutionReturnSet> conflictingSets)
        {
            ExistingSets = existingSets;
            ConflictingSets = conflictingSets;
        }

        public List<ExecutionReturnSet> ExistingSets { get; }
        public List<ExecutionReturnSet> ConflictingSets { get; }
    }
}