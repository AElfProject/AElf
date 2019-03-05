using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.OS.Network.Infrastructure;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.OS.Tests.Network
{
    [DependsOn(typeof(TestBaseAElfModule), typeof(AbpEventBusModule), typeof(TestsOSAElfModule), typeof(TestBaseKernelAElfModule))]
    public class NetTestAElfModule : AElfModule
    {
        private static int height = 0;
        
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IBlockchainExecutingService>(p =>
            {
                var mockExec = new Mock<IBlockchainExecutingService>();
                mockExec.Setup(exec =>
                        exec.ExecuteBlocksAttachedToLongestChain(It.IsAny<Chain>(),
                            It.IsAny<BlockAttachOperationStatus>()))
                    .Returns<Chain, BlockAttachOperationStatus>((c, a) => Task.FromResult(new List<ChainBlockLink>()));
                return mockExec.Object;
            });

            Configure<NetworkOptions>(m => new NetworkOptions());
            
            // mock a peer
            Mock<IPeer> peerMock = new Mock<IPeer>();
            
            peerMock.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>()))
                .Returns<Hash, int>((hash, cnt) =>
                {
                    var blockList = new List<Block>();
                    
                    var genesis = new Block 
                    {
                        Header = new BlockHeader { Height = ChainConsts.GenesisBlockHeight, PreviousBlockHash = Hash.Genesis },
                        Body = new BlockBody()
                    };

                    var h = genesis.GetHash();
                    for (int i = 0; i < 5; i++)
                    {
                        var blk = new Block 
                        {
                            Header = new BlockHeader { Height = (long)i+2, PreviousBlockHash = h }, 
                            Body = new BlockBody()
                        };
                        
                        blockList.Add(blk);
                        h = blk.GetHash();
                    }
                                        
                    return Task.FromResult(blockList);
                });
            
            Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
            peerPoolMock.Setup(p => p.FindPeer(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Returns<string, byte[]>((adr, pubkey) => peerMock.Object);
            peerPoolMock.Setup(p => p.GetPeers())
                .Returns(new List<IPeer> { peerMock.Object });

            context.Services.AddSingleton<IPeerPool>(peerPoolMock.Object);

            context.Services.AddTransient<ForkDownloadJob>();
        }
        
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            //init test data here
            var blockChainService = context.ServiceProvider.GetService<IBlockchainService>();
            
            var genesis = new Block {
                Header = new BlockHeader {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis,
                },
                Body = new BlockBody()
            };
            
            blockChainService.CreateChainAsync(genesis);
        }
    }
}