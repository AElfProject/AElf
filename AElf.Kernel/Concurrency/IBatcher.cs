using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IBatcher
    {
        List<List<Transaction>> Process(List<Transaction> transactions);
    }
}
