using AElf.Types;

namespace AElf.Runtime.WebAssembly.Extensions;

public static class Extensions
{
    public static bool Contains(this CallFlags callFlags, CallFlags other)
    {
        return (callFlags & other) == other;
    }

    /// <summary>
    /// TODO: Improve
    /// </summary>
    /// <param name="executeReturnValue"></param>
    /// <returns></returns>
    public static ReturnCode ToReturnCode(this ExecuteReturnValue executeReturnValue)
    {
        if (executeReturnValue.Flags == ReturnFlags.Empty)
        {
            return ReturnCode.Success;
        }

        return ReturnCode.CalleeTrapped;
    }

    public static TransactionExecutingStateSet Merge(this TransactionExecutingStateSet stateSet,
        TransactionExecutingStateSet? anotherStateSet)
    {
        if (anotherStateSet == null)
        {
            return stateSet;
        }

        stateSet.Writes.Add(anotherStateSet.Writes);
        stateSet.Reads.Add(anotherStateSet.Reads);
        stateSet.Deletes.Add(anotherStateSet.Deletes);
        return stateSet;
    }
}