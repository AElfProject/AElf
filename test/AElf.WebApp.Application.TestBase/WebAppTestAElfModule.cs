using System;
using System.Threading.Tasks;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.WebApp.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule),
        typeof(WebWebAppAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class WebAppTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.Replace(ServiceDescriptor.Singleton<INetworkService, NetworkService>());
            
            context.Services.Replace(ServiceDescriptor.Singleton<IAElfNetworkServer>(o =>
            {
                var pool = o.GetService<IPeerPool>();
                var serverMock = new Mock<IAElfNetworkServer>();
                
                serverMock.Setup(p => p.DisconnectAsync(It.IsAny<IPeer>(), It.IsAny<bool>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IPeer, bool>((peer, disc) => pool.RemovePeer(peer.Info.Pubkey));
                
                return serverMock.Object;
            }));
        }
    }
}