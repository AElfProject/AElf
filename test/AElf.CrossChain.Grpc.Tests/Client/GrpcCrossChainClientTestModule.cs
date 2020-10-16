using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Application;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Grpc.Server;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc.Client
{
    [DependsOn(typeof(GrpcCrossChainTestModule))]
    public class GrpcCrossChainClientTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<IGrpcCrossChainServer, GrpcCrossChainServer>();
            services.AddSingleton<GrpcCrossChainCommunicationTestHelper>();
            Configure<ChainOptions>(option => { option.ChainId = ChainHelper.ConvertBase58ToChainId("AELF"); });

            services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockchainService>();
                mockService.Setup(m => m.GetChainAsync())
                    .Returns(Task.FromResult(new Chain
                    {
                        LastIrreversibleBlockHeight = 1
                    }));
                return mockService.Object;
            });


            services.AddTransient(o =>
            {
                var mockService = new Mock<ICrossChainCacheEntityService>();
                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockBlockCacheEntityProvider = new Mock<IBlockCacheEntityProducer>();
                mockBlockCacheEntityProvider.Setup(m => m.TryAddBlockCacheEntity(It.IsAny<ICrossChainBlockEntity>()))
                    .Returns<ICrossChainBlockEntity>(
                        blockCacheEntity =>
                        {
                            if (!GrpcCrossChainCommunicationTestHelper.ClientBlockDataEntityCache.Contains(
                                blockCacheEntity))
                            {
                                GrpcCrossChainCommunicationTestHelper.ClientBlockDataEntityCache.Add(blockCacheEntity);
                                return true;
                            }

                            return false;
                        });
                return mockBlockCacheEntityProvider.Object;
            });

            services.AddTransient(o =>
            {
                var mockCrossChainResponseService = new Mock<ICrossChainResponseService>();
                int i = 0;
                mockCrossChainResponseService.Setup(b => b.ResponseSideChainBlockDataAsync(It.IsAny<long>()))
                    .Returns(
                        async () =>
                        {
                            if (i >= GrpcCrossChainCommunicationTestHelper.ServerBlockDataEntityCache.Count)
                                return null;
                            return await Task.FromResult(GrpcCrossChainCommunicationTestHelper.ServerBlockDataEntityCache[i++]);
                        });

                mockCrossChainResponseService
                    .Setup(m => m.ResponseChainInitializationDataFromParentChainAsync(It.IsAny<int>())).Returns(async () =>
                    {
                        var chainInitializationData = new ChainInitializationData
                        {
                            CreationHeightOnParentChain = 1
                        };
                        return await Task.FromResult(chainInitializationData);
                    });
                return mockCrossChainResponseService.Object;
            });
        }
    }

    [DependsOn(typeof(GrpcCrossChainClientTestModule))]
    public class GrpcCrossChainClientWithoutParentChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<CrossChainConfigOptions>(option => { option.ParentChainId = string.Empty; });
        }
    }
}