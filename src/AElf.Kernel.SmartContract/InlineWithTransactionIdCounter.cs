using System.Threading;

namespace AElf.Kernel.SmartContract;

public class InlineWithTransactionIdCounter
{
    private int _count = 0;

    public void Increment()
    {
        Interlocked.Increment(ref _count);
    }

    public int GetCount()
    {
        return _count;
    }
}