using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IBatch
    {
        List<Job> Jobs();
        void AddTransaction(ITransaction tx);
    }
}