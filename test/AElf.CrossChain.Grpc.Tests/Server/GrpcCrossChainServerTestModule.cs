using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Application;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc.Server
{
    [DependsOn(typeof(GrpcCrossChainTestModule))]
    public class GrpcCrossChainServerTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);

            var services = context.Services;
            services.AddSingleton<ICrossChainCommunicationPlugin, GrpcCrossChainServerNodePlugin>();
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
                    .Returns(Task.FromResult(HashHelper.ComputeFrom("hash")));
                mockService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>()))
                    .Returns(() =>
                    {
                        var previousBlockHash = HashHelper.ComputeFrom("previousBlockHash");
                        return Task.FromResult(kernelTestHelper.Value.GenerateBlock(9, previousBlockHash));
                    });
                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockCrossChainResponseService = new Mock<ICrossChainResponseService>();
                mockCrossChainResponseService
                    .Setup(c => c.ResponseParentChainBlockDataAsync(It.IsAny<long>(), It.IsAny<int>())).Returns<long, int>(
                        (height, chainId) =>
                        {
                            if (height > 100)
                                return Task.FromResult<ParentChainBlockData>(null);
                            
                            var parentChanBlockData = new ParentChainBlockData
                            {
                                ChainId = chainId,
                                Height = height,
                                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("TransactionStatusMerkleRoot")
                            };
                            return Task.FromResult(parentChanBlockData);
                        });
                mockCrossChainResponseService
                    .Setup(c => c.ResponseSideChainBlockDataAsync(It.IsAny<long>())).Returns<long>(
                        (height) =>
                        {
                            if (height > 100)
                                return Task.FromResult<SideChainBlockData>(null);
                            var sideChanBlockData = new SideChainBlockData()
                            {
                                ChainId = ChainHelper.GetChainId(1),
                                Height = height,
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