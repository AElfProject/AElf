using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;

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

        foreach (var write in anotherStateSet.Writes)
        {
            stateSet.Writes.TryAdd(write.Key, write.Value);
        }

        foreach (var read in anotherStateSet.Reads)
        {
            stateSet.Reads.TryAdd(read.Key, read.Value);
        }

        foreach (var delete in anotherStateSet.Deletes)
        {
            stateSet.Deletes.TryAdd(delete.Key, delete.Value);
        }

        return stateSet;
    }

    public static TransactionExecutingStateSet ReplaceAddress(this TransactionExecutingStateSet stateSet,
        string selfAddress)
    {
        var writes = new Dictionary<string, ByteString>();
        foreach (var write in stateSet.Writes)
        {
            writes.Add($"{selfAddress}{write.Key[selfAddress.Length..]}", write.Value);
        }

        stateSet.Writes.Clear();
        stateSet.Writes.Add(writes);
        return stateSet;
    }
}