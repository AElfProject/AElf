using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSCoreTestAElfModule)
    )]
    public class OSCoreWithChainTestAElfModule : AElfModule
    {
        private OSTestHelper _osTestHelper;

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerationService>(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(s =>
                        s.GenerateSystemTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(new List<Transaction>()));
                return mockService.Object;
            });

            context.Services.AddTransient<IBlockExtraDataService>(o =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(s =>
                    s.FillBlockExtraDataAsync(It.IsAny<BlockHeader>())).Returns(Task.CompletedTask);
                return mockService.Object;
            });

            context.Services.AddTransient<IBlockValidationService>(o =>
            {
                var mockService = new Mock<IBlockValidationService>();
                mockService.Setup(s =>
                    s.ValidateBlockBeforeAttachAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));
                mockService.Setup(s =>
                    s.ValidateBlockBeforeExecuteAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));
                mockService.Setup(s =>
                    s.ValidateBlockAfterExecuteAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));
                return mockService.Object;
            });

            context.Services.AddSingleton<IAElfNetworkServer>(o => Mock.Of<IAElfNetworkServer>());
            context.Services.AddSingleton<ITxHub, MockTxHub>();

            context.Services.AddSingleton<ISmartContractRunner, UnitTestCSharpSmartContractRunner>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new UnitTestCSharpSmartContractRunner(
                    option.Value.SdkDir);
            });
            context.Services.AddSingleton<IDefaultContractZeroCodeProvider, Kernel.UnitTestContractZeroCodeProvider>();
            context.Services.AddSingleton<ISmartContractAddressService, UnitTestSmartContractAddressService>();
            context.Services
                .AddSingleton<ISmartContractAddressNameProvider, ParliamentSmartContractAddressNameProvider>();
        }
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            _osTestHelper = context.ServiceProvider.GetService<OSTestHelper>();
            AsyncHelper.RunSync(() => _osTestHelper.MockChainAsync());
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            AsyncHelper.RunSync(() => _osTestHelper.DisposeMock());
        }
    }
}