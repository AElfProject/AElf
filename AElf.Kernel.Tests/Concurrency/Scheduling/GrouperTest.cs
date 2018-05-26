using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AElf.Kernel.Concurrency.Scheduling;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class GrouperTest
    {
        public List<Hash> _accountList = new List<Hash>();
        private ParallelTestDataUtil _dataUtil = new ParallelTestDataUtil();
        
        public Dictionary<Hash, List<ITransaction>> GetTestData()
        {
            Dictionary<Hash, List<ITransaction>> txList = new Dictionary<Hash, List<ITransaction>>();


            for (int i = 0; i < 12; i++)
            {
                _accountList.Add(Hash.Generate());
            }

            GetTransactionReadyInList(txList, 0, 1);
            GetTransactionReadyInList(txList, 2, 1);
            GetTransactionReadyInList(txList, 3, 2);
            GetTransactionReadyInList(txList, 2, 4);
            GetTransactionReadyInList(txList, 4, 5);
            GetTransactionReadyInList(txList, 6, 7);
            GetTransactionReadyInList(txList, 8, 7);
            GetTransactionReadyInList(txList, 9, 10);
            GetTransactionReadyInList(txList, 10, 11);

            return txList;
        }

        public void GetTransactionReadyInList(Dictionary<Hash, List<ITransaction>> txList, int from, int to)
        {
            var tx = GetTransaction(from, to);
            if (txList.ContainsKey(tx.From))
            {
                txList[tx.From].Add(tx);
            }
            else
            {
                var accountTxList = new List<ITransaction>();
                accountTxList.Add(tx);
                txList.Add(tx.From, accountTxList);
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
        public void MergeByAccountTest()
        {
            var txDic = GetTestData();
            Grouper grouper = new Grouper();
            var grouped = grouper.Process(txDic.Values.SelectMany(x => x).ToList());
            var s = grouped.Select(
                x =>
                String.Join(" ", x.OrderBy(y => _accountList.IndexOf(y.From)).ThenBy(z => _accountList.IndexOf(z.To)).Select(
                    y => String.Format("({0}-{1})", _accountList.IndexOf(y.From), _accountList.IndexOf(y.To))
                ))
            ).OrderBy(b => b).ToList();

            //group 1: {0-1}; {2-1, 2-4}; {3-2}; {4-5}
            Assert.Equal("(0-1) (2-1) (2-4) (3-2) (4-5)", s[0]);

            //group 2: {6-7}; {8-7}
            Assert.Equal("(6-7) (8-7)", s[1]);

            //group 3: {9-10}; {10-11}
            Assert.Equal("(9-10) (10-11)", s[2]);
        }

        [Fact]
        public void MergeByAccountTestFullTxList()
        {
            var txList = _dataUtil.GetFullTxList();
            Grouper grouper = new Grouper();
            var grouped = grouper.Process(txList.Select(x => x).ToList());
            var s = grouped.Select(
                x => _dataUtil.StringRepresentation(x)
            ).ToList();
            
            Assert.Equal(_dataUtil.StringRepresentation(_dataUtil.GetFirstGroupTxList().Select(x => x).ToList()), s[0]);
            Assert.Equal(_dataUtil.StringRepresentation(_dataUtil.GetSecondGroupTxList().Select(x => x).ToList()), s[1]);
        }
    }
}