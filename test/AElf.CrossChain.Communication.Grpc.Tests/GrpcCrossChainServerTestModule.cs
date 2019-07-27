using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainServerTestModule : GrpcCrossChainTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);

            var services = context.Services;
            services.AddTransient(o =>
            {
                var kernelTestHelper = context.Services.GetRequiredServiceLazy<KernelTestHelper>();
                var mockService = new Mock<IBlockchainService>();
                mockService.Setup(m => m.GetChainAsync())
                    .Returns(Task.FromResult(new Chain
                    {
                        LastIrreversibleBlockHeight = 10
                    }));
                mockService.Setup(m =>
                        m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(Hash.Generate()));
                mockService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>()))
                    .Returns(() =>
                    {
                        var previousBlockHash = Hash.FromString("previousBlockHash");
                        return Task.FromResult(kernelTestHelper.Value.GenerateBlock(9, previousBlockHash));
                    });
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
                var mockCrossChainResponseService = new Mock<ICrossChainResponseService>();
                mockCrossChainResponseService
                    .Setup(c => c.ResponseParentChainBlockDataAsync(It.IsAny<long>(), It.IsAny<int>())).Returns(
                        () =>
                        {
                            var parentChanBlockData = new ParentChainBlockData
                            {
                                ChainId = 123,
                                Height = 10,
                                TransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot")
                            };
                            return Task.FromResult(parentChanBlockData);
                        });
                mockCrossChainResponseService
                    .Setup(c => c.ResponseSideChainBlockDataAsync(It.IsAny<long>())).Returns(
                        () =>
                        {
                            var sideChanBlockData = new SideChainBlockData()
                            {
                                ChainId = 123,
                                Height = 10,
                            };
                            return Task.FromResult(sideChanBlockData);
                        });
                mockCrossChainResponseService
                    .Setup(c => c.ResponseChainInitializationDataFromParentChainAsync(It.IsAny<int>())).Returns(
                        () =>
                        {
                            var chainInitializationData = new ChainInitializationData()
                            {
                                CreationHeightOnParentChain = 1
                            };
                            return Task.FromResult(chainInitializationData);
                        });
                return mockCrossChainResponseService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<ICrossChainCacheEntityService>();
                return mockService.Object;
            });
        }
    }
}