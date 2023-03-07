using System.Collections.Generic;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Kernel;

[DependsOn(
    typeof(AbpEventBusModule),
    typeof(TestBaseKernelAElfModule))]
public class KernelCoreTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        services.AddTransient<BlockValidationProvider>();
        services.AddTransient<SystemTransactionValidationProvider>();
        services.AddSingleton(p => Mock.Of<IAccountService>());
    }
}

[DependsOn(
    typeof(KernelCoreTestAElfModule))]
public class KernelMinerTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        //For system transaction generator testing
        services.AddTransient(provider =>
        {
            var transactionList = new List<Transaction>
            {
                new()
                {
                    From = SampleAddress.AddressList[0], To = SampleAddress.AddressList[2], MethodName = "InValue"
                },
                new()
                {
                    From = SampleAddress.AddressList[1], To = SampleAddress.AddressList[3], MethodName = "OutValue"
                }
            };
            var consensusTransactionGenerator = new Mock<ISystemTransactionGenerator>();
            consensusTransactionGenerator
                .Setup(m => m.GenerateTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                .Returns(Task.FromResult(transactionList));

            return consensusTransactionGenerator.Object;
        });

        //For BlockExtraDataService testing.
        services.AddTransient(
            builder =>
            {
                var dataProvider = new Mock<IBlockExtraDataProvider>();
                dataProvider.Setup(m => m.GetBlockHeaderExtraDataAsync(It.Is<BlockHeader>(o => o.Height != 100)))
                    .Returns(Task.FromResult(ByteString.CopyFromUtf8("not null")));

                ByteString bs = null;
                dataProvider.Setup(m => m.GetBlockHeaderExtraDataAsync(It.Is<BlockHeader>(o => o.Height == 100)))
                    .Returns(Task.FromResult(bs));

                dataProvider.Setup(m => m.BlockHeaderExtraDataKey).Returns(nameof(IBlockExtraDataProvider));
                return dataProvider.Object;
            });
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    private delegate Task<List<Transaction>> MockGenerateTransactions(Address from, long preBlockHeight,
        Hash previousBlockHash);
}