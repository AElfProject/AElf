using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
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
                        s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(new List<Transaction>());
                return mockService.Object;
            });

            context.Services.AddTransient<IBlockExtraDataService>(o =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(s =>
                    s.FillBlockExtraData(It.IsAny<BlockHeader>())).Returns(Task.CompletedTask);
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