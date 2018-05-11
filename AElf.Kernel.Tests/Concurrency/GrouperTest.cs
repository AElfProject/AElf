using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AElf.Kernel.Concurrency;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency
{
    public class GrouperTest
    {
        public List<Hash> _accountList = new List<Hash>();
        public Dictionary<Hash, List<Transaction>> GetTestData()
        {
            Dictionary<Hash, List<Transaction>> txList = new Dictionary<Hash, List<Transaction>>();
            
           
            for (int i = 0; i < 12; i++)
            {
                _accountList.Add(Hash.Generate());
            }

            GetTransactionReadyInList(txList, _accountList, 0, 1);
            GetTransactionReadyInList(txList, _accountList, 2, 1);
            GetTransactionReadyInList(txList, _accountList, 3, 2);
            GetTransactionReadyInList(txList, _accountList, 2, 4);
            GetTransactionReadyInList(txList, _accountList, 4, 5);
            GetTransactionReadyInList(txList, _accountList, 6, 7);
            GetTransactionReadyInList(txList, _accountList, 8, 7);
            GetTransactionReadyInList(txList, _accountList, 9, 10);
            GetTransactionReadyInList(txList, _accountList, 10, 11);

            return txList;
        }

        public void GetTransactionReadyInList(Dictionary<Hash, List<Transaction>> txList, List<Hash> _accountList, int from, int to)
        {
            var tx = GetTransaction(_accountList, from, to);
            if (txList.ContainsKey(tx.From))
            {
                txList[tx.From].Add(tx);
            }
            else
            {
                var accountTxList = new List<Transaction>();
                accountTxList.Add(tx);
                txList.Add(tx.From, accountTxList);
            }
        }

        public Transaction GetTransaction(List<Hash> _accountList, int from, int to)
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

            var groups = grouper.MergeAccountTxList(txDic);
            
            //3 group
            Assert.Equal(3, groups.Count);
            //group 1: {0-1}; {2-1, 2-4}; {3-2}; {4-5}
            Assert.Equal(4, groups[0].Count);
            Assert.Equal(1, groups[0].GetAccountTxList(_accountList[0]).Count);
            Assert.Equal(2, groups[0].GetAccountTxList(_accountList[2]).Count);
            Assert.Equal(1, groups[0].GetAccountTxList(_accountList[3]).Count);
            Assert.Equal(1, groups[0].GetAccountTxList(_accountList[4]).Count);
            
            Assert.True(groups[0].GetAccountTxList(_accountList[0]).Contains(txDic[_accountList[0]][0]));
            Assert.True(groups[0].GetAccountTxList(_accountList[2]).Contains(txDic[_accountList[2]][0]));
            Assert.True(groups[0].GetAccountTxList(_accountList[2]).Contains(txDic[_accountList[2]][1]));
            Assert.True(groups[0].GetAccountTxList(_accountList[3]).Contains(txDic[_accountList[3]][0]));
            Assert.True(groups[0].GetAccountTxList(_accountList[4]).Contains(txDic[_accountList[4]][0]));
            
            //group 2: {6-7}; {8-7}
            Assert.Equal(2, groups[1].Count);
            Assert.Equal(1, groups[1].GetAccountTxList(_accountList[6]).Count);
            Assert.Equal(1, groups[1].GetAccountTxList(_accountList[8]).Count);
            Assert.True(groups[1].GetAccountTxList(_accountList[6]).Contains(txDic[_accountList[6]][0]));
            Assert.True(groups[1].GetAccountTxList(_accountList[8]).Contains(txDic[_accountList[8]][0]));
            //group 3: {9-10}; {10-11}
            Assert.Equal(2, groups[2].Count);
            Assert.Equal(1, groups[2].GetAccountTxList(_accountList[9]).Count);
            Assert.Equal(1, groups[2].GetAccountTxList(_accountList[10]).Count);
            Assert.True(groups[2].GetAccountTxList(_accountList[9]).Contains(txDic[_accountList[9]][0]));
            Assert.True(groups[2].GetAccountTxList(_accountList[10]).Contains(txDic[_accountList[10]][0]));
        }
    }
}