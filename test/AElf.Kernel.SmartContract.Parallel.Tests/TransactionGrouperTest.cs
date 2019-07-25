using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
            context.Services.AddSingleton<IBlockchainService>(
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

    public class TransactionGrouperTest : AbpIntegratedTest<TransactionGrouperTestModule>
    {
        private ITransactionGrouper Grouper => Application.ServiceProvider.GetRequiredService<ITransactionGrouper>();

        [Fact]
        public async Task Group_Test()
        {
            var group1Resources = new[] {(0, 1), (2, 1), (2, 4), (3, 2), (4, 5)};
            var group1 =
                group1Resources.Select(r => new {Resource = r, Transaction = GetTransaction("g1", r.Item1, r.Item2)})
                    .ToList();
            var group2Resources = new[] {(6, 7), (8, 7)};
            var group2 =
                group2Resources.Select(r => new {Resource = r, Transaction = GetTransaction("g2", r.Item1, r.Item2)})
                    .ToList();
            var group3Resources = new[] {(9, 10), (10, 11)};
            var group3 =
                group3Resources.Select(r => new {Resource = r, Transaction = GetTransaction("g3", r.Item1, r.Item2)})
                    .ToList();
            var groups = new[] {group1, group2, group3};
            var txLookup = groups.SelectMany(x => x).ToDictionary(x => x.Transaction.Params, x => x.Resource);
            var allTxns = groups.SelectMany(x => x).Select(x => x.Transaction).OrderBy(x => Guid.NewGuid()).ToList();

            var chainContext = new ChainContext
            {
                BlockHeight = 10,
                BlockHash = Hash.FromString("blockHash")
            };
            var grouped = await Grouper.GroupAsync(chainContext, allTxns);
            var groupedResources = grouped.Parallelizables.Select(g => g.Select(t => txLookup[t.Params]).ToList()).ToList();
            var expected = groups.Select(g => g.Select(x => x.Resource).ToList()).Select(StringRepresentation)
                .OrderBy(x => x);
            var actual = groupedResources.Select(StringRepresentation).OrderBy(x => x);
            Assert.Equal(expected, actual);
        }

        private Transaction GetTransaction(string methodName, int from, int to)
        {
            var tx = new Transaction
            {
                MethodName = methodName,
                Params = new TransactionResourceInfo
                {
                    Resources = {from, to}
                }.ToByteString()
            };
            return tx;
        }

        private string StringRepresentation(List<(int, int)> resources)
        {
            return string.Join(" ", resources.Select(r => r.ToString()).OrderBy(x => x));
        }
    }
}