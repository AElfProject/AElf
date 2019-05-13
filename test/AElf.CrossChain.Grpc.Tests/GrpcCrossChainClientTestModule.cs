using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientTestModule : GrpcCrossChainTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            
            var services = context.Services;
            services.AddSingleton<ICrossChainServer, CrossChainGrpcServer>();
            
            services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockchainService>();
                mockService.Setup(m=>m.GetChainAsync())
                    .Returns(Task.FromResult(new Chain
                    {
                        LastIrreversibleBlockHeight = 1
                    }));
                return mockService.Object;
            });
            
            services.AddTransient(o =>
            {
                var mockService = new Mock<INewChainRegistrationService>();
                return mockService.Object;
            });
        }
    }
}