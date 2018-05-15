using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TxMemPool;
using QuickGraph;


namespace AElf.Kernel.Concurrency
{
    public class Scheduler : IScheduler
    {
        private readonly IParallelGroupService _parallelGroupService;

        public Scheduler(IParallelGroupService parallelGroupService)
        {
            _parallelGroupService = parallelGroupService;
        }

        public Task<List<List<ITransaction>>> ScheduleTransactions(Dictionary<Hash, List<ITransaction>> txDict)
        {
            var groupResult = _parallelGroupService.ProduceGroup(txDict);
            throw new NotImplementedException();
        }
    }
}