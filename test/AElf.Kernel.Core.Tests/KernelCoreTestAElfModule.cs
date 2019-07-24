using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

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
    }
    
    [DependsOn(
        typeof(KernelCoreTestAElfModule))]
    public class KernelMinerTestAElfModule : AElfModule
    {
        delegate void MockGenerateTransactions(Address @from, long preBlockHeight, Hash previousBlockHash,
            ref List<Transaction> generatedTransactions);
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<BlockValidationProvider>();
            
            //For system transaction generator testing
            services.AddTransient(provider =>
            {
                var transactionList = new List<Transaction>
                {
                    new Transaction() {From = SampleAddress.AddressList[0], To = SampleAddress.AddressList[2], MethodName = "InValue"},
                    new Transaction() {From = SampleAddress.AddressList[1], To = SampleAddress.AddressList[3], MethodName = "OutValue"},
                };
                var consensusTransactionGenerator = new Mock<ISystemTransactionGenerator>();
                consensusTransactionGenerator.Setup(m => m.GenerateTransactions(It.IsAny<Address>(), It.IsAny<long>(),
                        It.IsAny<Hash>(), ref It.Ref<List<Transaction>>.IsAny))
                    .Callback(
                        new MockGenerateTransactions((Address from, long preBlockHeight, Hash previousBlockHash,
                            ref List<Transaction> generatedTransactions) => generatedTransactions = transactionList));
                    
                return consensusTransactionGenerator.Object;
            });
            
            //For BlockExtraDataService testing.
            services.AddTransient(
                builder =>
                {
                    var  dataProvider = new Mock<IBlockExtraDataProvider>();
                    dataProvider.Setup( m=>m.GetExtraDataForFillingBlockHeaderAsync(It.Is<BlockHeader>(o=>o.Height != 100)))
                        .Returns(Task.FromResult(ByteString.CopyFromUtf8("not null")));

                    ByteString bs = null;
                    dataProvider.Setup( m=>m.GetExtraDataForFillingBlockHeaderAsync(It.Is<BlockHeader>(o => o.Height == 100)))
                        .Returns(Task.FromResult(bs));
                   
                    return dataProvider.Object;
                });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}