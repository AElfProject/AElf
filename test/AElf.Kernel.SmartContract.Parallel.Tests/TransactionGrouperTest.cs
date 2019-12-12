using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    
    public class TransactionGrouperTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITransactionGrouper, TransactionGrouper>();
            context.Services.AddSingleton(
                _ =>
                {
                    var mock = new Mock<IBlockchainService>();
                    mock.Setup(s => s.GetChainAsync()).Returns(Task.FromResult<Chain>(new Chain()
                    {
                        BestChainHash = Hash.Empty
                    }));
                    return mock.Object;
                });
            context.Services.AddSingleton<IResourceExtractionService, MockResourceExtractionService>();
        }
    }
    public class TransactionGrouperTest:AbpIntegratedTest<TransactionGrouperTestModule>
    {
        private ITransactionGrouper Grouper => Application.ServiceProvider.GetRequiredService<ITransactionGrouper>();

        [Fact]
        public async Task Group_Transaction_Groups_Into_One_Group_Test()
        {
            //Transaction count less than min transaction count in group
            {
                var transactionCounts = new[] {5, 2, 2};
                var groupCount = 3;
                var groups = GenerateGroups(groupCount, transactionCounts);
                var txLookup = groups.SelectMany(x => x).ToDictionary(x => x.Transaction.Params, x => x.Resource);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).OrderBy(x => Guid.NewGuid()).ToList();

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);
                var groupedResources = grouped.Parallelizables.Select(g => g.Select(t => txLookup[t.Params]).ToList()).ToList();
                var expected = new List<List<Resource>> {groups.SelectMany(g => g.Select(x => x.Resource)).ToList()}
                    .Select(StringRepresentation).OrderBy(x => x);
           
                var actual = groupedResources.Select(StringRepresentation).OrderBy(x => x);
                expected.ShouldBe(actual);
            }
            //Transaction count equal to min transaction count in group
            {
                var transactionCounts = new[] {5, 3, 2};
                var groupCount = 3;
                var groups = GenerateGroups(groupCount, transactionCounts);
                var txLookup = groups.SelectMany(x => x).ToDictionary(x => x.Transaction.Params, x => x.Resource);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).OrderBy(x => Guid.NewGuid()).ToList();

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);
                var groupedResources = grouped.Parallelizables.Select(g => g.Select(t => txLookup[t.Params]).ToList()).ToList();
                var expected = new List<List<Resource>> {groups.SelectMany(g => g.Select(x => x.Resource)).ToList()}
                    .Select(StringRepresentation).OrderBy(x => x);
           
                var actual = groupedResources.Select(StringRepresentation).OrderBy(x => x);
                expected.ShouldBe(actual);
            }
            //Transaction count more than min transaction count in group
            {
                var transactionCounts = new[] {5, 2, 2, 2, 2, 1};
                var groupCount = 6;
                var groups = GenerateGroups(groupCount, transactionCounts);
                var txLookup = groups.SelectMany(x => x).ToDictionary(x => x.Transaction.Params, x => x.Resource);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).OrderBy(x => Guid.NewGuid()).ToList();

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);
                var groupedResources = grouped.Parallelizables.Select(g => g.Select(t => txLookup[t.Params]).ToList()).ToList();
                var expected = new List<List<Resource>> {groups.SelectMany(g => g.Select(x => x.Resource)).ToList()}
                    .Select(StringRepresentation).OrderBy(x => x);
           
                var actual = groupedResources.Select(StringRepresentation).OrderBy(x => x);
                expected.ShouldBe(actual);
            }
        }

        [Fact]
        public async Task Group_Transaction_Groups_Into_Multi_Groups_Test()
        {
            //Transaction count = 24 and group count = 12 groups into 2 groups
            {
                var transactionCounts = new[] {1, 1, 2, 2, 3, 3, 1, 1, 4, 4, 1, 1};
                var groups = GenerateGroups(transactionCounts.Length, transactionCounts);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).ToList();
                allTxns.Count.ShouldBe(transactionCounts.Sum());

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);
                grouped.Parallelizables.Count.ShouldBe(2);
                grouped.Parallelizables.Sum(p => p.Count).ShouldBe(transactionCounts.Sum());
                groups = groups.OrderBy(g => g.Count).ToArray(); //{1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4};
                grouped.Parallelizables[0].Count.ShouldBe(13);
                ContactGroups(groups, 0, 9).All(r => grouped.Parallelizables[0].Contains(r.Transaction))
                    .ShouldBe(true);
                grouped.Parallelizables[1].Count.ShouldBe(11);
                ContactGroups(groups, 9, 3).All(r => grouped.Parallelizables[1].Contains(r.Transaction))
                    .ShouldBe(true);
                grouped.NonParallelizables.Count.ShouldBe(0);
            }

            //Transaction count = 28 and group count = 12 groups into 3 groups
            {
                var transactionCounts = new[] {1, 1, 2, 2, 3, 3, 1, 1, 6, 6, 1, 1};
                var groups = GenerateGroups(transactionCounts.Length, transactionCounts);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).ToList();
                allTxns.Count.ShouldBe(transactionCounts.Sum());

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);
                grouped.Parallelizables.Count.ShouldBe(3);
                grouped.Parallelizables.Sum(p => p.Count).ShouldBe(transactionCounts.Sum());
                groups = groups.OrderBy(g => g.Count).ToArray(); //{1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 6, 6};
                grouped.Parallelizables[0].Count.ShouldBe(10);
                ContactGroups(groups, 0, 8).All(r => grouped.Parallelizables[0].Contains(r.Transaction))
                    .ShouldBe(true);
                grouped.Parallelizables[1].Count.ShouldBe(12);
                ContactGroups(groups, 8, 3).All(r => grouped.Parallelizables[1].Contains(r.Transaction))
                    .ShouldBe(true);
                grouped.Parallelizables[2].Count.ShouldBe(6);
                ContactGroups(groups, 11, 1).All(r => grouped.Parallelizables[2].Contains(r.Transaction))
                    .ShouldBe(true);
                grouped.NonParallelizables.Count.ShouldBe(0);
            }

            //Transaction count = 56 and group count = 2 groups into 2 groups
            {
                var transactionCounts = new[] {6, 50};
                var groups = GenerateGroups(transactionCounts.Length, transactionCounts);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).OrderBy(x => Guid.NewGuid())
                    .ToList();
                allTxns.Count.ShouldBe(transactionCounts.Sum());

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);

                grouped.Parallelizables.Count.ShouldBe(transactionCounts.Length);
                grouped.Parallelizables.Sum(p => p.Count).ShouldBe(transactionCounts.Sum());
                grouped.Parallelizables[0].Count.ShouldBe(50);
                groups[1].All(r => grouped.Parallelizables[0].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[1].Count.ShouldBe(6);
                groups[0].All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.NonParallelizables.Count.ShouldBe(0);
            }
            //Transaction count = 102 and group count = 2 groups into 2 groups
            {
                var transactionCounts = new[] {2, 100};
                var groups = GenerateGroups(transactionCounts.Length, transactionCounts);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).OrderBy(x => Guid.NewGuid())
                    .ToList();
                allTxns.Count.ShouldBe(transactionCounts.Sum());

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);

                grouped.Parallelizables.Count.ShouldBe(transactionCounts.Length);
                grouped.Parallelizables.Sum(p => p.Count).ShouldBe(transactionCounts.Sum());
                grouped.Parallelizables[0].Count.ShouldBe(100);
                groups[1].All(r => grouped.Parallelizables[0].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[1].Count.ShouldBe(2);
                groups[0].All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.NonParallelizables.Count.ShouldBe(0);
            }

            //Transaction count = 109 and group count = 16 groups into 8 groups
            {
                var transactionCounts = new[] {2, 20, 16, 8, 11, 6, 9, 5, 7, 3, 1, 2, 4, 5, 6, 4};
                var groups = GenerateGroups(transactionCounts.Length, transactionCounts);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction)
                    .ToList();
                allTxns.Count.ShouldBe(transactionCounts.Sum());

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);

                grouped.Parallelizables.Count.ShouldBe(8);
                grouped.Parallelizables.Sum(p => p.Count).ShouldBe(transactionCounts.Sum());
                groups = groups.OrderBy(g => g.Count).ToArray(); //{1, 2, 2, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 16, 20}
                grouped.Parallelizables[0].Count.ShouldBe(12);
                ContactGroups(groups, 0, 5).All(r => grouped.Parallelizables[0].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[1].Count.ShouldBe(14);
                ContactGroups(groups, 5, 3).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[2].Count.ShouldBe(12);
                ContactGroups(groups, 8, 2).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[3].Count.ShouldBe(15);
                ContactGroups(groups, 10, 2).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[4].Count.ShouldBe(11);
                ContactGroups(groups, 13, 1).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[5].Count.ShouldBe(16);
                ContactGroups(groups, 14, 1).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[6].Count.ShouldBe(20);
                ContactGroups(groups, 15, 1).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[7].Count.ShouldBe(9);
                ContactGroups(groups, 12, 1).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.NonParallelizables.Count.ShouldBe(0);
            }
            
            //Transaction count = 269 and group count = 16 groups into 9 groups
            {
                var transactionCounts = new[] {12, 30, 27, 18, 20, 16, 19, 15, 17, 13, 11, 12, 14, 15, 16, 14};
                var groups = GenerateGroups(transactionCounts.Length, transactionCounts);
                var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction)
                    .ToList();
                allTxns.Count.ShouldBe(transactionCounts.Sum());

                var chainContext = new ChainContext
                {
                    BlockHeight = 10,
                    BlockHash = Hash.FromString("blockHash")
                };
                var grouped = await Grouper.GroupAsync(chainContext, allTxns);

                grouped.Parallelizables.Count.ShouldBe(9);
                grouped.Parallelizables.Sum(p => p.Count).ShouldBe(transactionCounts.Sum());
                groups = groups.OrderBy(g => g.Count).ToArray(); //{11, 12, 12, 13, 14, 14, 15, 15, 16, 16, 17, 18, 19, 20, 27, 30}
                grouped.Parallelizables[0].Count.ShouldBe(35);
                ContactGroups(groups, 0, 3).All(r => grouped.Parallelizables[0].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[1].Count.ShouldBe(27);
                ContactGroups(groups, 3, 2).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[2].Count.ShouldBe(29);
                ContactGroups(groups, 5, 2).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[3].Count.ShouldBe(31);
                ContactGroups(groups, 7, 2).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[4].Count.ShouldBe(33);
                ContactGroups(groups, 9, 2).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[5].Count.ShouldBe(37);
                ContactGroups(groups, 11, 2).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[6].Count.ShouldBe(27);
                ContactGroups(groups, 14, 1).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[7].Count.ShouldBe(30);
                ContactGroups(groups, 15, 1).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.Parallelizables[8].Count.ShouldBe(20);
                ContactGroups(groups, 13, 1).All(r => grouped.Parallelizables[1].Contains(r.Transaction)).ShouldBe(true);
                grouped.NonParallelizables.Count.ShouldBe(0);
            }
        }

        private List<ResourceInfo>[] GenerateGroups(int groupCount, int[] transactionCounts)
        {
            var groups = new List<ResourceInfo>[groupCount];
            for (var i = 0; i < groupCount; i++)
            {
                var groupResources = new List<Resource>();
                var count = transactionCounts[i];
                for (var j = 0; j < count; j++)
                {
                    groupResources.Add(new Resource{First = i * 100 + j,Second =  i * 100 + j + 1});
                }

                groups[i] = groupResources.Select(r => new ResourceInfo
                        {Resource = r, Transaction = GetTransaction($"g{i}", r.First, r.Second)})
                    .ToList();
            }

            return groups;
        }

        private List<ResourceInfo> ContactGroups(List<ResourceInfo>[] groups,int start, int offset)
        {
            var resourceInfos= new List<ResourceInfo>();
            for (int i = start; i < offset; i++)
            {
                resourceInfos.AddRange(groups[i]);
            }

            return resourceInfos;
        }

        private Transaction GetTransaction(string methodName, int from, int to)
        {
            var tx = new Transaction
            {
                MethodName = methodName,
                Params = new TransactionResourceInfo
                {
                    Paths =
                    {
                        GetPath(from), GetPath(to)
                    }
                }.ToByteString()
            };
            return tx;
        }

        private string StringRepresentation(List<Resource> resources)
        {
            return string.Join(" ", resources.Select(r => r.ToString()).OrderBy(x => x));
        }

        private ScopedStatePath GetPath(int value)
        {
            return new ScopedStatePath
            {
                Path = new StatePath
                {
                    Parts = {value.ToString()}
                }
            };
        }

        private class Resource
        {
            public int First { get; set; }
            
            public int Second { get; set; }

            public override string ToString()
            {
                return $"({First}, {Second})";
            }
        }

        private class ResourceInfo
        {
            public Resource Resource { get; set; }
            
            public Transaction Transaction { get; set; }
        }
    }
}