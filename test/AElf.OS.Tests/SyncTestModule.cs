using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS.Handlers;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.Types;
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
            Configure<NetworkOptions>(o =>
            {
                o.MinBlockGapBeforeSync = 2;
            });
            
            context.Services.Configure<BackgroundJobOptions>(options => { options.IsJobExecutionEnabled = false; });
            
            var block = new BlockWithTransactions { Header = new BlockHeader { Height = 1 } };
            var block2 = new BlockWithTransactions { Header = new BlockHeader { Height = 2 } };
            
            context.Services.AddTransient<PeerConnectedEventHandler>();

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

                blockchainService.Setup(bs => bs.GetChainAsync()).Returns(Task.FromResult(new Chain
                {
                    LastIrreversibleBlockHeight = 2,
                    BestChainHeight = 10
                }));

                blockchainService.Setup(bs => bs.AttachBlockToChainAsync(It.IsAny<Chain>(), It.Is<Block>(b => b.Equals(block))))
                    .Returns(Task.FromResult(BlockAttachOperationStatus.NewBlockLinked));
                
                blockchainService.Setup(bs => bs.AttachBlockToChainAsync(It.IsAny<Chain>(), It.Is<Block>(b => b.Equals(block2))))
                    .Returns(Task.FromResult(BlockAttachOperationStatus.NewBlockNotLinked));
                    
                return blockchainService.Object;
            });

            context.Services.AddSingleton<IAccountService>(p =>
            {
                var ecKeyPair = CryptoHelpers.GenerateKeyPair();
                
                Mock<IAccountService> mockAccountService = new Mock<IAccountService>();
                mockAccountService.Setup(ac => ac.GetPublicKeyAsync()).Returns(Task.FromResult(ecKeyPair.PublicKey));

                return mockAccountService.Object;
            });
            
            context.Services.AddSingleton<IBlockchainExecutingService>(p => Mock.Of<IBlockchainExecutingService>());
        }
    }
}