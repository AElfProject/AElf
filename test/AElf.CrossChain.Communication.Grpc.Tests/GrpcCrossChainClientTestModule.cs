using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientTestModule : GrpcCrossChainTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            
            var services = context.Services;
            services.AddSingleton<IGrpcCrossChainServer, GrpcGrpcCrossChainServer>();
            
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
                var mockCrossChainDataProvider = new Mock<ICrossChainDataProvider>();
                mockCrossChainDataProvider
                    .Setup(c => c.GetChainInitializationDataAsync(It.IsAny<int>(), It.IsAny<Hash>(),
                        It.IsAny<long>())).Returns(async () => await Task.FromResult(new ChainInitializationData
                    {
                        CreationHeightOnParentChain = 1,
                    }));
                return mockCrossChainDataProvider.Object;
            });
            
            services.AddTransient(o =>
            {
                var mockService = new Mock<INewChainRegistrationService>();
                return mockService.Object;
            });
        }
    }
}