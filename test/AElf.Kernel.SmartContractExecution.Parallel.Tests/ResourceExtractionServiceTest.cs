using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Parallel.Tests
{
    public class ResourceExtractionServiceTest : AbpIntegratedTest<TestModule>
    {
        private IResourceExtractionService Service =>
            Application.ServiceProvider.GetRequiredService<IResourceExtractionService>();

        [Fact]
        public async Task GetResourcesAsync_NonAcs2()
        {
            var txn = GetNonAcs2Transaction();
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn})).ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                NonParallelizable = true
            });
        }

        [Fact]
        public async Task GetResourcesAsync_Acs2()
        {
            var txn = GetAcs2Transaction();
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn})).ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                Resources =
                {
                    txn.GetHashCode()
                }
            });
        }

        private Transaction GetAcs2Transaction()
        {
            return new Transaction()
            {
                From = Address.FromString("Dummy"),
                To = Address.FromString(InternalConstants.Acs2),
                MethodName = nameof(TestContract.TestContract.GetResourceInfo),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
        }

        private Transaction GetNonAcs2Transaction()
        {
            return new Transaction()
            {
                From = Address.FromString("Dummy"),
                To = Address.FromString(InternalConstants.NonAcs2),
                MethodName = nameof(TestContract.TestContract.GetResourceInfo),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
        }
    }
}