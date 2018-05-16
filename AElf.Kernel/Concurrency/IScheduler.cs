using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Concurrency
{
    public interface IScheduler
    {
        List<IParallelGroup> ScheduleTransactions(List<ITransaction> txList);
    }
}