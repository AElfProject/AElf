using System;
using System.Collections.Generic;
using AElf.Kernel.Concurrency;
using Xunit;
using Xunit.Sdk;

namespace AElf.Kernel.Tests.Concurrency
{
    public class BatchTest
    {
        private ParallelTestDataUtil _dataUtil = new ParallelTestDataUtil();
        public List<ITransaction> GetTestData()
        {
            var txList = _dataUtil.GetFirstBatchTxList();
            
            return txList;
        }


        [Fact]
        public void TestBatch()
        {
            var batch = new Batch();
            var txList = GetTestData();
            foreach (var tx in txList)
            {
                batch.AddTransaction(tx);
            }
            
            //Test unique sender property of batch
            Assert.Throws<Exception>(() => batch.AddTransaction(_dataUtil.NewTransaction(0, 10)));
            
            List<ITransaction> testJob1 = new List<ITransaction>();
            testJob1.Add(txList[0]);
            testJob1.Add(txList[1]);
            testJob1.Add(txList[7]);
            
            List<ITransaction> testJob2 = new List<ITransaction>();
            testJob2.Add(txList[2]);
            testJob2.Add(txList[6]);
            
            List<ITransaction> testJob3 = new List<ITransaction>();
            testJob3.Add(txList[3]);
            testJob3.Add(txList[5]);
            testJob3.Add(txList[8]);
            testJob3.Add(txList[9]);
            testJob3.Add(txList[10]);
            
            List<ITransaction> testJob4 = new List<ITransaction>();
            testJob4.Add(txList[4]);
            testJob4.Add(txList[11]);
            
            Assert.Equal(4, batch.Jobs.Count);
            
            Assert.Collection(batch.Jobs,
                job1 =>
                {
                    Assert.Equal(3, job1.Count);
                    for (int i = 0; i < job1.Count; i++)
                    {
                        Assert.Equal(testJob1[i], job1.TxList[i]);
                    }
                },
                job2 =>
                {
                    Assert.Equal(2, job2.Count);
                    for (int i = 0; i < job2.Count; i++)
                    {
                        Assert.Equal(testJob2[i], job2.TxList[i]);
                    }
                },
                job3 =>
                {
                    Assert.Equal(5, job3.Count);
                    for (int i = 0; i < job3.Count; i++)
                    {
                        Assert.Equal(testJob3[i], job3.TxList[i]);
                    }
                },
                job4 =>
                {
                    Assert.Equal(2, job4.Count);
                    for (int i = 0; i < job4.Count; i++)
                    {
                        Assert.Equal(testJob4[i], job4.TxList[i]);
                    }
                }
                );
        }
    }
}