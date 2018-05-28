using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{
    public interface IBatcher
    {
        List<List<ITransaction>> Process(List<ITransaction> transactions);
    }
}
