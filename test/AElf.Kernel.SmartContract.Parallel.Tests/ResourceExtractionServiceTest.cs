using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs2;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class ResourceExtractionServiceTest : AbpIntegratedTest<ParallelMockTestModule>
    {
        private IResourceExtractionService Service =>
            Application.ServiceProvider.GetRequiredService<IResourceExtractionService>();

        private ISmartContractExecutiveService SmartContractExecutiveService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractExecutiveService>();

        [Fact]
        public async Task GetResourcesAsync_NonAcs2_Test()
        {
            var txn = GetNonAcs2Transaction(new ResourceInfo());
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, CancellationToken.None))
                .ToList();

            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().TransactionResourceInfo.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                ParallelType = ParallelType.NonParallelizable
            });
        }

        [Fact]
        public async Task GetResourcesAsync_Acs2_Parallelizable_Test()
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

            var executive =
                await SmartContractExecutiveService.GetExecutiveAsync(new Mock<IChainContext>().Object, txn.To);
            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().TransactionResourceInfo.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                Paths =
                {
                    GetPath(12345)
                },
                ContractHash = executive.ContractHash
            });
        }

        [Fact]
        public async Task GetResourcesAsync_Acs2_CancellationRequested_Test()
        {
            var cancelTokenSource = new CancellationTokenSource();
            cancelTokenSource.Cancel();
            var txn = GetAcs2Transaction(new ResourceInfo
            {
                Paths =
                {
                    GetPath(12345)
                }
            });
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, cancelTokenSource.Token))
                .ToList();
            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().TransactionResourceInfo.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                ParallelType = ParallelType.NonParallelizable
            });
        }

        [Fact]
        public async Task GetResourcesAsync_Acs2_MarkedNonParallelizable_Test()
        {
            var txn = GetAcs2Transaction(new ResourceInfo
            {
                Paths =
                {
                    GetPath(12345)
                }
            });
            MockContractRemarksService.NonParallelizable = true;
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, CancellationToken.None))
                .ToList();

            var executive = await SmartContractExecutiveService.GetExecutiveAsync(new Mock<IChainContext>().Object, txn.To);
            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().TransactionResourceInfo.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                ParallelType = ParallelType.NonParallelizable,
                ContractHash = executive.ContractHash,
                IsContractRemarks = true
            });
            MockContractRemarksService.NonParallelizable = false;
        }

        [Fact]
        public async Task GetResourcesAsync_Acs2_NonParallelizable_Test()
        {
            var txn = GetAcs2Transaction(new ResourceInfo
            {
                NonParallelizable = true
            });
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn}, CancellationToken.None))
                .ToList();

            var executive = await SmartContractExecutiveService.GetExecutiveAsync(new Mock<IChainContext>().Object, txn.To);
            resourceInfos.Count.ShouldBe(1);
            resourceInfos.First().TransactionResourceInfo.ShouldBe(new TransactionResourceInfo()
            {
                TransactionId = txn.GetHash(),
                ParallelType = ParallelType.NonParallelizable,
                ContractHash = executive.ContractHash
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
                Signature = ByteString.CopyFromUtf8(KernelConstants.SignaturePlaceholder)
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
                Signature = ByteString.CopyFromUtf8(KernelConstants.SignaturePlaceholder)
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