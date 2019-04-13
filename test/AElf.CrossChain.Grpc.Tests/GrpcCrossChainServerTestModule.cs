using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
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
            Configure<GrpcCrossChainConfigOption>(option =>
            {
                option.LocalClient = false;
                option.LocalServer = true;
            });
            Configure<CrossChainConfigOption>(option=>
            {
                option.ParentChainId = ChainHelpers.ConvertBase58ToChainId("AELF");
                option.ExtraDataSymbols = new List<string>();
            });

            var services = context.Services;
            services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockchainService>();
                mockService.Setup(m=>m.GetChainAsync())
                    .Returns(Task.FromResult(new Chain
                    {
                        LastIrreversibleBlockHeight = 10
                    }));
                mockService.Setup(m=>m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(Hash.Generate()));
                mockService.Setup(m=>m.GetBlockByHashAsync(It.IsAny<Hash>()))
                    .Returns(Task.FromResult(new Block
                    {
                        Height = 10,
                        Header = new BlockHeader
                        {
                            ChainId = 0,
                            BlockExtraDatas = { 
                                ByteString.CopyFrom(Hash.Generate().ToByteArray()), 
                                ByteString.CopyFrom(Hash.Generate().ToByteArray())
                            }
                        }
                    }));
                
                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockExtraDataExtractor>();
                mockService.Setup(m=>m.ExtractTransactionStatusMerkleTreeRoot(It.IsAny<BlockHeader>()))
                    .Returns(Hash.Generate);
                return mockService.Object;
            });
            
            services.AddSingleton<CrossChainRpc.CrossChainRpcBase, CrossChainGrpcServerBase>();
        }
    }
}