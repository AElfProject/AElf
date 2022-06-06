using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Domain;

public class ExecutionReturnSetCollection
{
    public ExecutionReturnSetCollection(IEnumerable<ExecutionReturnSet> returnSets)
    {
        AddRange(returnSets);
    }

    public List<ExecutionReturnSet> Executed { get; } = new();


    public List<ExecutionReturnSet> Conflict { get; } = new();

    public void AddRange(IEnumerable<ExecutionReturnSet> returnSets)
    {
        foreach (var returnSet in returnSets)
            if (returnSet.Status == TransactionResultStatus.Mined ||
                returnSet.Status == TransactionResultStatus.Failed)
                Executed.Add(returnSet);
            else if (returnSet.Status == TransactionResultStatus.Conflict) Conflict.Add(returnSet);
    }

    public BlockStateSet ToBlockStateSet()
    {
        var blockStateSet = new BlockStateSet();
        foreach (var returnSet in Executed)
        {
            foreach (var change in returnSet.StateChanges)
            {
                blockStateSet.Changes[change.Key] = change.Value;
                blockStateSet.Deletes.Remove(change.Key);
            }

            foreach (var delete in returnSet.StateDeletes)
            {
                blockStateSet.Deletes.AddIfNotContains(delete.Key);
                blockStateSet.Changes.Remove(delete.Key);
            }
        }

        return blockStateSet;
    }

    public List<ExecutionReturnSet> GetExecutionReturnSetList()
    {
        return Executed.Concat(Conflict).ToList();
    }
}