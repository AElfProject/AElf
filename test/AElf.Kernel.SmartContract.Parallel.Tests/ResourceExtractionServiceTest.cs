using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs2;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
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
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, CancellationToken.None))
                .ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().Item2.ShouldBe(new TransactionResourceInfo()
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
                Paths =
                {
                    GetPath(12345)
                }
            });
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, CancellationToken.None))
                .ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().Item2.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                Paths =
                {
                    GetPath(12345)
                }
            });
        }

        [Fact]
        public async Task GetResourcesAsync_Acs2_MarkedNonParallelizable()
        {
            var txn = GetAcs2Transaction(new ResourceInfo
            {
                Paths =
                {
                    GetPath(12345)
                }
            });
            MockCodeRemarksManager.NonParallelizable = true;
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, CancellationToken.None))
                .ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().Item2.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                NonParallelizable = true
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
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, CancellationToken.None))
                .ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().Item2.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                NonParallelizable = true
            });
        }

        private Transaction GetAcs2Transaction(ResourceInfo resourceInfo)
        {
            return new Transaction()
            {
                From = AddressHelper.Base58StringToAddress("9Njc5pXW9Rw499wqSJzrfQuJQFVCcWnLNjZispJM4LjKmRPyq"),
                To = AddressHelper.Base58StringToAddress(InternalConstants.Acs2),
                MethodName = nameof(SmartContractExecution.Parallel.Tests.TestContract.TestContract.GetResourceInfo),
                Params = resourceInfo.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
        }

        private Transaction GetNonAcs2Transaction(ResourceInfo resourceInfo)
        {
            return new Transaction()
            {
                From = AddressHelper.Base58StringToAddress("9Njc5pXW9Rw499wqSJzrfQuJQFVCcWnLNjZispJM4LjKmRPyq"),
                To = AddressHelper.Base58StringToAddress(InternalConstants.NonAcs2),
                MethodName = nameof(SmartContractExecution.Parallel.Tests.TestContract.TestContract.GetResourceInfo),
                Params = resourceInfo.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
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
    }
}