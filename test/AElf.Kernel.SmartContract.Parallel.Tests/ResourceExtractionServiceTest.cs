using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs2;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp.Testing;
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
                WritePaths =
                {
                    GetPath(12345)
                },
                ReadPaths =
                {
                    GetPath(123)
                }
            });
            var otherTxn = GetAcs2Transaction(new ResourceInfo
            {
                WritePaths =
                {
                    GetPath(6789)
                },
                ReadPaths =
                {
                    GetPath(12345),
                    GetPath(123)
                }
            });
            var resourceInfos =
                (await Service.GetResourcesAsync(new Mock<IChainContext>().Object, new[] {txn,otherTxn}, CancellationToken.None))
                .ToList();
            var readOnlyPaths = resourceInfos.GetReadOnlyPaths();
            readOnlyPaths.Count.ShouldBe(1);
            readOnlyPaths.ShouldContain(GetPath(123));
            var executive =
                await SmartContractExecutiveService.GetExecutiveAsync(new Mock<IChainContext>().Object, txn.To);
            resourceInfos.Count.ShouldBe(2);
            resourceInfos[0].TransactionResourceInfo.ShouldBe(new TransactionResourceInfo
            {
                TransactionId = txn.GetHash(),
                WritePaths =
                {
                    GetPath(12345)
                },
                ReadPaths =
                {
                    GetPath(123)
                },
                ContractHash = executive.ContractHash
            });
            resourceInfos[1].TransactionResourceInfo.ShouldBe(new TransactionResourceInfo
            {
                TransactionId = otherTxn.GetHash(),
                WritePaths =
                {
                    GetPath(6789)
                },
                ReadPaths =
                {
                    GetPath(12345),
                    GetPath(123)
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
                WritePaths =
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
                From = Address.FromBase58("9Njc5pXW9Rw499wqSJzrfQuJQFVCcWnLNjZispJM4LjKmRPyq"),
                To = Address.FromBase58(InternalConstants.Acs2),
                MethodName = nameof(SmartContractExecution.Parallel.Tests.TestContract.TestContract.GetResourceInfo),
                Params = resourceInfo.ToByteString(),
                Signature = ByteString.CopyFromUtf8(KernelConstants.SignaturePlaceholder)
            };
        }

        private Transaction GetNonAcs2Transaction(ResourceInfo resourceInfo)
        {
            return new Transaction()
            {
                From = Address.FromBase58("9Njc5pXW9Rw499wqSJzrfQuJQFVCcWnLNjZispJM4LjKmRPyq"),
                To = Address.FromBase58(InternalConstants.NonAcs2),
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