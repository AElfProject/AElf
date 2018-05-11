using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Concurrency
{
    public interface IScheduler
    {
        Task<List<List<ITransaction>>> ScheduleTransactions(Dictionary<Hash, List<ITransaction>> txDict);
    }
}