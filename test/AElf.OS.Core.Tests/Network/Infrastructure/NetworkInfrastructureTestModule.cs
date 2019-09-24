using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule))]
    public class NetworkInfrastructureTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o=>
            {
                o.MaxPeers = 2;
            });

            var services = context.Services;

            services.AddSingleton(provider =>
            {
                var mockService = new Mock<IBlockchainService>();
                mockService.Setup(m => m.GetChainAsync()).Returns(
                    Task.FromResult(new Chain
                    {
                        BestChainHash = Hash.FromString("best"),
                        BestChainHeight = 10
                    }));
                mockService.Setup(m => m.GetBlockHeaderByHashAsync(It.IsAny<Hash>())).Returns(
                    Task.FromResult(new BlockHeader(
                    )));

                return mockService.Object;
            });

            services.AddSingleton<IPeerReconnectionStateProvider>();
        }
    }
}