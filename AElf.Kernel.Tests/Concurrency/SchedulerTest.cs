using System.Collections.Generic;
using AElf.Kernel.Concurrency;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency
{
    public class SchedulerTest
    {
        private ParallelTestDataUtil _dataUtil = new ParallelTestDataUtil();
        
        [Fact]
        public void TestScheduleTransactionsWithJustOneGroup()
        {
            var txList = _dataUtil.GetFullTxList();
            Scheduler scheduler = new Scheduler(new ParallelGroupService());
            var parallelGroupList = scheduler.ScheduleTransactions(txList);
            
            Assert.Equal(2, parallelGroupList.Count);
            
        }
    }
}