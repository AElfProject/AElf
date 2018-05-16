using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Concurrency;
using Castle.Components.DictionaryAdapter;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency
{
    public class ParallelGroupTest
    {
        private ParallelTestDataUtil _dataUtil = new ParallelTestDataUtil();

        [Fact]
        public void TestParallelGroup()
        {
            var txDict = _dataUtil.GetFirstGroupTxDict(out List<int[]> expectedJobSizeOfEachJobInBatch);
            var group = new ParallelGroup();
            foreach (var pair in txDict)
            {
                group.AddAccountTxList(pair);
            }
            
            Assert.Equal(4, group.Batches.Count);
            
            
            for (int i = 0; i < group.Batches.Count; i++)
            {
                Assert.Equal(expectedJobSizeOfEachJobInBatch[i].Length, group.Batches[i].Jobs.Count);
                for (int j = 0; j < group.Batches[i].Jobs.Count; j++)
                {
                    Assert.Equal(expectedJobSizeOfEachJobInBatch[i][j], group.Batches[i].Jobs[j].Count);
                }
            }

            //We already test the function of spliting the job in the batch,
            //so we just collect all the tx in the batch in a same List<ITransaction> and see if transaction are contained as expected
            List<List<ITransaction>> txListInBatches = new List<List<ITransaction>>();
            for (int i = 0; i < group.Batches.Count; i++)
            {
                List<ITransaction> list = new List<ITransaction>();
                foreach (var job in group.Batches[i].Jobs)
                {
                    list.AddRange(job.TxList);
                }
                txListInBatches.Add(list);
            }
            foreach (var pair in txDict)
            {
                var txAccountList = pair.Value;
                for (int batchIndex = 0; batchIndex < txAccountList.Count; batchIndex++)
                {
                    Assert.Contains(txAccountList[batchIndex], txListInBatches[batchIndex]);
                }
            }
        }
    }
}