using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TxMemPool;
using QuickGraph;


namespace AElf.Kernel.Concurrency
{
    public class Scheduler : IScheduler
    {
        private ITxPoolService _txPoolService;
        protected Grouper _grouper;

        public Scheduler(ITxPoolService txPoolService, Grouper grouper)
        {
            _txPoolService = txPoolService;
            _grouper = grouper;
        }

        public async Task<List<List<Transaction>>> ScheduleTransactions()
        {
            var txList = await _txPoolService.GetReadyTxsAsync();
            return _grouper.ProduceGroup(txList);
        }
    }
}