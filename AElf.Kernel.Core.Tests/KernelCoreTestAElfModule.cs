using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(TestBaseKernelAElfModule))]
    public class KernelCoreTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<BlockValidationProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
//            var kernelTestHelper = context.ServiceProvider.GetService<KernelTestHelper>();
//            AsyncHelper.RunSync(() => kernelTestHelper.CreateChain());
        }
    }
    
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(TestBaseKernelAElfModule))]
    public class KernelMinerTestAElfModule : AElfModule
    {
        delegate void MockGenerateTransactions(Address @from, long preBlockHeight, Hash previousBlockHash,
            ref List<Transaction> generatedTransactions);
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<BlockValidationProvider>();
            
            services.AddTransient(provider =>
            {
                var transactionList = new List<Transaction>
                {
                    new Transaction() {From = Address.Zero, To = Address.Generate(), MethodName = "InValue"},
                    new Transaction() {From = Address.Zero, To = Address.Generate(), MethodName = "OutValue"},
                };
                var consensusTransactionGenerator = new Mock<ISystemTransactionGenerator>();
                consensusTransactionGenerator.Setup(m => m.GenerateTransactions(It.IsAny<Address>(), It.IsAny<long>(),
                        It.IsAny<Hash>(), ref It.Ref<List<Transaction>>.IsAny))
                    .Callback(
                        new MockGenerateTransactions((Address from, long preBlockHeight, Hash previousBlockHash,
                            ref List<Transaction> generatedTransactions) => generatedTransactions = transactionList));
                    
                return consensusTransactionGenerator.Object;
            });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}