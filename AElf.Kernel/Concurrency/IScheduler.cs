using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Concurrency
{
    public interface IScheduler
    {
        Task<List<List<Transaction>>> ScheduleTransactions();
    }
}