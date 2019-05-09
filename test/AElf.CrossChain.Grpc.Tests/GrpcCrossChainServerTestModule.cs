using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc
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
                                ByteString.CopyFrom(Hash.Generate().ToByteArray()),
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
                    .Setup(c => c.GetChainInitializationContextAsync(It.IsAny<int>(), It.IsAny<Hash>(),
                        It.IsAny<long>())).Returns(async () => await Task.FromResult(new ChainInitializationContext
                    {
                        ParentChainHeightOfCreation = 1,
                    }));
                return mockCrossChainDataProvider.Object;
            });
            services.AddTransient(o =>
            {
                var mockService = new Mock<IBasicCrossChainDataProvider>();
                mockService.Setup(c => c.GetChainInitializationContextAsync(It.IsAny<int>(), It.IsAny<Hash>(),
                    It.IsAny<long>())).Returns(async () => await Task.FromResult(new ChainInitializationContext
                {
                    ParentChainHeightOfCreation = 1,
                }));
                return mockService.Object;
            });
            services.AddTransient(o =>
            {
                var mockService = new Mock<INewChainRegistrationService>();
                return mockService.Object;
            });

            services.AddSingleton<CrossChainRpc.CrossChainRpcBase, CrossChainGrpcServerBase>();
        }
    }
}