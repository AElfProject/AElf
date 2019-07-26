using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class ConflictingTransactionIdentificationServiceTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IResourceExtractionService, MockResourceExtractionService>();
            var blockchainService = new MockBlockchainService();
            context.Services.AddSingleton<IBlockchainService>(_ => blockchainService);
            context.Services
                .AddSingleton<IConflictingTransactionIdentificationService, ConflictingTransactionIdentificationService
                >();
        }
    }

    public class
        ConflictingTransactionIdentificationServiceTest : AbpIntegratedTest<
            ConflictingTransactionIdentificationServiceTestModule>
    {
        private IConflictingTransactionIdentificationService Service =>
            Application.ServiceProvider.GetRequiredService<IConflictingTransactionIdentificationService>();

        private IBlockchainService BlockchainService =>
            Application.ServiceProvider.GetRequiredService<IBlockchainService>();

        [Fact]
        public async Task IdentifyProblematicTransactionTest()
        {
            var includedInBlock = new[]
            {
                GetFakePairs(SampleAddress.AddressList[0], new[] {1, 2, 3}), // independent
                GetFakePairs(SampleAddress.AddressList[1], new[] {4, 5, 6}),
                GetFakePairs(SampleAddress.AddressList[2], new[] {6, 7, 8})
            };
            var conflicting = new[]
            {
                GetFakePairs(SampleAddress.AddressList[3], new[] {10, 11, 12}, new[] {4, 10, 11, 12})
            };
            var okTxnInConflictingSet = new[]
            {
                GetFakePairs(SampleAddress.AddressList[4], new[] {13, 14, 15})
            };

            await BlockchainService.AddTransactionsAsync(includedInBlock.Concat(conflicting).Concat(okTxnInConflictingSet)
                .Select(x => x.Item2));

            var wrong = await Service.IdentifyConflictingTransactionsAsync(new Mock<IChainContext>().Object,
                includedInBlock.Select(x => x.Item1).ToList(),
                conflicting.Concat(okTxnInConflictingSet).Select(x => x.Item1).ToList());
            Assert.Single(wrong);
            Assert.Equal(conflicting.First().Item2, wrong.First());
        }

        private (ExecutionReturnSet, Transaction) GetFakePairs(Address destination, int[] expectedKeys,
            int[] actualKeys = null)
        {
            var tri = new TransactionResourceInfo
            {
                Paths =
                {
                    expectedKeys.Select(GetPath)
                }
            };
            var txn = new Transaction
            {
                From = SampleAddress.AddressList[7],
                To = destination,
                MethodName = "Dummy",
                Params = tri.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
            var rs = new ExecutionReturnSet {TransactionId = txn.GetHash()};
            actualKeys = actualKeys ?? expectedKeys;
            foreach (var key in actualKeys.Select(GetPath).Select(p => p.ToStateKey()))
            {
                rs.StateAccesses[key] = true;
            }

            return (rs, txn);
        }

        private ScopedStatePath GetPath(int value)
        {
            return new ScopedStatePath
            {
                Address = SampleAddress.AddressList[0],
                Path = new StatePath
                {
                    Parts = {value.ToString()}
                }
            };
        }
    }
}