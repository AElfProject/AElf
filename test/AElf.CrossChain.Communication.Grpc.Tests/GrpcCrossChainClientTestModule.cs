using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication.Grpc
{
    [DependsOn(typeof(GrpcCrossChainTestModule))]
    public class GrpcCrossChainClientTestModule :AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<IGrpcCrossChainServer, GrpcCrossChainServer>();

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
                var mockCrossChainDataProvider = new Mock<ICrossChainService>();
                mockCrossChainDataProvider
                    .Setup(c => c.GetChainInitializationDataAsync(It.IsAny<int>())).Returns(async () =>
                        await Task.FromResult(new ChainInitializationData
                        {
                            CreationHeightOnParentChain = 1,
                        }));
                return mockCrossChainDataProvider.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<ICrossChainCacheEntityService>();
                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockBlockCacheEntityProvider = new Mock<IBlockCacheEntityProducer>();
                mockBlockCacheEntityProvider.Setup(b => b.TryAddBlockCacheEntity(It.IsAny<IBlockCacheEntity>()))
                    .Returns<IBlockCacheEntity>(
                        blockCacheEntity =>
                        {
                            if (!GrpcCrossChainCommunicationTestHelper.CrossChainBlockDataEntityCache.TryGetValue(
                                blockCacheEntity.ChainId, out var blockCacheEntities))
                            {
                                GrpcCrossChainCommunicationTestHelper.CrossChainBlockDataEntityCache.Add(
                                    blockCacheEntity.ChainId, new List<IBlockCacheEntity> {blockCacheEntity});
                                return true;
                            }

                            if (blockCacheEntities.Contains(blockCacheEntity))
                                return false;
                            blockCacheEntities.Add(blockCacheEntity);
                            return true;
                        });
                return mockBlockCacheEntityProvider.Object;
            });
        }
    }
}