using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(
        typeof(CoreKernelAElfModule),
        typeof(CrossChainAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class CrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var dictionary = new Dictionary<long, Hash>
            {
                {1, Hash.FromString("1")},
                {2, Hash.FromString("2")},
                {3, Hash.FromString("3")}
            };

            //context.Services.AddTransient<IBlockValidationProvider, CrossChainValidationProvider>();
            context.Services.AddSingleton<CrossChainTestHelper>();
            context.Services.AddTransient(provider =>
            {
                var mockTransactionReadOnlyExecutionService = new Mock<ITransactionReadOnlyExecutionService>();
                mockTransactionReadOnlyExecutionService
                    .Setup(m => m.ExecuteAsync(It.IsAny<IChainContext>(), It.IsAny<Transaction>(),
                        It.IsAny<Timestamp>()))
                    .Returns<IChainContext, Transaction, Timestamp>((chainContext, transaction, dateTime) =>
                    {
                        var crossChainTestHelper =
                            context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                        return Task.FromResult(crossChainTestHelper.CreateFakeTransactionTrace(transaction));
                    });
                return mockTransactionReadOnlyExecutionService.Object;
            });
            context.Services.AddTransient(provider =>
            {
                var mockSmartContractAddressService = new Mock<ISmartContractAddressService>();
                mockSmartContractAddressService.Setup(m => m.GetAddressByContractName(It.IsAny<Hash>()))
                    .Returns(SampleAddress.AddressList[0]);
                return mockSmartContractAddressService.Object;
            });
            context.Services.AddTransient(provider =>
            {
                var mockBlockChainService = new Mock<IBlockchainService>();
                mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(() =>
                {
                    var chain = new Chain();
                    var crossChainTestHelper = context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                    chain.LastIrreversibleBlockHeight = crossChainTestHelper.FakeLibHeight;
                    return Task.FromResult(chain);
                });
                mockBlockChainService.Setup(m =>
                        m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns<Chain, long, Hash>((chain, height, hash) =>
                    {
                        if (height > 0 && height <= 3)
                            return Task.FromResult(dictionary[height]);
                        return Task.FromResult<Hash>(null);
                    });
                mockBlockChainService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>())).Returns<Hash>(hash =>
                {
                    foreach (var kv in dictionary)
                    {
                        if (kv.Value.Equals(hash))
                            return Task.FromResult(new Block {Header = new BlockHeader {Height = kv.Key}});
                    }

                    return Task.FromResult<Block>(null);
                });
                return mockBlockChainService.Object;
            });
        }
    }

    [DependsOn(
        typeof(CrossChainAElfModule),
        typeof(KernelCoreWithChainTestAElfModule)
    )]
    public class CrossChainWithChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<CrossChainTestHelper>();
            context.Services.AddTransient(provider =>
            {
                var mockSmartContractAddressService = new Mock<ISmartContractAddressService>();
                mockSmartContractAddressService.Setup(m => m.GetAddressByContractName(It.IsAny<Hash>()))
                    .Returns(SampleAddress.AddressList[0]);
                return mockSmartContractAddressService.Object;
            });
            context.Services.AddTransient(provider =>
            {
                var mockTransactionReadOnlyExecutionService = new Mock<ITransactionReadOnlyExecutionService>();
                mockTransactionReadOnlyExecutionService
                    .Setup(m => m.ExecuteAsync(It.IsAny<IChainContext>(), It.IsAny<Transaction>(),
                        It.IsAny<Timestamp>()))
                    .Returns<IChainContext, Transaction, Timestamp>((chainContext, transaction, dateTime) =>
                    {
                        var crossChainTestHelper =
                            context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                        return Task.FromResult(crossChainTestHelper.CreateFakeTransactionTrace(transaction));
                    });
                return mockTransactionReadOnlyExecutionService.Object;
            });
        }
    }
}