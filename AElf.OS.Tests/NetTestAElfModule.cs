using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS.Jobs;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(OSTestAElfModule))]
    public class NetTestAElfModule : AElfModule
    {
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

            context.Services.AddTransient<ForkDownloadJob>();

            context.Services.AddSingleton<IPeerPool>(o =>
            {
                var blockchainService = context.Services.GetRequiredServiceLazy<IBlockchainService>().Value;

                Mock<IPeer> peerMock = new Mock<IPeer>();

                peerMock.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>()))
                    .Returns<Hash, int>((hash, cnt) =>
                    {
                        var blockList = new List<Block>();

                        var chain = blockchainService.GetChainAsync().Result;
                        var previousBlockHash = chain.BestChainHash;
                        for (var i = chain.BestChainHeight; i < chain.BestChainHeight + 5; i++)
                        {
                            var blk = new Block
                            {
                                Header = new BlockHeader
                                {
                                    ChainId = chain.Id,
                                    Height = i + 1,
                                    PreviousBlockHash = previousBlockHash
                                },
                                Body = new BlockBody()
                            };

                            blockList.Add(blk);
                            previousBlockHash = blk.GetHash();
                        }

                        return Task.FromResult(blockList);
                    });

                Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
                peerPoolMock.Setup(p => p.FindPeerByAddress(It.IsAny<string>()))
                    .Returns<string>((adr) => peerMock.Object);
                peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                    .Returns(new List<IPeer> {peerMock.Object});

                return peerPoolMock.Object;
            });
        }
    }
}