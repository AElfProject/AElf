using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
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
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(o =>
            {
                var peerPoolMock = new Mock<IPeerPool>();
                peerPoolMock.Setup(p => p.FindPeerByAddress(It.IsAny<string>()))
                    .Returns<string>(adr => null);
                peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                    .Returns(new List<IPeer>());
                return peerPoolMock.Object;
            });

            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(s =>
                        s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(new List<Transaction>());
                return mockService.Object;
            });

            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(s =>
                    s.FillBlockExtraData(It.IsAny<BlockHeader>())).Returns(Task.CompletedTask);
                return mockService.Object;
            });

            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockValidationService>();
                mockService.Setup(s =>
                    s.ValidateBlockBeforeExecuteAsync(It.IsAny<Block>())).Returns(Task.FromResult(true));
                mockService.Setup(s =>
                    s.ValidateBlockAfterExecuteAsync(It.IsAny<Block>())).Returns(Task.FromResult(true));
                return mockService.Object;
            });

            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var osTestHelper = context.ServiceProvider.GetService<OSTestHelper>();
            AsyncHelper.RunSync(() => osTestHelper.MockChain());
        }
    }
}