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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txList"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<IParallelGroup> ScheduleTransactions(List<ITransaction> txList)
        {
            //TODO: Because not sure about whether it's convenient to use txDict when grouping by metadata
            //for now the this method takes List<ITransaction> and Scheduler convert this list to Dictionary<Hash, List<ITransaction>>
            var txDict = ConvertTxListIntoTxDict(txList);
            return _parallelGroupService.ProduceGroup(txDict);
        }

        private Dictionary<Hash, List<ITransaction>> ConvertTxListIntoTxDict(List<ITransaction> txList)
        {
            var txDict = new Dictionary<Hash, List<ITransaction>>();
            foreach (var tx in txList)
            {
                if (!txDict.TryGetValue(tx.From, out var accountTxList))
                {
                    accountTxList = new List<ITransaction>();
                    txDict.Add(tx.From, accountTxList);
                }
                accountTxList.Add(tx);
            }

            return txDict;
        }
    }
}