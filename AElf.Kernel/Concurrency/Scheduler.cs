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

        public async Task<List<List<Transaction>>> ScheduleTransactions(Dictionary<Hash, List<Transaction>> txDict)
        {
            throw new NotImplementedException();
        }
    }
}