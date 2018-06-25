using System.Collections.Generic;
using AElf.Kernel.Types;

namespace AElf.Kernel.Concurrency.Scheduling
{
    public interface IBatcher
    {
        List<List<ITransaction>> Process(List<ITransaction> transactions);
    }
}
