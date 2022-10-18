using System.Collections.Generic;

namespace AElf.Kernel.SmartContract.Parallel;

public class ConflictingTransactionsFoundInParallelGroupsEvent
{
    public ConflictingTransactionsFoundInParallelGroupsEvent(
        BlockHeader blockHeader,
        List<ExecutionReturnSet> existingSets,
        List<ExecutionReturnSet> conflictingSets)
    {
        BlockHeader = blockHeader;
        ExistingSets = existingSets;
        ConflictingSets = conflictingSets;
    }

    public BlockHeader BlockHeader { get; set; }
    public List<ExecutionReturnSet> ExistingSets { get; }
    public List<ExecutionReturnSet> ConflictingSets { get; }
}