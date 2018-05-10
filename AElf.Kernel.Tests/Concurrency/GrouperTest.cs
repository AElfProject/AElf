using System.Collections.Generic;
using System.Text.RegularExpressions;
using AElf.Kernel.Concurrency;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency
{
    public class GrouperTest
    {
        public List<Transaction> GetTestData()
        {
            List<Transaction> txList = new List<Transaction>();
            
            List<Hash> accountList = new List<Hash>();
            for (int i = 0; i < 12; i++)
            {
                accountList.Add(Hash.Generate());
            }

            GetTransactionReadyInList(txList, accountList, 0, 1);
            GetTransactionReadyInList(txList, accountList, 2, 1);
            GetTransactionReadyInList(txList, accountList, 3, 2);
            GetTransactionReadyInList(txList, accountList, 2, 4);
            GetTransactionReadyInList(txList, accountList, 4, 5);
            GetTransactionReadyInList(txList, accountList, 6, 7);
            GetTransactionReadyInList(txList, accountList, 8, 7);
            GetTransactionReadyInList(txList, accountList, 9, 10);
            GetTransactionReadyInList(txList, accountList, 10, 11);

            return txList;
        }

        public void GetTransactionReadyInList(List<Transaction> txList, List<Hash> accountList, int from, int to)
        {
            txList.Add(GetTransaction(accountList, from, to));
        }

        public Transaction GetTransaction(List<Hash> accountList, int from, int to)
        {
            var tx = new Transaction();
            tx.From = accountList[from];
            tx.To = accountList[to];
            return tx;
        }

        [Fact]
        public void MergeByAccountTest()
        {
            List<Transaction> txList = GetTestData();
            Grouper grouper = new Grouper();

            var groups = grouper.MergeByAccount(txList);
            
            Assert.Equal(3, groups.Count);
            Assert.Equal(5, groups[0].Count);
            Assert.Equal(2, groups[1].Count);
            Assert.Equal(2, groups[2].Count);

            Assert.True(groups[0].Contains(txList[0]));
            Assert.True(groups[0].Contains(txList[1]));
            Assert.True(groups[0].Contains(txList[2]));
            Assert.True(groups[0].Contains(txList[3]));
            Assert.True(groups[0].Contains(txList[4]));
            
            Assert.True(groups[1].Contains(txList[5]));
            Assert.True(groups[1].Contains(txList[6]));
            
            Assert.True(groups[2].Contains(txList[7]));
            Assert.True(groups[2].Contains(txList[8]));
        }
    }
}