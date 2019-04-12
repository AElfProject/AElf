using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS.Handlers;
using AElf.OS.Jobs;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(OSAElfModule), typeof(KernelTestAElfModule))]
    public class SyncTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.Configure<BackgroundJobOptions>(options => { options.IsJobExecutionEnabled = false; });
            
            var block = new Block { Header = new BlockHeader { Height = 1 } };
            var block2 = new Block { Header = new BlockHeader { Height = 2 } };
            
            context.Services.AddTransient<PeerConnectedEventHandler>();

            context.Services.AddSingleton<IPeerPool>(p =>
            {
                var poolMock = new Mock<IPeerPool>();
                poolMock.Setup(pl => pl.FindPeerByPublicKey(It.IsAny<string>())).Returns(Mock.Of<IPeer>());
                return poolMock.Object;
            });

            context.Services.AddSingleton<INetworkService>(p =>
            {
                var netMock = new Mock<INetworkService>();
                
                netMock.Setup(ns =>
                    ns.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("linkable")), It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns(Task.FromResult(block));
                
                netMock.Setup(ns =>
                        ns.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("unlinkable")), It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns(Task.FromResult(block2));
                
                return netMock.Object;
            });

            context.Services.AddSingleton<IBlockchainService>(p =>
            {
                Mock<IBlockchainService> blockchainService = new Mock<IBlockchainService>();
                blockchainService
                    .Setup(bs => bs.GetBlockByHashAsync(It.Is<Hash>(h => h == Hash.FromString("block"))))
                    .Returns(Task.FromResult(new Block()));

                blockchainService.Setup(bs => bs.GetChainAsync()).Returns(Task.FromResult(new Chain {LastIrreversibleBlockHeight = 2}));

                blockchainService.Setup(bs => bs.AttachBlockToChainAsync(It.IsAny<Chain>(), It.Is<Block>(b => b.Equals(block))))
                    .Returns(Task.FromResult(BlockAttachOperationStatus.NewBlockLinked));             
                
                blockchainService.Setup(bs => bs.AttachBlockToChainAsync(It.IsAny<Chain>(), It.Is<Block>(b => b.Equals(block2))))
                    .Returns(Task.FromResult(BlockAttachOperationStatus.NewBlockNotLinked));
                    
                return blockchainService.Object;
            });
            
            context.Services.AddSingleton<IBlockchainExecutingService>(p => Mock.Of<IBlockchainExecutingService>());
        }
    }
}