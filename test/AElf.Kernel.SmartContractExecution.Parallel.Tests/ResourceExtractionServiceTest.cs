using System.Linq;
using System.Threading.Tasks;
using Acs2;
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
            var txn = GetNonAcs2Transaction(new ResourceInfo());
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
        public async Task GetResourcesAsync_Acs2_Parallelizable()
        {
            var txn = GetAcs2Transaction(new ResourceInfo
            {
                Reources = {12345}
            });
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn})).ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                Resources =
                {
                    12345
                }
            });
        }

        [Fact]
        public async Task GetResourcesAsync_Acs2_NonParallelizable()
        {
            var txn = GetAcs2Transaction(new ResourceInfo
            {
                NonParallelizable = true
            });
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn})).ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                NonParallelizable = true
            });
        }

        private Transaction GetAcs2Transaction(ResourceInfo resourceInfo)
        {
            return new Transaction()
            {
                From = Address.FromString("Dummy"),
                To = Address.FromString(InternalConstants.Acs2),
                MethodName = nameof(TestContract.TestContract.GetResourceInfo),
                Params = resourceInfo.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
        }

        private Transaction GetNonAcs2Transaction(ResourceInfo resourceInfo)
        {
            return new Transaction()
            {
                From = Address.FromString("Dummy"),
                To = Address.FromString(InternalConstants.NonAcs2),
                MethodName = nameof(TestContract.TestContract.GetResourceInfo),
                Params = resourceInfo.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
        }
    }
}