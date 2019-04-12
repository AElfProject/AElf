using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Scheduling
{
    public class GrouperTests : SmartContractExecutionGrouperTestBase
    {
        private readonly Grouper _grouper;

        private int _chainId = 1;
        private List<Address> _accountList = new List<Address>();

        public GrouperTests()
        {
            _grouper = GetRequiredService<Grouper>();

            for (var i = 0; i < 30; i++)
            {
                _accountList.Add(Address.Generate());
            }
        }

        [Fact]
        public async Task Process_Transaction_ByAccount()
        {
            var txList = GetTestData();
            var grouped = (await _grouper.ProcessNaive(_chainId, txList)).Item1;
            var s = grouped.Select(
                StringRepresentation
            ).ToList();

            //group 1: {0-1}; {2-1, 2-4}; {3-2}; {4-5}
            Assert.Equal("(0-1) (2-1) (2-4) (3-2) (4-5)", s[0]);

            //group 2: {6-7}; {8-7}
            Assert.Equal("(6-7) (8-7)", s[1]);

            //group 3: {9-10}; {10-11}
            Assert.Equal("(9-10) (10-11)", s[2]);
        }

        [Fact]
        public async Task Process_Transaction_ByAccount_TwoGroup()
        {
            var txList = GetFullTxList();
            var grouped = (await _grouper.ProcessNaive(_chainId, txList.Select(x => x).ToList())).Item1;
            var s = grouped.Select(
                StringRepresentation
            ).ToList();

            Assert.Equal(StringRepresentation(GetFirstGroupTxList().Select(x => x).ToList()), s[0]);
            Assert.Equal(StringRepresentation(GetSecondGroupTxList().Select(x => x).ToList()), s[1]);
        }

        [Fact]
        public async Task Process_Transaction_MaxAddMins()
        {
            var testCasesCount = 4;
            var coreCountList = new[] {7, 10, 1, 5, 100, 1000, 5, 3};
            var testCaseSizesList = new List<List<int>>(new[]
            {
                new List<int>() {100, 20, 30, 1, 2, 4, 5, 1, 50, 70, 90}, //normal cases
                new List<int>() {1000}, // test a single giant group with multiple cores
                new List<int>() {1, 1, 1, 1, 1, 100, 12, 13, 1}, //test one core
                new List<int>() {10, 10, 10, 10, 10, 10, 9, 11, 20}, //normal cases
                new List<int>(), //test empty tx list
                new List<int>() {10, 20, 10, 4, 5, 12, 51, 25, 31}, //test when core is far bigger
                new List<int>() {20, 20, 20, 20, 20}, //test when nothing changes needed

                new List<int>() {499, 2, 497, 2, 496, 3, 496, 6}, //test worst case

            });
            var expectedSizesList = new List<List<int>>(new[]
            {
                new List<int>() {100, 90, 70, 52, 41, 20},
                new List<int>() {1000},
                new List<int>() {131},
                new List<int>() {20, 20, 20, 20, 20},
                new List<int>(),
                new List<int>() {10, 20, 10, 4, 5, 12, 51, 25, 31},
                new List<int>() {20, 20, 20, 20, 20},
                new List<int>() {1002, 499, 499},
            });


            for (int i = 0; i < testCasesCount; i++)
            {
                var unmergedGroup = ProduceFakeTxGroup(testCaseSizesList[i]);
                var txList = new List<Transaction>();
                unmergedGroup.ForEach(a => txList.AddRange(a));
                var actualRes = (await _grouper.ProcessWithCoreCount(GroupStrategy.Limited_MaxAddMins,
                    coreCountList[i], _chainId, txList)).Item1;
                var acutalSizes = actualRes.Select(a => a.Count).ToList();
                Assert.Equal(expectedSizesList[i].OrderBy(a => a), acutalSizes.OrderBy(a => a));
            }

        }

        [Fact]
        public async Task Process_Transaction_MinsAddUp()
        {
            var testCasesCount = 4;
            var coreCountList = new[] {7, 10, 1, 5, 100, 1000, 5, 3};
            var testCaseSizesList = new List<List<int>>(new[]
            {
                new List<int>() {100, 20, 30, 1, 2, 4, 5, 1, 50, 70, 90}, //normal cases
                new List<int>() {1000}, // test a single giant group with multiple cores
                new List<int>() {1, 1, 1, 1, 1, 100, 12, 13, 1}, //test one core
                new List<int>() {10, 10, 10, 10, 10, 10, 9, 11, 20}, //normal cases
                new List<int>(), //test empty tx list
                new List<int>() {10, 20, 10, 4, 5, 12, 51, 25, 31}, //test when core is far bigger
                new List<int>() {20, 20, 20, 20, 20}, //test when nothing changes needed
                new List<int>() {499, 2, 497, 2, 496, 3, 496, 6}, //test worst case

                //test for sorted insert
                new List<int>() {3, 5, 7, 9, 1, 1}, //insert at first
                new List<int>() {3, 5, 7, 9, 2, 2}, //insert in middle
                new List<int>() {3, 5, 7, 9, 3, 3}, //insert in middle
                new List<int>() {5, 5, 7, 9, 5, 5}, //insert at last
            });
            var expectedSizesList = new List<List<int>>(new[]
            {
                new List<int>() {100, 90, 70, 50, 30, 20, 13},
                new List<int>() {1000},
                new List<int>() {131},
                new List<int>() {21, 20, 20, 20, 19},
                new List<int>(),
                new List<int>() {10, 20, 10, 4, 5, 12, 51, 25, 31},
                new List<int>() {20, 20, 20, 20, 20},
                new List<int>() {993, 509, 499},

                new List<int>() {2, 3, 5, 7, 9}, //insert at first
                new List<int>() {3, 4, 5, 7, 9}, //insert in middle
                new List<int>() {3, 5, 6, 7, 9}, //insert in middle
                new List<int>() {5, 5, 7, 9, 10}, //insert at last
            });


            for (int i = 0; i < testCasesCount; i++)
            {
                var unmergedGroup = ProduceFakeTxGroup(testCaseSizesList[i]);
                var txList = new List<Transaction>();
                unmergedGroup.ForEach(a => txList.AddRange(a));
                var actualRes =
                    (await _grouper.ProcessWithCoreCount(GroupStrategy.Limited_MinsAddUp, coreCountList[i], _chainId,
                        txList)).Item1;
                var acutalSizes = actualRes.Select(a => a.Count).ToList();
                Assert.Equal(expectedSizesList[i].OrderBy(a => a), acutalSizes.OrderBy(a => a));
            }
        }

        private List<Transaction> GetTestData()
        {
            var txList = new List<Transaction>();

            AddTxToList(txList, 0, 1);
            AddTxToList(txList, 2, 1);
            AddTxToList(txList, 3, 2);
            AddTxToList(txList, 2, 4);
            AddTxToList(txList, 4, 5);
            AddTxToList(txList, 6, 7);
            AddTxToList(txList, 8, 7);
            AddTxToList(txList, 9, 10);
            AddTxToList(txList, 10, 11);

            return txList;
        }

        private List<List<Transaction>> ProduceFakeTxGroup(IEnumerable<int> groupSizes)
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

        private List<Transaction> GetFullTxList()
        {
            var txList1 = GetFirstGroupTxList();
            var txList2 = GetSecondGroupTxList();

            for (int i = 0, x = 1; i < txList2.Count; i++, x += 3)
            {
                txList1.Insert(i + x, txList2[i]);
            }

            return txList1;
        }

        public List<Transaction> GetFirstGroupTxList()
        {
            var txList = new List<Transaction>();
            //Build txs that belong to same group
            AddTxToList(txList, 0, 1); //A -> B
            AddTxToList(txList, 0, 5); //A -> F
            AddTxToList(txList, 0, 4); //A -> E
            AddTxToList(txList, 1, 5); //B -> F   
            AddTxToList(txList, 1, 6); //B -> G
            AddTxToList(txList, 1, 7); //B -> H
            AddTxToList(txList, 1, 2); //B -> C
            AddTxToList(txList, 2, 3); //C -> D
            AddTxToList(txList, 3, 4); //D -> E
            AddTxToList(txList, 7, 5); //H -> F
            AddTxToList(txList, 8, 9); //I -> J    
            AddTxToList(txList, 10, 9); //K -> J
            AddTxToList(txList, 9, 11); //J -> L
            AddTxToList(txList, 9, 3); //J -> D
            AddTxToList(txList, 11, 10); //L -> k
            AddTxToList(txList, 11, 14); //L -> O
            AddTxToList(txList, 12, 11); //M -> L
            AddTxToList(txList, 12, 13); //M -> N
            AddTxToList(txList, 14, 15); //O -> P
            AddTxToList(txList, 14, 16); //O -> Q
            AddTxToList(txList, 16, 15); //Q -> P

            return txList;
        }

        public List<Transaction> GetSecondGroupTxList()
        {
            var txList = new List<Transaction>();
            //Build txs that belong to same group
            AddTxToList(txList, 17, 18); //R -> S
            AddTxToList(txList, 19, 18); //T -> S


            return txList;
        }

        private void AddTxToList(ICollection<Transaction> txList, int from, int to)
        {
            var tx = GetTransaction(from, to);
            txList.Add(tx);
        }

        private Transaction GetTransaction(int from, int to)
        {
            var tx = new Transaction();
            tx.From = _accountList[from];
            tx.To = _accountList[to];
            return tx;
        }

        private string StringRepresentation(List<Transaction> txs)
        {
            return String.Join(
                " ",
                txs.OrderBy(y => _accountList.IndexOf(y.From))
                    .ThenBy(z => _accountList.IndexOf(z.To))
                    .Select(
                        y => String.Format("({0}-{1})", _accountList.IndexOf(y.From), _accountList.IndexOf(y.To))
                    ));
        }
    }
}