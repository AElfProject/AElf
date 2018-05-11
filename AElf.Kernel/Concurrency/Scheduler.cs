using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TxMemPool;
using QuickGraph;


namespace AElf.Kernel.Concurrency
{
    public class Scheduler : IScheduler
    {
        protected IGrouper _grouper;

        public Scheduler(IGrouper grouper)
        {
            _grouper = grouper;
        }

        public async Task<List<List<ITransaction>>> ScheduleTransactions(Dictionary<Hash, List<ITransaction>> txDict)
        {
            throw new NotImplementedException();
        }
    }
}