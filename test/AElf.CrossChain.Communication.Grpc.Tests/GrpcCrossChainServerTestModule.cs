using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
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
            Configure<CrossChainConfigOption>(option =>
            {
                option.ParentChainId = ChainHelpers.ConvertBase58ToChainId("AELF");
            });

            var services = context.Services;
            services.AddTransient(o =>
            {
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
                    .Returns(Task.FromResult(new Block
                    {
                        Height = 10,
                        Header = new BlockHeader
                        {
                            ChainId = 0,
                            BlockExtraDatas =
                            {
                                ByteString.CopyFrom(new CrossChainExtraData().ToByteArray()),
                                ByteString.CopyFrom(Hash.Generate().ToByteArray())
                            }
                        }
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
                var mockService = new Mock<ICrossChainCacheEntityService>();
                return mockService.Object;
            });
        }
    }
}