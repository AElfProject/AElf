using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public interface IBatch : IEnumerable<Job>
    {
        void AddTransaction(ITransaction tx);
        List<Job> Jobs { get; }
    }
}