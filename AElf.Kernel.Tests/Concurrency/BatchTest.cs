using System;
using System.Collections.Generic;
using AElf.Kernel.Concurrency;
using Xunit;
using Xunit.Sdk;

namespace AElf.Kernel.Tests.Concurrency
{
    public class BatchTest
    {
        public List<Hash> _accountList = new List<Hash>();
        public List<ITransaction> GetTestData()
        {
            var txList = new List<ITransaction>();
            
            for (int i = 0; i < 17; i++)
            {
                _accountList.Add(Hash.Generate());
                //0    1    2    3    4    5    6    7    8    9    10
                //A    B    C    D    E    F    G    H    I    J    K
                
                //11   12   13   14   15   16
                //L    M    N    O    P    Q
            }

            //build txs that is the first batch of test case in ParallelGroupTest.cs
            AddTxInList(txList, 0, 1);        //0: A -> B    //group1
            AddTxInList(txList, 1, 5);        //1: B -> F    //group1
            AddTxInList(txList, 2, 3);        //2: C -> D    //group2
            AddTxInList(txList, 8, 9);        //3: I -> J    //group3
            AddTxInList(txList, 16, 15);      //4: Q -> P    //group4
            AddTxInList(txList, 9, 11);       //5: J -> L    //group3
            AddTxInList(txList, 3, 4);        //6: D -> E    //group2
            AddTxInList(txList, 7, 5);        //7: H -> F    //group1
            AddTxInList(txList, 10, 9);       //8: K -> J    //group3    this J, K, L form a circle
            AddTxInList(txList, 11, 10);      //9: L -> k    //group3
            AddTxInList(txList, 12, 11);      //10: M -> L    //group3
            AddTxInList(txList, 14, 15);      //11: O -> P    //group4
            
            return txList;
        }

        public void AddTxInList(List<ITransaction> txList, int from, int to)
        {
            var tx = NewTransaction(from, to);
            txList.Add(tx);
        }

        public Transaction NewTransaction(int from, int to)
        {
            var tx = new Transaction();
            tx.From = _accountList[from];
            tx.To = _accountList[to];
            return tx;
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
            Assert.Throws<Exception>(() => batch.AddTransaction(NewTransaction(0, 10)));
            
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