//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using AElf.Kernel.SmartContractExecution.Scheduling;
//using Xunit;
//using Google.Protobuf;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    /*
    public class GrouperTest
    {
        public List<Address> _accountList = new List<Address>();
        private ParallelTestDataUtil _dataUtil = new ParallelTestDataUtil();

        public Dictionary<Address, List<Transaction>> GetTestData()
        {
            Dictionary<Address, List<Transaction>> txList = new Dictionary<Address, List<Transaction>>();


            for (int i = 0; i < 12; i++)
            {
                _accountList.Add(Address.Generate());
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

        private void GetTransactionReadyInList(Dictionary<Address, List<Transaction>> txList, int from, int to)
        {
            var tx = GetTransaction(from, to);
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

        public Transaction GetTransaction(int from, int to)
        {
            var tx = new Transaction();
            tx.From = _accountList[from];
            tx.To = _accountList[to];
            return tx;
        }

        [Fact]
        public async Task MergeByAccountTest()
        {
            var txDic = GetTestData();
            Grouper grouper = new Grouper(new MockResourceUsageDetectionService());
            var grouped = (await grouper.ProcessNaive(ChainHelpers.GetChainId(123), txDic.Values.SelectMany(x => x).ToList()))
            .Item1;
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
        public async Task MergeByAccountTestFullTxList()
        {
            var txList = _dataUtil.GetFullTxList();
            Grouper grouper = new Grouper(new MockResourceUsageDetectionService());
            var grouped = (await grouper.ProcessNaive(ChainHelpers.GetChainId(123), txList.Select(x => x).ToList())).Item1;
            var s = grouped.Select(
                x => _dataUtil.StringRepresentation(x)
            ).ToList();

            Assert.Equal(_dataUtil.StringRepresentation(_dataUtil.GetFirstGroupTxList().Select(x => x).ToList()), s[0]);
            Assert.Equal(_dataUtil.StringRepresentation(_dataUtil.GetSecondGroupTxList().Select(x => x).ToList()), s[1]);
        }

        [Fact]
        public async Task TestReblancedGrouping_MaxAddMins()
        {
            Grouper grouper = new Grouper(new MockResourceUsageDetectionService());

            var testCasesCount = 4;
            var coreCountList = new[] {7, 10, 1, 5, 100, 1000, 5, 3};
            var testCaseSizesList = new List<List<int>>(new []
            {
                new List<int>(){100, 20, 30, 1, 2, 4, 5, 1, 50, 70, 90}, //normal cases
                new List<int>(){1000}, // test a single giant group with multiple cores
                new List<int>(){1,1,1,1,1,100,12,13,1}, //test one core
                new List<int>(){10, 10, 10, 10, 10, 10, 9, 11, 20}, //normal cases
                new List<int>(), //test empty tx list
                new List<int>(){10, 20, 10, 4, 5, 12, 51, 25, 31}, //test when core is far bigger
                new List<int>(){20, 20, 20, 20, 20}, //test when nothing changes needed
                
                new List<int>(){499, 2, 497, 2, 496, 3, 496, 6}, //test worst case
                
            });
            var expectedSizesList = new List<List<int>>(new []
            {
                new List<int>(){100, 90, 70, 52, 41, 20}, 
                new List<int>(){1000},
                new List<int>(){131},
                new List<int>(){20, 20, 20, 20, 20}, 
                new List<int>(), 
                new List<int>(){10, 20, 10, 4, 5, 12, 51, 25, 31}, 
                new List<int>(){20, 20, 20, 20, 20},
                new List<int>(){1002, 499, 499}, 
            });
            

            for (int i = 0; i < testCasesCount; i++)
            {
                var unmergedGroup = ProduceFakeTxGroup(testCaseSizesList[i]);
                var txList = new List<Transaction>();
                unmergedGroup.ForEach(a => txList.AddRange(a));
                var actualRes = (await grouper.ProcessWithCoreCount(GroupStrategy.Limited_MaxAddMins, 
                coreCountList[i], ChainHelpers.GetChainId(123), txList)).Item1;
                var acutalSizes = actualRes.Select(a => a.Count).ToList();
                Assert.Equal(expectedSizesList[i].OrderBy(a=>a), acutalSizes.OrderBy(a=>a));
            }
            
        }
        
        [Fact]
        public async Task TestReblancedGrouping_MinsAddUp()
        {
            Grouper grouper = new Grouper(new MockResourceUsageDetectionService());

            var testCasesCount = 4;
            var coreCountList = new[] {7, 10, 1, 5, 100, 1000, 5, 3};
            var testCaseSizesList = new List<List<int>>(new []
            {
                new List<int>(){100, 20, 30, 1, 2, 4, 5, 1, 50, 70, 90}, //normal cases
                new List<int>(){1000}, // test a single giant group with multiple cores
                new List<int>(){1,1,1,1,1,100,12,13,1}, //test one core
                new List<int>(){10, 10, 10, 10, 10, 10, 9, 11, 20}, //normal cases
                new List<int>(), //test empty tx list
                new List<int>(){10, 20, 10, 4, 5, 12, 51, 25, 31}, //test when core is far bigger
                new List<int>(){20, 20, 20, 20, 20}, //test when nothing changes needed
                new List<int>(){499, 2, 497, 2, 496, 3, 496, 6}, //test worst case
                
                //test for sorted insert
                new List<int>(){3, 5, 7, 9, 1, 1}, //insert at first
                new List<int>(){3, 5, 7, 9, 2, 2}, //insert in middle
                new List<int>(){3, 5, 7, 9, 3, 3}, //insert in middle
                new List<int>(){5, 5, 7, 9, 5, 5}, //insert at last
            });
            var expectedSizesList = new List<List<int>>(new []
            {
                new List<int>(){100, 90, 70, 50, 30, 20, 13}, 
                new List<int>(){1000},
                new List<int>(){131},
                new List<int>(){21, 20, 20, 20, 19}, 
                new List<int>(), 
                new List<int>(){10, 20, 10, 4, 5, 12, 51, 25, 31}, 
                new List<int>(){20, 20, 20, 20, 20},
                new List<int>(){993, 509, 499}, 
                
                new List<int>(){2, 3, 5, 7, 9}, //insert at first
                new List<int>(){3, 4, 5, 7, 9}, //insert in middle
                new List<int>(){3, 5, 6, 7, 9}, //insert in middle
                new List<int>(){5, 5, 7, 9, 10}, //insert at last
            });
            

            for (int i = 0; i < testCasesCount; i++)
            {
                var unmergedGroup = ProduceFakeTxGroup(testCaseSizesList[i]);
                var txList = new List<Transaction>();
                unmergedGroup.ForEach(a => txList.AddRange(a));
                var actualRes = (await grouper.ProcessWithCoreCount(GroupStrategy.Limited_MinsAddUp, 
                coreCountList[i], ChainHelpers.GetChainId(123), txList)).Item1;
                var acutalSizes = actualRes.Select(a => a.Count).ToList();
                Assert.Equal(expectedSizesList[i].OrderBy(a=>a), acutalSizes.OrderBy(a=>a));
            }
            
        }

        public List<List<Transaction>> ProduceFakeTxGroup(List<int> groupSizes)
        {
            int userId = 0;
            var res = new List<List<Transaction>>();
            foreach (var size in groupSizes)
            {
                var txGroup = new List<Transaction>();
                for (int i = 0; i < size; i++)
                {
                    txGroup.Add(new Transaction()
                    {
                        From = Address.FromString(userId++.ToString()),
                        To = Address.FromString(userId.ToString())
                    });
                }
                res.Add(txGroup);
                userId++;
            }

            return res;
        }
    }
    */
}