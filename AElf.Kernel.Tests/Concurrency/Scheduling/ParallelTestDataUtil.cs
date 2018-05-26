using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    /// <summary>
    /// This class putting test data for parallel altogather
    /// </summary>
    public class ParallelTestDataUtil
    {
        public List<Hash> AccountList { get; } = new List<Hash>();

        public ParallelTestDataUtil()
        {
            for (int i = 0; i < 26; i++)
            {
                AccountList.Add(Hash.Generate());
                //0    1    2    3    4    5    6    7    8    9    10
                //A    B    C    D    E    F    G    H    I    J    K
                
                //11   12   13   14   15   16   17   18   19   20   21
                //L    M    N    O    P    Q    R    S    T    U    V
                
                //22   23   24   25   
                //W    X    Y    Z
            }
        }
        
        public List<ITransaction> GetFullTxList()
        {
            var txList1 = GetFirstGroupTxList();
            var txList2 = GetSecondGroupTxList();

            for (int i = 0, x = 1; i < txList2.Count; i++, x+=3)
            {
                txList1.Insert(i + x,txList2[i]);
            }

            return txList1;
        }

        
        public Dictionary<Hash, List<ITransaction>> GetFirstGroupTxDict(out List<int[]> expectedJobSizeOfEachJobInBatch)
        {
            var txList = GetFirstGroupTxList();
            var txDict = ConvertTxListIntoTxDict(txList);
            
            expectedJobSizeOfEachJobInBatch = new List<int[]>();
            expectedJobSizeOfEachJobInBatch.Add(new int[] {3, 2, 5, 2});    //{A - > B -> F <- H}, {C -> D -> E}, {I ->J <- K, J -> L -> K, M -> L}, {O->P, Q->P}
            expectedJobSizeOfEachJobInBatch.Add(new int[] {1, 1, 1, 2, 1}); //{A->F}, {B->G}, {J->D}, {L ->O ->Q}, {M->N}
            expectedJobSizeOfEachJobInBatch.Add(new int[] {1, 1});          //{A->E}, {B->H}
            expectedJobSizeOfEachJobInBatch.Add(new int[] {1});             //{B->C}

            return txDict;
        }
        
        public List<ITransaction> GetFirstGroupTxList()
        {
            var txList = new List<ITransaction>();
            //Build txs that belong to same group
            AddTxInList(txList, 0, 1);        //A -> B
            AddTxInList(txList, 0, 5);        //A -> F
            AddTxInList(txList, 0, 4);        //A -> E
            AddTxInList(txList, 1, 5);        //B -> F   
            AddTxInList(txList, 1, 6);        //B -> G
            AddTxInList(txList, 1, 7);        //B -> H
            AddTxInList(txList, 1, 2);        //B -> C
            AddTxInList(txList, 2, 3);        //C -> D
            AddTxInList(txList, 3, 4);        //D -> E
            AddTxInList(txList, 7, 5);        //H -> F
            AddTxInList(txList, 8, 9);        //I -> J    
            AddTxInList(txList, 10, 9);       //K -> J
            AddTxInList(txList, 9, 11);       //J -> L
            AddTxInList(txList, 9, 3);        //J -> D
            AddTxInList(txList, 11, 10);      //L -> k
            AddTxInList(txList, 11, 14);      //L -> O
            AddTxInList(txList, 12, 11);      //M -> L
            AddTxInList(txList, 12, 13);      //M -> N
            AddTxInList(txList, 14, 15);      //O -> P
            AddTxInList(txList, 14, 16);      //O -> Q
            AddTxInList(txList, 16, 15);      //Q -> P
            
            return txList;
        }
        
        
        public List<ITransaction> GetSecondGroupTxList()
        {
            var txList = new List<ITransaction>();
            //Build txs that belong to same group
            AddTxInList(txList, 17, 18);        //R -> S
            AddTxInList(txList, 19, 18);        //T -> S
            
            
            return txList;
        }


        public List<ITransaction> GetFirstBatchTxList()
        {
            var txList = new List<ITransaction>();
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

        public List<ITransaction> GetJobTxListInFirstBatch(int jobIndex)
        {
            var txList = new List<ITransaction>();
            switch (jobIndex)
            {
                    case 0: 
                        //{A - > B -> F <- H}
                        AddTxInList(txList, 0, 1);
                        AddTxInList(txList, 1, 5);
                        AddTxInList(txList, 7, 5);
                        break;
                    case 1:
                        //{C -> D -> E}
                        AddTxInList(txList, 2, 3);
                        AddTxInList(txList, 3, 4);
                        break;
                    case 2:
                        //{I ->J <- K, J -> L -> K, M -> L}
                        AddTxInList(txList, 8, 9);
                        AddTxInList(txList, 10, 9);
                        AddTxInList(txList, 9, 11);
                        AddTxInList(txList, 11, 10);
                        AddTxInList(txList, 12, 11);
                        break;
                    case 3:
                        //{O->P, Q->P}
                        AddTxInList(txList, 14, 15);
                        AddTxInList(txList, 16, 15);
                        break;
            }

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
            tx.From = AccountList[from];
            tx.To = AccountList[to];
            return tx;
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
        
        public string StringRepresentation(List<ITransaction> l)
        {
            return String.Join(
                " ",
                l.OrderBy(y => AccountList.IndexOf(y.From))
                    .ThenBy(z => AccountList.IndexOf(z.To))
                    .Select(
                        y => String.Format("({0}-{1})", AccountList.IndexOf(y.From), AccountList.IndexOf(y.To))
                    ));
        }
    }
}