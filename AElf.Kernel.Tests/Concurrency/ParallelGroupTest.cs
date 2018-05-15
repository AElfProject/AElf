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
        public List<Hash> _accountList = new List<Hash>();
        public Dictionary<Hash, List<ITransaction>> GetTestData()
        {
            Dictionary<Hash, List<ITransaction>> txDict = new Dictionary<Hash, List<ITransaction>>();
            
           
            for (int i = 0; i < 17; i++)
            {
                _accountList.Add(Hash.Generate());
                //0    1    2    3    4    5    6    7    8    9    10
                //A    B    C    D    E    F    G    H    I    J    K
                
                //11   12   13   14   15   16
                //L    M    N    O    P    Q
            }

            //Build txs that belong to same group
            GetTransactionReadyInList(txDict, 0, 1);        //A -> B
            GetTransactionReadyInList(txDict, 0, 5);        //A -> F
            GetTransactionReadyInList(txDict, 0, 4);        //A -> E
            GetTransactionReadyInList(txDict, 1, 5);        //B -> F   
            GetTransactionReadyInList(txDict, 1, 6);        //B -> G
            GetTransactionReadyInList(txDict, 1, 7);        //B -> H
            GetTransactionReadyInList(txDict, 1, 2);        //B -> C
            GetTransactionReadyInList(txDict, 2, 3);        //C -> D
            GetTransactionReadyInList(txDict, 3, 4);        //D -> E
            GetTransactionReadyInList(txDict, 7, 5);        //H -> F
            GetTransactionReadyInList(txDict, 8, 9);        //I -> J    
            GetTransactionReadyInList(txDict, 10, 9);       //K -> J
            GetTransactionReadyInList(txDict, 9, 11);       //J -> L
            GetTransactionReadyInList(txDict, 9, 3);        //J -> D
            GetTransactionReadyInList(txDict, 11, 10);      //L -> k
            GetTransactionReadyInList(txDict, 11, 14);      //L -> O
            GetTransactionReadyInList(txDict, 12, 11);      //M -> L
            GetTransactionReadyInList(txDict, 12, 13);      //M -> N
            GetTransactionReadyInList(txDict, 14, 15);      //O -> P
            GetTransactionReadyInList(txDict, 14, 16);      //O -> Q
            GetTransactionReadyInList(txDict, 16, 15);      //Q -> P

            return txDict;
        }

        public void GetTransactionReadyInList(Dictionary<Hash, List<ITransaction>> txDict, int from, int to)
        {
            var tx = GetTransaction(from, to);
            if (txDict.ContainsKey(tx.From))
            {
                txDict[tx.From].Add(tx);
            }
            else
            {
                var accountTxList = new List<ITransaction>();
                accountTxList.Add(tx);
                txDict.Add(tx.From, accountTxList);
            }
        }

        public Transaction GetTransaction(int from, int to)
        {
            var tx = new Transaction();
            tx.From = _accountList[from];
            tx.To = _accountList[to];
            return tx;
        }
        
        [Fact]
        public void TestParallelGroup()
        {
            var txDict = GetTestData();
            var group = new ParallelGroup();
            foreach (var pair in txDict)
            {
                group.AddAccountTxList(pair);
            }
            
            Assert.Equal(4, group.Batches.Count);
            
            List<int[]> expectedJobSizeOfEachJobInBatch = new EditableList<int[]>();
            expectedJobSizeOfEachJobInBatch.Add(new int[] {3, 2, 5, 2});    //{A - > B -> F <- H}, {C -> D -> E}, {I ->J <- K, J -> L -> K, M -> L}, {O->P, Q->P}
            expectedJobSizeOfEachJobInBatch.Add(new int[] {1, 1, 1, 2, 1}); //{A->F}, {B->G}, {J->D}, {L ->O ->Q}, {M->N}
            expectedJobSizeOfEachJobInBatch.Add(new int[] {1, 1});          //{A->E}, {B->H}
            expectedJobSizeOfEachJobInBatch.Add(new int[] {1});             //{B->C}

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